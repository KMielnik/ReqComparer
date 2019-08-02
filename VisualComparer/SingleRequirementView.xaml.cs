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
    /// Interaction logic for SingleRequirementView.xaml
    /// </summary>
    public partial class SingleRequirementView : UserControl
    {
        public ObservableCollection<RequirementSingleView> reqsCollection { get; set; }
        private ObservableCollection<ReqComparer.Requirement> basicReqs;
        private ObservableCollection<int> AllTCs;

        private IEnumerable<Brush> GetBrushesEnumerable()
        {
            yield return Brushes.Red;
            yield return Brushes.Blue;
            yield return Brushes.Green;
            yield return Brushes.Purple;
            yield return Brushes.Orange;
            yield return Brushes.Navy;
        }
        public SingleRequirementView(ObservableCollection<ReqComparer.Requirement> basicReqs)
        {
            InitializeComponent();
            reqsCollection = new ObservableCollection<RequirementSingleView>();
            AllTCs = new ObservableCollection<int>();
            this.basicReqs = basicReqs;
            this.basicReqs.CollectionChanged += BasicReqs_CollectionChanged;

            RequirementsDataGrid.ItemsSource = reqsCollection;
            AllTCsListBox.ItemsSource = AllTCs;

            SetReqDataGrid();
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
                Binding = new Binding(nameof(RequirementDoubleView.TCStringified))
            });

            RequirementsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Functional Variants",
                Binding = new Binding(nameof(RequirementDoubleView.FVariants)),
                Width = 200
            });

        }

        private void SetReqDataGrid()
        {
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

            setBoldDataTrigger(RequirementsDataGrid);
            RefreshRequirementsDataGrid();
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

            var brushes = GetBrushesEnumerable().ToArray();
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
                    Value = brushes[(actualBrush) % brushes.Length]
                });

                dataTrigger.Setters.Add(new Setter()
                {
                    Property = ForegroundProperty,
                    Value = brushes[(actualBrush) % brushes.Length]
                });

                dataTrigger.Setters.Add(new Setter()
                {
                    Property = BorderBrushProperty,
                    Value = brushes[(actualBrush++) % brushes.Length]
                });

                TCColumn.CellStyle = new Style();
                TCColumn.CellStyle.Triggers.Add(dataTrigger);

                RequirementsDataGrid.Columns.Add(TCColumn);
            }

        }
    }
}
