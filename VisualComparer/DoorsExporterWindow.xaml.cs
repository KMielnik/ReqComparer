using ReqComparer;
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
using System.Windows.Shapes;

namespace VisualComparer
{
    /// <summary>
    /// Interaction logic for DoorsExporterWindow.xaml
    /// </summary>
    public partial class DoorsExporterWindow : Window
    {
        readonly ReqParser parser;
        public DoorsExporterWindow(ReqParser parser)
        {
            InitializeComponent();
            this.parser = parser;
        }

        private async void InputButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".htm",
                Filter = "HTML Files (*.htm)|*.htm"
            };

            if (dlg.ShowDialog() == true)
            {
                InputTextBox.Text = dlg.FileName;
                InputTextBox.CaretIndex = InputTextBox.Text.Length;
            }
        }

        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".json",
                Filter = "Json Files (*.json)|*.json"
            };

            if (dlg.ShowDialog() == true)
            {
                OutputTextBox.Text = dlg.FileName;
                OutputTextBox.CaretIndex = OutputTextBox.Text.Length;
            }
        }

        private async void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            await parser.ParseToFileAsync(
                new Progress<string>(x => ProgressTextBlock.Text = x),
                InputTextBox.Text,
                OutputTextBox.Text);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = ReqParser.defaultCachedFileName;
            OutputButton.IsEnabled = false;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "";
            OutputButton.IsEnabled = true;
        }
    }
}
