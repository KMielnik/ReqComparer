using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ReqComparer;
using System.Deployment.Application;
using System.Threading;
using System.Windows.Media;

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

        private void DisplayChangeLog()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return;

            Title += " - " + ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();

            if (!ApplicationDeployment.CurrentDeployment.IsFirstRun)
                return;

            new ChangelogWindow().Show();
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
            DisplayChangeLog();

            await LoadReqsFromCache();
            if (await parser.CheckForUpdates())
            {
                UpdateButton.Content = "Update Available!";
                UpdateButton.Background = Brushes.Red;
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to replace actual cached file with latest one from server?", "Confirm replacement.",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                await parser.DownloadNewestVersion();
                await LoadReqsFromCache();
                UpdateButton.Content = "Get latest version";
                UpdateButton.Background = Brushes.Gainsboro;
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
