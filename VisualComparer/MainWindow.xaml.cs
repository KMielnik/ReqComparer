using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

        private async Task LoadReqsFromCache(string filename = "cached_reqs.json")
        {
            try
            {
                var dataFromCache = await parser.GetReqsFromCachedFile(filename);

                reqsCollection.Clear();
                await reqsCollection.AddRangeNotifyFinishAsync(dataFromCache.reqs);

                ActualExportDateTextBlock.Text = $"{dataFromCache.exportDate.ToShortDateString()} {dataFromCache.exportDate.ToShortTimeString()}";
            }
            catch
            {
                MessageBox.Show("Error while loading the cached file.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReqsFromCache();
            if (await parser.CheckForUpdates())
                UpdateButton.Content = "Update Available!";
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to replace actual cached file with newest one from server?", "Confirm replacement.",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                await parser.DownloadNewestVersion();
                await LoadReqsFromCache();
                UpdateButton.Content = "Get newest version";
            }
        }

        private void ExporterButton_Click(object sender, RoutedEventArgs e)
        {
            var exporterWindow = new DoorsExporterWindow(parser);
            exporterWindow.Show();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "JSON Files (*.json)|*.json"
            };

            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            if (dlg.ShowDialog() == true)
                await LoadReqsFromCache(dlg.FileName);
        }
    }
}
