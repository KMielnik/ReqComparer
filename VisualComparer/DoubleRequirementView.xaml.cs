using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VisualComparer
{
    /// <summary>
    /// Interaction logic for DoubleRequirementView.xaml
    /// </summary>
    public partial class DoubleRequirementView : UserControl
    {
        public ObservableCollection<RequirementDoubleView> reqsCollection { get; set; }
        private ObservableCollection<ReqComparer.Requirement> basicReqs;
        public DoubleRequirementView(ObservableCollection<ReqComparer.Requirement> basicReqs)
        {
            InitializeComponent();
            reqsCollection = new ObservableCollection<RequirementDoubleView>();
            this.basicReqs = basicReqs;
            this.basicReqs.CollectionChanged += BasicReqs_CollectionChanged;

            SetRequirementsDataGrid(RequirementsDataGridRight);
            SetRequirementsDataGrid(RequirementsDataGridLeft);

            RefreshRequirementsDataGrid();
        }

        private void RefreshRequirementsDataGrid()
        {
            reqsCollection.Clear();
            foreach (var req in basicReqs)
                reqsCollection.Add(new RequirementDoubleView(req));

            var allTCs = reqsCollection
                .SelectMany(x => x.TCIDsValue)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            LeftTCComboBox.Items.Clear();
            RightTCComboBox.Items.Clear();

            allTCs.ForEach(x =>
            {
                LeftTCComboBox.Items.Add(x);
                RightTCComboBox.Items.Add(x);
            });
        }

        private void BasicReqs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshRequirementsDataGrid();
        }

        private void SetRequirementsDataGrid(DataGrid dataGrid)
        {
            dataGrid.ItemsSource = reqsCollection;
            dataGrid.Columns.Clear();

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new Binding(nameof(RequirementDoubleView.ID)),
                IsReadOnly = true
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Text",
                Binding = new Binding(nameof(RequirementDoubleView.TextIntended)),
                IsReadOnly = true,
                Width = 500
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "TCs",
                Binding = new Binding(nameof(RequirementDoubleView.TCStringified))
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Functional Variants",
                Binding = new Binding(nameof(RequirementDoubleView.FVariants))
            });

            setColorTriggers(dataGrid);
            setBoldDataTrigger(dataGrid);
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

        private void setColorTriggers(DataGrid dataGrid)
        {
            var isRight = dataGrid.Name.Contains("Right");
            string highlightedRowPropertyName;
            if (isRight)
                highlightedRowPropertyName = nameof(RequirementDoubleView.HighlightedRowRight);
            else
                highlightedRowPropertyName = nameof(RequirementDoubleView.HighlightedRowLeft);

            Brush brushColor;
            if (isRight)
                brushColor = Brushes.Indigo;
            else
                brushColor = Brushes.DarkGreen;

            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding(highlightedRowPropertyName),
                Value = "true"
            };

            dataTrigger.Setters.Add(new Setter()
            {
                Property = BackgroundProperty,
                Value = brushColor
            });
            dataTrigger.Setters.Add(new Setter()
            {
                Property = ForegroundProperty,
                Value = Brushes.White
            });

            dataGrid.RowStyle.Triggers.Add(dataTrigger);
        }

        private void TCComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            foreach (var req in reqsCollection)
            {
                if (comboBox.Name.Contains("Left"))
                    req.HighlightedRowLeft = false;
                if (comboBox.Name.Contains("Right"))
                    req.HighlightedRowRight = false;
            }
            if (comboBox.SelectedValue is int)
            {
                var selectedTC = (int)comboBox.SelectedValue;

                foreach (var req in reqsCollection)
                {
                    if (req.TCIDsValue.Contains(selectedTC))
                    {
                        if (comboBox.Name.Contains("Left"))
                            req.HighlightedRowLeft = true;
                        if (comboBox.Name.Contains("Right"))
                            req.HighlightedRowRight = true;
                    }
                }
            }

            RequirementsDataGridLeft.Items.Refresh();
            RequirementsDataGridRight.Items.Refresh();

            CountReqsOccurences();
        }

        private void CountReqsOccurences()
        {
            int leftCount = 0, rightCount = 0, bothCount = 0;
            foreach (var req in reqsCollection)
            {
                if (req.HighlightedRowLeft && req.HighlightedRowRight)
                    bothCount++;
                if (req.HighlightedRowLeft)
                    leftCount++;
                if (req.HighlightedRowRight)
                    rightCount++;
            }
            ReqsCoveredLeft.Text = leftCount.ToString();
            ReqsCoveredCenter.Text = bothCount.ToString();
            ReqsCoveredRight.Text = rightCount.ToString();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 3));
            e.Handled = true;
        }
    }
}
