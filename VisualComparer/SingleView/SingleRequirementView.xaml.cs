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
using System.Text.RegularExpressions;

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

            ValidIn.SelectedIndex = 0;
            ValidIn.UpdateLayout();

            this.basicReqs = basicReqs;
            this.basicReqs.CollectionChanged += BasicReqs_CollectionChanged;

            RequirementsDataGrid.ItemsSource = reqsCollection;
            AllTCsListBox.ItemsSource = FilteredTCs;
            var TCView = CollectionViewSource.GetDefaultView(FilteredTCs);
            TCView.Filter = x =>
            {
                if (AllTCsListBox.SelectedItems.Contains(x))
                    return true;
                if (string.IsNullOrEmpty(TCFilter.Text))
                    return true;

                return x.ToString().StartsWith(TCFilter.Text);
            };

            ReqTopHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqBottomHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqTopHelperData.Add(new Dictionary<string, bool>());
            ReqBottomHelperData.Add(new Dictionary<string, bool>());
            ReqHelperTop.ItemsSource = ReqTopHelperData;
            ReqHelperBottom.ItemsSource = ReqBottomHelperData;

            setBoldDataTrigger(RequirementsDataGrid);
            setVisibilityDataTrigger(RequirementsDataGrid);
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

            basicReqs
                .AsParallel()
                .AsOrdered()
                .Select(x=> new RequirementSingleView(x))
                .ToList()
                .ForEach(x => reqsCollection.Add(new RequirementSingleView(x)));

            FilteredTCs.Clear();
            AllTCs.Clear();

            var TCParseTask = Task.Run(() =>
            {
                reqsCollection
                .AsParallel()
                .SelectMany(x => x.TCs)
                .Distinct()
                .OrderBy(x => x.IDValue)
                .ToList()
                .ForEach(x => AllTCs.Add(x));
            });


            RefreshRequirementsDataGrid();
            SetReqHelpersColumns();

            await TCParseTask;
            RefreshFilteredTCs();
        }

        private async void BasicReqs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await SetReqDataGrid();
            GetScrollViewer(RequirementsDataGrid)
                .ScrollToVerticalOffset(reqsCollection.IndexOf(reqsCollection
                    .Skip(1)
                    .First(x => x.Type == ReqComparer.Requirement.Types.Head)));
        }

        private void setBoldDataTrigger(DataGrid datagrid)
        {
            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding(nameof(RequirementSingleView.Type)),
                Value = ReqComparer.Requirement.Types.Head
            };

            dataTrigger.Setters.Add(new Setter()
            {
                Property = FontWeightProperty,
                Value = FontWeights.ExtraBold
            });

            datagrid.RowStyle.Triggers.Add(dataTrigger);

            dataTrigger = new DataTrigger()
            {
                Binding = new Binding(nameof(RequirementSingleView.Type)),
                Value = ReqComparer.Requirement.Types.Req
            };

            dataTrigger.Setters.Add(new Setter()
            {
                Property = FontWeightProperty,
                Value = FontWeights.Bold
            });

            datagrid.RowStyle.Triggers.Add(dataTrigger);
        }

        private void setVisibilityDataTrigger(DataGrid datagrid)
        {
            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding(nameof(RequirementSingleView.IsVisible)),
                Value = false
            };

            dataTrigger.Setters.Add(new Setter()
            {
                Property = VisibilityProperty,
                Value = Visibility.Hidden
            });

            datagrid.RowStyle.Triggers.Add(dataTrigger);
        }

        private void ShowOneChapter(int chapterID, bool showAllTCs = false)
        {
            foreach (var req in reqsCollection)
                req.IsVisible = true;

            if (chapterID == 0)
            {
                RequirementsDataGrid.Items.Refresh();
                ChapterNameTextBlock.Text = "-";
                return;
            }

            var firstChapterReq = reqsCollection
                .SkipWhile(x => x.IDValue != chapterID)
                .FirstOrDefault();

            var chapterLevel = firstChapterReq
                ?.Level;

            if (chapterLevel is null)
            {
                RequirementsDataGrid.Items.Refresh();
                MessageBox.Show("Invalid chapter ID");
                return;
            }

            var chapterReqs = reqsCollection
                    .SkipWhile(x => x.IDValue != chapterID)
                    .TakeWhile(x => x.Level > chapterLevel || x.IDValue == chapterID)
                    .ToList();

            if(showAllTCs)
            {
                AllTCsListBox.SelectedItems.Clear();

                chapterReqs
                    .SelectMany(x => x.TCIDsValue)
                    .Distinct()
                    .ToList()
                    .ForEach(x => AllTCsListBox.SelectedItems.Add(x));
                PushSelectedTCsUp();
            }

            reqsCollection
                .Except(chapterReqs)
                .ToList()
                .ForEach(x => x.IsVisible = false);

            var scrollViewer = GetScrollViewer(RequirementsDataGrid);
            scrollViewer.ScrollToVerticalOffset(reqsCollection.IndexOf(firstChapterReq));

            RequirementsDataGrid.Items.Refresh();
        }

        private void AllTCsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshRequirementsDataGrid();

            int actualBrush = 0;
            
            foreach(int TCSelected in AllTCsListBox.SelectedItems)
            {
                DataGridTextColumn TCColumn = new DataGridTextColumn
                {
                    Header = TCSelected,
                    IsReadOnly = true
                };


                var dataTrigger = new DataTrigger()
                {
                    Binding = new Binding(nameof(RequirementSingleView.TCIDsValue)) { Converter = new TestCaseConverter(), ConverterParameter = TCSelected },
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

            var tcID = (int)header.Content;

            var firstOccurence = reqsCollection
                    .TakeWhile(x => x.TCIDsValue.Contains(tcID) != true)
                    .Count();

            var lastOccurence = reqsCollection.Count() - reqsCollection
                   .Reverse()
                   .TakeWhile(x => x.TCIDsValue.Contains(tcID) != true)
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

            foreach (var column in RequirementsDataGrid.Columns.OrderBy(x=>x.DisplayIndex))
            {
                ReqHelperTop.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Header,
                    Width = column.ActualWidth,
                    IsReadOnly = true
                });

                ReqHelperBottom.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Header,
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

        public static ScrollViewer GetScrollViewer(UIElement element)
        {
            if (element == null) return null;

            ScrollViewer retour = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element) && retour == null; i++)
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
                    retour = (ScrollViewer)(VisualTreeHelper.GetChild(element, i));
                else
                    retour = GetScrollViewer(VisualTreeHelper.GetChild(element, i) as UIElement);

            return retour;
        }

        private void RequirementsDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            new List<ScrollViewer>()
            {
                GetScrollViewer(ReqHelperTop),
                GetScrollViewer(ReqHelperBottom)
            }.ForEach(x => x.ScrollToHorizontalOffset(e.HorizontalOffset));

            var datagridHeight = RequirementsDataGrid.ActualHeight;
            var rowHeight = RequirementsDataGrid.RowHeight;
            var firstRow = (int)e.VerticalOffset;
            var lastRow = firstRow + (int)(datagridHeight / rowHeight) - 1;

            if (reqsCollection.Any(x => x.IsVisible == false))
            {
                var firstVisible = reqsCollection
                    .IndexOf(reqsCollection
                        .First(x => x.IsVisible == true));
                var lastVisible = reqsCollection
                    .IndexOf(reqsCollection
                        .Last(x => x.IsVisible == true));

                if (lastVisible < lastRow)
                    lastRow = lastVisible + 1;

                if (e.VerticalChange < 0 && firstRow<firstVisible)
                {
                    var scrollViewer = GetScrollViewer(RequirementsDataGrid);
                    scrollViewer.ScrollToVerticalOffset(firstVisible);
                }
                else if (e.VerticalChange > 0 && lastVisible < firstRow)
                {
                    var scrollViewer = GetScrollViewer(RequirementsDataGrid);
                    scrollViewer.ScrollToVerticalOffset(lastVisible);
                }
                RequirementsDataGrid.UpdateLayout();
            }

            foreach (int TCSelected in AllTCsListBox.SelectedItems)
            {
                var TCSelectedString = TCSelected.ToString();

                var firstOccurence = reqsCollection
                    .TakeWhile(x => x.TCIDsValue.Contains(TCSelected) != true)
                    .Count();

                if (firstOccurence < firstRow)
                    ReqTopHelperData[0][TCSelectedString] = true;
                else
                    ReqTopHelperData[0][TCSelectedString] = false;


                var lastOccurence = reqsCollection.Count() - reqsCollection
                    .Reverse()
                    .TakeWhile(x => x.TCIDsValue.Contains(TCSelected) != true)
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
            ShowOneChapter(0);
        }

        private void TCFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(FilteredTCs).Refresh();
        }
        private void PushSelectedTCsUp()
        {
            var selectedTcs = AllTCsListBox
                .SelectedItems
                .Cast<int>()
                //.OrderByDescending(x => x)
                .ToList();

            AllTCsListBox.SelectedItems.Clear();

            var temp = TCFilter.Text;
            TCFilter.Text = "";
            RefreshFilteredTCs();

            selectedTcs.ForEach(x => FilteredTCs.Move(FilteredTCs.IndexOf(x), 0));

            selectedTcs.ForEach(x => AllTCsListBox.SelectedItems.Add(x));
            TCFilter.Text = temp;
        }
        private void AllTCsListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            PushSelectedTCsUp();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            TCFilter.Text = "";
            AllTCsListBox.SelectedItems.Clear();
            ShowOneChapter(0);
        }

        public void ChapterSelectButton_Click(object sender,RoutedEventArgs e)
        {
            var chapterSelectionWindow = new ChapterSelectionWindow(reqsCollection);
            chapterSelectionWindow.ShowDialog();
            if (chapterSelectionWindow.DialogResult == true)
            {
                var selectedChapter = chapterSelectionWindow.Answer;
                Console.WriteLine(selectedChapter.chapter);
                ShowOneChapter(selectedChapter.id, true);
                ChapterNameTextBlock.Text = selectedChapter.chapter;
            }
        }
        private void RequirementsDataGrid_ColumnDisplayIndexChanged(object sender, DataGridColumnEventArgs e)
        {
            ReqHelperTop.Columns
                .First(x => x.Header.ToString() == e.Column.Header.ToString())
                .DisplayIndex = e.Column.DisplayIndex;

            ReqHelperBottom.Columns
                .First(x => x.Header.ToString() == e.Column.Header.ToString())
                .DisplayIndex = e.Column.DisplayIndex;
        }

        private void ChapterClear_Click(object sender, RoutedEventArgs e)
        {
            ShowOneChapter(0);
        }
    }
}
