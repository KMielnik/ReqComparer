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
        readonly ReqParser parser;
        public ListWithNotifications<Requirement> reqsCollection { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            parser = new ReqParser();
            reqsCollection = new ListWithNotifications<Requirement>();

            RequirementsArea.Content = new SingleRequirementView(reqsCollection);
        }

        private async Task LoadReqsFromFile(string filename)
        {
            await parser.LoadFromFile(filename);
            var reqs = parser.GetRequiermentsList();

            reqsCollection.Clear();
            reqsCollection.AddRangeNotifyFinish(reqs);
        }

        private async void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            //await LoadReqsFromFile("d.htm");
            var reqs = await parser.GetReqsFromCachedFile();

            reqsCollection.Clear();
            reqsCollection.AddRangeNotifyFinish(reqs);
        }

        private void SwitchViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequirementsArea.Content is SingleRequirementView)
                RequirementsArea.Content = new DoubleRequirementView(reqsCollection);
            else
                RequirementsArea.Content = new SingleRequirementView(reqsCollection);
        }

        private async void FileSelector_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".htm",
                Filter = "HTML Files (*.htm)|*.htm"
            };

            if (dlg.ShowDialog() == true)
                await LoadReqsFromFile(dlg.FileName);
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files[0].Contains(".htm"))
                await LoadReqsFromFile(files[0]);
            else
                MessageBox.Show("Invalid file format.\nTry with .htm next time.");
        }
    }
}
