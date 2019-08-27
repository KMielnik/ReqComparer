using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace VisualComparer
{
    /// <summary>
    /// Interaction logic for ChangelogWindow.xaml
    /// </summary>
    public partial class ChangelogWindow : Window
    {
        private const string changelogPath = @"\\10.128.3.1\DFS_data_SSC_FS_Images-SSC\KMIM\MiniDoorsy\Data\changelog.rtf";
        public ChangelogWindow()
        {
            InitializeComponent();
        }

        private void RichTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var richTextBox = (RichTextBox)sender;

            try
            {
                var changelogText = File.ReadAllBytes(changelogPath);
                MemoryStream stream = new MemoryStream(changelogText);

                richTextBox.Selection.Load(stream, DataFormats.Rtf);
            }
            catch
            {
                MessageBox.Show($"Error with opening changelog, to see it manually go to {changelogPath}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
