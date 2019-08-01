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

        public MainWindow()
        {
            InitializeComponent();
            parser = new ReqParser();
            reqsCollection = new ObservableCollection<Requirement>();

            RequirementsArea.Content = new DoubleRequirementView(reqsCollection);
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            await parser.LoadFromFile("d.htm");
            var reqs = parser.GetRequiermentsList();

            reqsCollection.Clear();
            reqs.ForEach(x => reqsCollection.Add(x));
        }
    }
}
