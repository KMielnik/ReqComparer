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

        private SingleRequirementView singleRequirementView;
        private DoubleRequirementView doubleRequirementView;

        public MainWindow()
        {
            InitializeComponent();
            parser = new ReqParser();
            reqsCollection = new ObservableCollection<Requirement>();

            singleRequirementView = new SingleRequirementView(reqsCollection);
            doubleRequirementView = new DoubleRequirementView(reqsCollection);

            RequirementsArea.Content = singleRequirementView;
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            await parser.LoadFromFile("d.htm");
            var reqs = parser.GetRequiermentsList();

            reqsCollection.Clear();
            reqs.ForEach(x => reqsCollection.Add(x));
        }

        private void SwitchViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequirementsArea.Content == singleRequirementView)
                RequirementsArea.Content = doubleRequirementView;
            else
                RequirementsArea.Content = singleRequirementView;
        }
    }
}
