using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using ReqComparer;

namespace VisualComparer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ReqParser parser;
        public ObservableCollection<Requirement> reqsCollection { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            parser = new ReqParser();
            reqsCollection = new ObservableCollection<Requirement>();
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            await parser.LoadFromFile("d.htm");
            var reqs = parser.GetRequiermentsList();

            reqsCollection.Clear();
            reqs.ForEach(x => reqsCollection.Add(x));

            var allTCs = reqs
                .SelectMany(x => x.TCIDsValue)
                .Distinct()
                .OrderBy(x=>x)
                .ToList();

            LeftTCComboBox.Items.Clear();
            RightTCComboBox.Items.Clear();

            allTCs.ForEach(x =>
            {
                LeftTCComboBox.Items.Add(x);
                RightTCComboBox.Items.Add(x);
            });
        }

        private void RequirementsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            dataGrid.ItemsSource = reqsCollection;
            dataGrid.Columns.Clear();

            DataGridTextColumn reqID = new DataGridTextColumn();
            reqID.Header = "ID";
            reqID.Binding = new Binding(nameof(Requirement.ID));
            reqID.IsReadOnly = true;
            dataGrid.Columns.Add(reqID);

            DataGridTextColumn reqText = new DataGridTextColumn();
            reqText.Header = "Text";
            reqText.Binding = new Binding(nameof(Requirement.TextIntended));
            reqText.IsReadOnly = true;
            reqText.Width = 500;
            dataGrid.Columns.Add(reqText);

            DataGridTextColumn tcIDs = new DataGridTextColumn();
            tcIDs.Header = "TCs";
            tcIDs.Binding = new Binding(nameof(Requirement.TCStringified));
            dataGrid.Columns.Add(tcIDs);

            setColorTriggers(dataGrid);
            setBoldDataTrigger(dataGrid);
        }

        private void setBoldDataTrigger(DataGrid datagrid)
        {
            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding(nameof(Requirement.IsImportant)),
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
                highlightedRowPropertyName = nameof(Requirement.HighlightedRowRight);
            else
                highlightedRowPropertyName = nameof(Requirement.HighlightedRowLeft);

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
            if (!(comboBox.SelectedValue is int))
                return;

            int selectedTC = (int)comboBox.SelectedValue;

            foreach (var req in reqsCollection)
            {
                if (req.TCIDsValue.Contains(selectedTC))
                {
                    if(comboBox.Name.Contains("Left"))
                        req.HighlightedRowLeft = true;
                    if (comboBox.Name.Contains("Right"))
                        req.HighlightedRowRight = true;
                }
            }
            RequirementsDataGridLeft.Items.Refresh();
            RequirementsDataGridRight.Items.Refresh();

            CountReqsOccurences();
        }

        private void CountReqsOccurences()
        {
            int leftCount = 0, rightCount = 0, bothCount = 0;
            foreach(var req in reqsCollection)
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
