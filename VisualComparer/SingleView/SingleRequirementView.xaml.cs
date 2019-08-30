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
        private ObservableCollection<Dictionary<string, bool>> ReqTopHelperData;
        private ObservableCollection<Dictionary<string, bool>> ReqBottomHelperData;

        private Brush[] brushes = {
            GetBrushFromHex("#e6194B"),
            GetBrushFromHex("#3cb44b"),
            GetBrushFromHex("#ffe119"),
            GetBrushFromHex("#4363d8"),
            GetBrushFromHex("#f58231"),
            GetBrushFromHex("#911eb4"),
            GetBrushFromHex("#42d4f4"),
            GetBrushFromHex("#f032e6"),
            GetBrushFromHex("#bfef45"),
            GetBrushFromHex("#fabebe"),
            GetBrushFromHex("#469990"),
            GetBrushFromHex("#e6beff"),
            GetBrushFromHex("#9A6324")
        };

        public SingleRequirementView(ListWithNotifications<ReqComparer.Requirement> basicReqs)
        {
            InitializeComponent();
            reqsCollection = new ObservableCollection<RequirementSingleView>();
            FilteredTCs = new ObservableCollection<int>();
            AllTCs = new ObservableCollection<ReqComparer.TestCase>();

            this.basicReqs = basicReqs;
            this.basicReqs.CollectionChanged += BasicReqs_CollectionChanged;

            RequirementsDataGrid.ItemsSource = reqsCollection;
            SetReqDataGridFilter();
            AllTCsListBox.ItemsSource = FilteredTCs;
            SetTCListView();

            ReqTopHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqBottomHelperData = new ObservableCollection<Dictionary<string, bool>>();
            ReqTopHelperData.Add(new Dictionary<string, bool>());
            ReqBottomHelperData.Add(new Dictionary<string, bool>());
            ReqHelperTop.ItemsSource = ReqTopHelperData;
            ReqHelperBottom.ItemsSource = ReqBottomHelperData;

            RequirementsDataGrid.RowStyle = new Style();

            setBoldDataTrigger(RequirementsDataGrid);
            setVisibilityDataTrigger(RequirementsDataGrid);
        }

        private void SetReqDataGridFilter()
        {
            var reqDataGridFilter = CollectionViewSource.GetDefaultView(reqsCollection);
            reqDataGridFilter.Filter = x =>
            {
                var req = x as RequirementSingleView;
                var ValidInVersion = (ValidIn.SelectedItem as ComboBoxItem)?.Content.ToString();
                return req.IsValidInSpecifiedVersion(ValidInVersion) && !req.IsDropped;
            };
        }

        private static SolidColorBrush GetBrushFromHex(string hex)
            => (SolidColorBrush)new BrushConverter().ConvertFrom(hex);

        private void SetTCListView()
        {
            var TCView = CollectionViewSource.GetDefaultView(FilteredTCs);
            TCView.Filter = x =>
            {
                if (AllTCsListBox.SelectedItems.Contains(x))
                    return true;
                if (string.IsNullOrEmpty(TCFilter.Text))
                    return true;

                var delimeter = Regex.Match(TCFilter.Text, "[^0-9]");
                if(!delimeter.Success)
                    return x.ToString().StartsWith(TCFilter.Text);

                return x.ToString().StartsWith(TCFilter.Text.Split(delimeter.Value.ToCharArray()).Last());
            };
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
                .Select(x => new RequirementSingleView(x))
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
                .ScrollToVerticalOffset(RequirementsDataGrid.Items.IndexOf(RequirementsDataGrid.Items
                    .Cast<RequirementSingleView>()
                    .Skip(1)
                    .First(x => x.Type == ReqComparer.Requirement.Types.Head)));
            ChapterNameTextBlock.Text = "-";
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

        private async Task ShowOneChapter(int chapterID, bool showAllTCs = false)
        {
            TCFilter.Text = "";
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

            if (showAllTCs)
            {
                var selectedTCs = chapterReqs
                    .SelectMany(x => x.TCIDsValue)
                    .Distinct()
                    .ToList();

                await SelectMultipleTCs(selectedTCs);

                await PushSelectedTCsUp();
            }

            reqsCollection
                .Except(chapterReqs)
                .ToList()
                .ForEach(x => x.IsVisible = false);

            var scrollViewer = GetScrollViewer(RequirementsDataGrid);
            scrollViewer.ScrollToVerticalOffset(RequirementsDataGrid.Items.IndexOf(firstChapterReq));

            RequirementsDataGrid.Items.Refresh();
            await RefreshHelpers();
        }

        private async Task SelectMultipleTCs(List<int> tcs)
        {
            tcs.ForEach(x => AllTCsListBox.SelectedItems.Add(x));
        }

        private async Task AddTCColumn(int tc)
        {
            DataGridTextColumn TCColumn = new DataGridTextColumn
            {
                Header = tc,
                IsReadOnly = true
            };


            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding(nameof(RequirementSingleView.TCIDsValue)) { Converter = new TestCaseConverter(), ConverterParameter = tc },
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
                Handler = new MouseButtonEventHandler(ColumnHeader_DoubleClickEvent)
            });

            TCColumn.HeaderStyle.Setters.Add(new EventSetter()
            {
                Event = MouseRightButtonDownEvent,
                Handler = new MouseButtonEventHandler(ColumnHeader_RightClickEvent)
            });

            TCColumn.HeaderStyle.Setters.Add(new EventSetter()
            {
                Event = MouseDoubleClickEvent,
                Handler = new MouseButtonEventHandler(ColumnHeader_DoubleClickEvent)
            });

            TCColumn.HeaderStyle.Setters.Add(new EventSetter()
            {
                Event = MouseRightButtonDownEvent,
                Handler = new MouseButtonEventHandler(ColumnHeader_RightClickEvent)
            });

            TCColumn.HeaderStyle.Setters.Add(new Setter()
            {
                Property = ToolTipProperty,
                Value = AllTCs.First(x => x.IDValue == tc).Text
            });

            await Task.Run(async () =>
                await RequirementsDataGrid.Dispatcher.BeginInvoke((Action)(() => RequirementsDataGrid.Columns.Add(TCColumn))));
        }

        private async Task DeleteTCColumn(int tc)
        {
            var removedColumn = RequirementsDataGrid.Columns
                       .FirstOrDefault(x => x.Header is int && (int)x.Header == tc);

            await Task.Run(async () =>
            {
                if (removedColumn != null)
                    await RequirementsDataGrid.Dispatcher.BeginInvoke((Action)(() => RequirementsDataGrid.Columns.Remove(removedColumn)));
            });
        }
        

        private int actualBrush = 0;
        private async void AllTCsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AllTCsListBox.SelectedItems.Count == 0)
                actualBrush = 0;

            var deletingTasks = new List<Task>();
            foreach (int TCSelected in e.RemovedItems)
                deletingTasks.Add(DeleteTCColumn(TCSelected));

            await Task.WhenAll(deletingTasks);

            foreach (int TCSelected in e.AddedItems)
                await AddTCColumn(TCSelected);

            SetReqHelpersColumns();
        }

        private void ColumnHeader_RightClickEvent(object sender, MouseButtonEventArgs e)
        {
            var TC = (int)(sender as System.Windows.Controls.Primitives.DataGridColumnHeader).Content;
            RequirementsDataGrid.SelectedItems.Clear();
            RequirementsDataGrid.Items.Cast<RequirementSingleView>()
                .Where(x => x.TCIDsValue.Contains(TC))
                .ToList()
                .ForEach(x => RequirementsDataGrid.SelectedItems.Add(x));
            RequirementsDataGrid.Focus();
        }

        private void ColumnHeader_DoubleClickEvent(object sender, MouseButtonEventArgs e)
        {
            var header = (System.Windows.Controls.Primitives.DataGridColumnHeader)sender;

            var tcID = (int)header.Content;

            var firstOccurence = RequirementsDataGrid.Items.Cast<RequirementSingleView>()
                    .TakeWhile(x => x.TCIDsValue.Contains(tcID) != true)
                    .Count();

            var lastOccurence = RequirementsDataGrid.Items.Cast<RequirementSingleView>().Count() - RequirementsDataGrid.Items.Cast<RequirementSingleView>()
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

            foreach (var column in RequirementsDataGrid.Columns.OrderBy(x => x.DisplayIndex))
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

                var columnValue = column.Header.ToString();


                ReqTopHelperData[0].Add(columnValue, false);
                ReqBottomHelperData[0].Add(columnValue, false);


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

                var eventTrigger = new EventSetter()
                {
                    Event = PreviewMouseLeftButtonDownEvent,
                    Handler = new MouseButtonEventHandler(Helper_LeftClickEvent)
                };

                ReqHelperTop.Columns.Last().CellStyle = new Style();
                ReqHelperTop.Columns.Last().CellStyle.Triggers.Add(dataTrigger);
                ReqHelperTop.Columns.Last().CellStyle.Setters.Add(eventTrigger);

                ReqHelperBottom.Columns.Last().CellStyle = new Style();
                ReqHelperBottom.Columns.Last().CellStyle.Triggers.Add(dataTrigger);
                ReqHelperBottom.Columns.Last().CellStyle.Setters.Add(eventTrigger);
            }
        }

        private void Helper_LeftClickEvent(object sender, MouseButtonEventArgs e)
        {
            var gridCell = (DataGridCell)sender;
            var tc = (int)gridCell.Column.Header;

            var senderDatagrid = VisualTreeHelper.GetParent(gridCell);
            while (senderDatagrid != null && senderDatagrid.GetType() != typeof(DataGrid))
                senderDatagrid = VisualTreeHelper.GetParent(senderDatagrid);

            var isGoingDown = ((DataGrid)senderDatagrid).Name == nameof(ReqHelperBottom);

            var selectedRows = RequirementsDataGrid.SelectedItems.Count;

            if (selectedRows >= 1)
            {
                var firstSelected = (RequirementSingleView)RequirementsDataGrid.SelectedItems[0];
                RequirementsDataGrid.SelectedItems.Clear();

                if (firstSelected.TCIDsValue.Contains(tc))
                    RequirementsDataGrid.SelectedItems.Add(firstSelected);
                else
                {
                    RequirementsDataGrid.SelectedItems.Add(RequirementsDataGrid.Items.Cast<RequirementSingleView>().First(x => x.TCIDsValue.Contains(tc) && x.IsVisible));
                    RequirementsDataGrid.ScrollIntoView(firstSelected);
                    return;
                }

                RequirementsDataGrid.ScrollIntoView(firstSelected);

                if (selectedRows > 1)
                    return;


                RequirementsDataGrid.SelectedItems.Clear();
                var reqs = RequirementsDataGrid.Items.Cast<RequirementSingleView>().AsEnumerable();
                if (isGoingDown == false)
                    reqs = reqs.Reverse();

                RequirementsDataGrid.SelectedItems
                    .Add(reqs
                        .SkipWhile(x => x != firstSelected)
                        .Skip(1)
                        .FirstOrDefault(x => x.TCIDsValue.Contains(tc) && x.IsVisible));

                if (RequirementsDataGrid.SelectedItem == null)
                    RequirementsDataGrid.SelectedItems.Add(firstSelected);

                RequirementsDataGrid.ScrollIntoView(RequirementsDataGrid.SelectedItem);
            }
            else
            {
                RequirementsDataGrid.SelectedItems.Add(RequirementsDataGrid.Items.Cast<RequirementSingleView>().First(x => x.TCIDsValue.Contains(tc) && x.IsVisible));
                RequirementsDataGrid.ScrollIntoView((RequirementSingleView)RequirementsDataGrid.SelectedItems[0]);
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

        private async Task RefreshHelpers()
        {
            await RefreshHelpers(true, true,
                GetScrollViewer(RequirementsDataGrid).HorizontalOffset,
                GetScrollViewer(RequirementsDataGrid).VerticalOffset);
        }

        private async Task RefreshHelpers(bool horizontalRefresh, bool verticalRefresh, double horizontalOffset, double verticalOffset, double verticalChange = 0)
        {
            if (horizontalRefresh)
            {
                new List<ScrollViewer>()
                {
                    GetScrollViewer(ReqHelperTop),
                    GetScrollViewer(ReqHelperBottom)
                }.ForEach(x => x.ScrollToHorizontalOffset(horizontalOffset));
            }

            if (verticalRefresh)
            {
                var datagridHeight = RequirementsDataGrid.ActualHeight;
                var rowHeight = RequirementsDataGrid.RowHeight;
                var firstRow = (int)verticalOffset;
                var lastRow = firstRow + (int)(datagridHeight / rowHeight) - 1;

                if (reqsCollection.AsParallel().Any(x => x.IsVisible == false))
                {
                    var firstVisible = RequirementsDataGrid.Items
                        .IndexOf(RequirementsDataGrid.Items.Cast<RequirementSingleView>()
                            .First(x => x.IsVisible == true));
                    var lastVisible = RequirementsDataGrid.Items
                        .IndexOf(RequirementsDataGrid.Items.Cast<RequirementSingleView>()
                            .Last(x => x.IsVisible == true));

                    if (lastVisible < lastRow)
                        lastRow = lastVisible + 1;

                    if (verticalChange < 0 && firstRow < firstVisible)
                    {
                        var scrollViewer = GetScrollViewer(RequirementsDataGrid);
                        scrollViewer.ScrollToVerticalOffset(firstVisible);
                    }
                    else if (verticalChange > 0 && lastVisible < firstRow)
                    {
                        var scrollViewer = GetScrollViewer(RequirementsDataGrid);
                        scrollViewer.ScrollToVerticalOffset(lastVisible);
                    }
                }

                var selectedTCs = new List<int>();
                foreach (int TC in AllTCsListBox.SelectedItems)
                    selectedTCs.Add(TC);


                Parallel.ForEach(selectedTCs, TCSelected =>
                {
                    var TCSelectedString = TCSelected.ToString();
                    if (ReqTopHelperData[0].ContainsKey(TCSelectedString))
                    {
                        var firstOccurence = RequirementsDataGrid.Items.Cast<RequirementSingleView>()
                            .TakeWhile(x => x.TCIDsValue.Contains(TCSelected) != true)
                            .Count();

                        if (firstOccurence < firstRow)
                            ReqTopHelperData[0][TCSelectedString] = true;
                        else
                            ReqTopHelperData[0][TCSelectedString] = false;
                    }

                    if (ReqBottomHelperData[0].ContainsKey(TCSelectedString))
                    {
                        var lastOccurence = RequirementsDataGrid.Items.Cast<RequirementSingleView>().Count() - RequirementsDataGrid.Items.Cast<RequirementSingleView>()
                          .Reverse()
                          .TakeWhile(x => x.TCIDsValue.Contains(TCSelected) != true)
                          .Count();

                        if (lastOccurence > lastRow)
                            ReqBottomHelperData[0][TCSelectedString] = true;
                        else
                            ReqBottomHelperData[0][TCSelectedString] = false;
                    }
                });

            }
            RequirementsDataGrid.UpdateLayout();
            ReqHelperTop.Items.Refresh();
            ReqHelperBottom.Items.Refresh();
        }

        private async void RequirementsDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            await RefreshHelpers(e.HorizontalChange != 0, e.VerticalChange != 0, e.HorizontalOffset, e.VerticalOffset, e.VerticalChange);
        }

        private void RequirementsDataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            for (int i = 0; i < RequirementsDataGrid.Columns.Count; i++)
            {
                if (i < ReqHelperTop.Columns.Count)
                {
                    ReqHelperTop.Columns[i].Width = RequirementsDataGrid.Columns[i].Width;
                    ReqHelperBottom.Columns[i].Width = RequirementsDataGrid.Columns[i].Width;
                }
            }
        }

        private async void ValidFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilteredTCs();
            CollectionViewSource.GetDefaultView(reqsCollection).Refresh();
            GetScrollViewer(RequirementsDataGrid)
                .ScrollToVerticalOffset(RequirementsDataGrid.Items.IndexOf(RequirementsDataGrid.Items
                    .Cast<RequirementSingleView>()
                    .Skip(1)
                    .FirstOrDefault(x => x.Type == ReqComparer.Requirement.Types.Head)));
            await ShowOneChapter(0);
        }

        private void TCFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(FilteredTCs).Refresh();
        }
        private async Task PushSelectedTCsUp()
        {
            var selectedTcs = AllTCsListBox
                .SelectedItems
                .Cast<int>()
                .Reverse()
                .ToList();

            if (selectedTcs.Count == 1 && FilteredTCs.IndexOf(selectedTcs[0]) == 0)
                return;

            var tempFilter = TCFilter.Text;
            TCFilter.Text = "";

            selectedTcs
                .ForEach(x => FilteredTCs.Move(FilteredTCs.IndexOf(x), 0));

            TCFilter.Text = tempFilter;
            AllTCsListBox.UpdateLayout();
            await RefreshHelpers();
        }
        private async void AllTCsListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await PushSelectedTCsUp();
        }

        private async void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            TCFilter.Text = "";
            RefreshRequirementsDataGrid();
            AllTCsListBox.SelectedItems.Clear();
            await ShowOneChapter(0);
        }

        public async void ChapterSelectButton_Click(object sender,RoutedEventArgs e)
        {
            var chapterSelectionWindow = new ChapterSelectionWindow(reqsCollection);
            chapterSelectionWindow.ShowDialog();
            if (chapterSelectionWindow.DialogResult == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                if (chapterSelectionWindow.ClearPreviousTCs)
                {
                    RefreshRequirementsDataGrid();
                    AllTCsListBox.SelectedItems.Clear();
                    RequirementsDataGrid.UpdateLayout();
                }

                var selectedChapter = chapterSelectionWindow.Answer;
                Console.WriteLine(selectedChapter.chapter);
                await ShowOneChapter(selectedChapter.id, chapterSelectionWindow.SelectTCs);
                ChapterNameTextBlock.Text = selectedChapter.chapter;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        private void RequirementsDataGrid_ColumnDisplayIndexChanged(object sender, DataGridColumnEventArgs e)
        {
            var topHelper = ReqHelperTop.Columns
                .FirstOrDefault(x => x.Header.ToString() == e.Column.Header.ToString());

            if(topHelper!=null)
                topHelper.DisplayIndex = e.Column.DisplayIndex;

            var bottomHelper = ReqHelperBottom.Columns
                .FirstOrDefault(x => x.Header.ToString() == e.Column.Header.ToString());

            if(bottomHelper!=null)
                bottomHelper.DisplayIndex = e.Column.DisplayIndex;
        }

        private async void ChapterClear_Click(object sender, RoutedEventArgs e)
        {
            await ShowOneChapter(0);
        }

        private void Helper_GotFocus(object sender, RoutedEventArgs e)
        {
            var helper = (DataGrid)sender;
            helper.SelectedItems.Clear();

            RequirementsDataGrid.Focus();
        }

        private void ValidIn_Loaded(object sender, RoutedEventArgs e)
        {
            ValidIn.SelectedIndex = 0;
            ValidIn.UpdateLayout();
        }

        private async void TCFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            var tcs = TCFilter.Text
                .Trim()
                .Split(' ')
                .Select(x => int.Parse(x))
                .ToList();
            await SelectMultipleTCs(tcs);
        }

        private async void SelectTCForReqsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTCs = RequirementsDataGrid.SelectedItems
                .Cast<RequirementSingleView>()
                .SelectMany(x => x.TCIDsValue)
                .Distinct()
                .ToList();

            await SelectMultipleTCs(selectedTCs);

            await PushSelectedTCsUp();
        }
    }
}
