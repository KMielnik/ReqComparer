using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace VisualComparer
{
    /// <summary>
    /// Interaction logic for SingleRequirementView.xaml
    /// </summary>
    public partial class SingleRequirementView : UserControl
    {
        public ObservableCollection<RequirementSingleView> reqsCollection { get; set; }
        private ObservableCollection<ReqComparer.Requirement> basicReqs;
        private ObservableCollection<int> AllTCs;
        private ObservableCollection<Dictionary<string,bool>> ReqTopHelperData;
        private ObservableCollection<Dictionary<string,bool>> ReqBottomHelperData;

        private Brush[] brushes = {
            Brushes.Red,
            Brushes.Blue,
            Brushes.Green,
            Brushes.Purple,
            Brushes.Orange,
            Brushes.Navy
        };

        public SingleRequirementView(ObservableCollection<ReqComparer.Requirement> basicReqs)
        {
            InitializeComponent();
            reqsCollection = new ObservableCollection<RequirementSingleView>();
            AllTCs = new ObservableCollection<int>();
            this.basicReqs = basicReqs;
            this.basicReqs.CollectionChanged += BasicReqs_CollectionChanged;

            RequirementsDataGrid.ItemsSource = reqsCollection;
            AllTCsListBox.ItemsSource = AllTCs;

            ReqTopHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqBottomHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqTopHelperData.Add(new Dictionary<string, bool>());
            ReqBottomHelperData.Add(new Dictionary<string, bool>());
            ReqHelperTop.ItemsSource = ReqTopHelperData;
            ReqHelperBottom.ItemsSource = ReqBottomHelperData;

            SetReqDataGrid();
            setBoldDataTrigger(RequirementsDataGrid);
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
                Header = "TCs",
                Binding = new Binding(nameof(RequirementDoubleView.TCStringified)),
                IsReadOnly = true
            });

            RequirementsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Functional Variants",
                Binding = new Binding(nameof(RequirementDoubleView.FVariants)),
                IsReadOnly = true,
                Width = 200
            });
        }

        private void SetReqDataGrid()
        {
            RequirementsDataGrid.CanUserResizeRows = false;
            RequirementsDataGrid.RowHeight = 24;

            reqsCollection.Clear();
            foreach (var req in basicReqs)
                reqsCollection.Add(new RequirementSingleView(req));

            AllTCs.Clear();

            reqsCollection
                .SelectMany(x => x.TCIDsValue)
                .Distinct()
                .OrderBy(x => x)
                .ToList()
                .ForEach(x => AllTCs.Add(x));

            foreach (var req in reqsCollection)
                req.SetCoveredTCs(AllTCs);

            RefreshRequirementsDataGrid();
            SetReqHelpersColumns();
        }

        private void BasicReqs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetReqDataGrid();
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
            
            foreach(var TCSelected in AllTCsListBox.SelectedItems)
            {
                DataGridTextColumn TCColumn = new DataGridTextColumn
                {
                    Header = TCSelected.ToString()
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

                RequirementsDataGrid.Columns.Add(TCColumn);
            }

            SetReqHelpersColumns();
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

            foreach (var TCSelected in AllTCsListBox.SelectedItems)
            {
                var firstOccurence = reqsCollection
                    .TakeWhile(x => x.TCCovered[TCSelected.ToString()] != true)
                    .Count();

                if (firstOccurence < firstRow)
                    ReqTopHelperData[0][TCSelected.ToString()] = true;
                else
                    ReqTopHelperData[0][TCSelected.ToString()] = false;


                var lastOccurence = reqsCollection.Count() - reqsCollection
                    .Reverse()
                    .TakeWhile(x => x.TCCovered[TCSelected.ToString()] != true)
                    .Count() + 5;

                if (firstOccurence > lastRow)
                    ReqBottomHelperData[0][TCSelected.ToString()] = true;
                else
                    ReqBottomHelperData[0][TCSelected.ToString()] = false;
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
    }
}
