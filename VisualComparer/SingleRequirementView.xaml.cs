using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.ComponentModel;

namespace VisualComparer
{
    /// <summary>
    /// Interaction logic for SingleRequirementView.xaml
    /// </summary>
    public partial class SingleRequirementView : UserControl
    {
        public ObservableCollection<RequirementSingleView> reqsCollection { get; set; }
        private ListWithNotifications<ReqComparer.Requirement> basicReqs;
        private ObservableCollection<int> FilteredTCs;
        private ObservableCollection<ReqComparer.TestCase> AllTCs;
        private ObservableCollection<Dictionary<string,bool>> ReqTopHelperData;
        private ObservableCollection<Dictionary<string,bool>> ReqBottomHelperData;

        private ICollectionView TCView;

        private Brush[] brushes = {
            Brushes.Red,
            Brushes.Blue,
            Brushes.Green,
            Brushes.Purple,
            Brushes.Orange,
            Brushes.Navy
        };

        public SingleRequirementView(ListWithNotifications<ReqComparer.Requirement> basicReqs)
        {
            InitializeComponent();
            reqsCollection = new ObservableCollection<RequirementSingleView>();
            FilteredTCs = new ObservableCollection<int>();
            AllTCs = new ObservableCollection<ReqComparer.TestCase>();

            ValidIn.SelectedItem = "-";

            this.basicReqs = basicReqs;
            this.basicReqs.CollectionChanged += BasicReqs_CollectionChanged;

            RequirementsDataGrid.ItemsSource = reqsCollection;
            AllTCsListBox.ItemsSource = FilteredTCs;
            TCView = CollectionViewSource.GetDefaultView(FilteredTCs);
            TCView.Filter = x =>
            {
                if (AllTCsListBox.SelectedItems.Contains(x))
                    return true;
                if (string.IsNullOrEmpty(TCFilter.Text))
                    return true;

                return x.ToString().Contains(TCFilter.Text);
            };

            ReqTopHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqBottomHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqTopHelperData.Add(new Dictionary<string, bool>());
            ReqBottomHelperData.Add(new Dictionary<string, bool>());
            ReqHelperTop.ItemsSource = ReqTopHelperData;
            ReqHelperBottom.ItemsSource = ReqBottomHelperData;

            setBoldDataTrigger(RequirementsDataGrid);
        }

        private void RefreshFilteredTCs()
        {
            var ValidInVersion = (ValidIn.SelectedItem as ComboBoxItem)?.Content.ToString();
            FilteredTCs.Clear();
            foreach (var TC in AllTCs)
            {
                if (TC.IsValidInSpecifiedVersion(ValidInVersion))
                    FilteredTCs.Add(TC.IDValue);
            }
        }

        private void RefreshRequirementsDataGrid()
        {
            RequirementsDataGrid.Columns.Clear();

            RequirementsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new Binding(nameof(RequirementDoubleView.ID)),
                IsReadOnly = true
            });

            RequirementsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Text",
                Binding = new Binding(nameof(RequirementDoubleView.TextIntended)),
                IsReadOnly = true,
                Width = 500
            });

            RequirementsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Functional Variants",
                Binding = new Binding(nameof(RequirementDoubleView.FVariants)),
                IsReadOnly = true,
                Width = 200
            });
        }

        private async Task SetReqDataGrid()
        {
            RequirementsDataGrid.CanUserResizeRows = false;
            RequirementsDataGrid.RowHeight = 20;
            ReqHelperTop.RowHeight = 15;
            ReqHelperBottom.RowHeight = 15;


            reqsCollection.Clear();
            foreach (var req in basicReqs)
                reqsCollection.Add(new RequirementSingleView(req));

            FilteredTCs.Clear();
            AllTCs.Clear();

            var TCParseTask = Task.Run(() =>
            {
                reqsCollection
                .SelectMany(x => x.TCs)
                .Distinct()
                .OrderBy(x => x.IDValue)
                .ToList()
                .ForEach(x => AllTCs.Add(x));

                var allTCsValues = AllTCs.Select(x => x.IDValue);

                reqsCollection
                    .AsParallel()
                    .ForAll(x => x.SetCoveredTCs(allTCsValues));
            });


            RefreshRequirementsDataGrid();
            SetReqHelpersColumns();

            await TCParseTask;
            RefreshFilteredTCs();
        }

        private async void BasicReqs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await SetReqDataGrid();
        }

        private void setBoldDataTrigger(DataGrid datagrid)
        {
            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding(nameof(RequirementDoubleView.IsImportant)),
                Value = true
            };

            dataTrigger.Setters.Add(new Setter()
            {
                Property = FontWeightProperty,
                Value = FontWeights.Bold
            });

            datagrid.RowStyle.Triggers.Add(dataTrigger);
        }

        private void AllTCsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshRequirementsDataGrid();

            int actualBrush = 0;
            
            foreach(int TCSelected in AllTCsListBox.SelectedItems)
            {
                DataGridTextColumn TCColumn = new DataGridTextColumn
                {
                    Header = TCSelected
                };


                var dataTrigger = new DataTrigger()
                {
                    Binding = new Binding(nameof(RequirementSingleView.TCCovered) + "[" + TCSelected + "]"),
                    Value = "True"
                };

                dataTrigger.Setters.Add(new Setter()
                {
                    Property = BackgroundProperty,
                    Value = brushes[actualBrush % brushes.Length]
                });

                dataTrigger.Setters.Add(new Setter()
                {
                    Property = ForegroundProperty,
                    Value = brushes[actualBrush % brushes.Length]
                });

                dataTrigger.Setters.Add(new Setter()
                {
                    Property = BorderBrushProperty,
                    Value = brushes[actualBrush++ % brushes.Length]
                });

                TCColumn.CellStyle = new Style();
                TCColumn.CellStyle.Triggers.Add(dataTrigger);

                TCColumn.HeaderStyle = new Style();

                TCColumn.HeaderStyle.Setters.Add(new EventSetter()
                {
                    Event = MouseDoubleClickEvent,
                    Handler = new MouseButtonEventHandler(Helper_DoubleClickEvent)
                });

                RequirementsDataGrid.Columns.Add(TCColumn);
            }

            SetReqHelpersColumns();
        }

        private void Helper_DoubleClickEvent(object sender, MouseButtonEventArgs e)
        {
            var header = (System.Windows.Controls.Primitives.DataGridColumnHeader)sender;

            var firstOccurence = reqsCollection
                    .TakeWhile(x => x.TCCovered[(int)header.Content] != true)
                    .Count();

            var lastOccurence = reqsCollection.Count() - reqsCollection
                   .Reverse()
                   .TakeWhile(x => x.TCCovered[(int)header.Content] != true)
                   .Count();

            RequirementsDataGrid.ScrollIntoView(RequirementsDataGrid.Items[lastOccurence]);

            RequirementsDataGrid.ScrollIntoView(RequirementsDataGrid.Items[firstOccurence]);
        }

        private void SetReqHelpersColumns()
        {
            ReqHelperTop.Columns.Clear();
            ReqHelperBottom.Columns.Clear();

            ReqTopHelperData[0].Clear();
            ReqBottomHelperData[0].Clear();

            int actualBrush = 0;

            foreach (var column in RequirementsDataGrid.Columns)
            {
                ReqHelperTop.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Header,
                    Binding = new Binding("[" + column.Header + "]"),
                    Width = column.ActualWidth,
                    IsReadOnly = true
                });

                ReqHelperBottom.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Header,
                    Binding = new Binding("[" + column.Header + "]"),
                    Width = column.ActualWidth,
                    IsReadOnly = true
                });

                if (!int.TryParse(column.Header.ToString(), out _))
                    continue;
                
                ReqTopHelperData[0].Add(column.Header.ToString(), false);
                ReqBottomHelperData[0].Add(column.Header.ToString(), false);

                var dataTrigger = new DataTrigger()
                {
                    Binding = new Binding("[" + column.Header + "]"),
                    Value = "true"
                };
                dataTrigger.Setters.Add(new Setter()
                {
                    Property = BorderBrushProperty,
                    Value = Brushes.Black
                });
                dataTrigger.Setters.Add(new Setter()
                {
                    Property = BackgroundProperty,
                    Value = brushes[actualBrush++ % brushes.Length]
                });

                ReqHelperTop.Columns.Last().CellStyle = new Style();
                ReqHelperTop.Columns.Last().CellStyle.Triggers.Add(dataTrigger);

                ReqHelperBottom.Columns.Last().CellStyle = new Style();
                ReqHelperBottom.Columns.Last().CellStyle.Triggers.Add(dataTrigger);
            }
        }

        private void RequirementsDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var datagridHeight = RequirementsDataGrid.ActualHeight;
            var rowHeight = RequirementsDataGrid.RowHeight;
            var firstRow = e.VerticalOffset;
            var lastRow = firstRow + (int)(datagridHeight / rowHeight) - 1;

            foreach (int TCSelected in AllTCsListBox.SelectedItems)
            {
                var TCSelectedString = TCSelected.ToString();

                var firstOccurence = reqsCollection
                    .TakeWhile(x => x.TCCovered[TCSelected] != true)
                    .Count();

                if (firstOccurence < firstRow)
                    ReqTopHelperData[0][TCSelectedString] = true;
                else
                    ReqTopHelperData[0][TCSelectedString] = false;


                var lastOccurence = reqsCollection.Count() - reqsCollection
                    .Reverse()
                    .TakeWhile(x => x.TCCovered[TCSelected] != true)
                    .Count();

                if (lastOccurence > lastRow)
                    ReqBottomHelperData[0][TCSelectedString] = true;
                else
                    ReqBottomHelperData[0][TCSelectedString] = false;
            }

            ReqHelperTop.Items.Refresh();
            ReqHelperBottom.Items.Refresh();
        }

        private void RequirementsDataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            for (int i = 0; i < RequirementsDataGrid.Columns.Count; i++)
            {
                ReqHelperTop.Columns[i].Width = RequirementsDataGrid.Columns[i].Width;
                ReqHelperBottom.Columns[i].Width = RequirementsDataGrid.Columns[i].Width;
            }
        }

        private void ValidFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilteredTCs();
        }

        private void TCFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(FilteredTCs).Refresh();
        }
    }
}
