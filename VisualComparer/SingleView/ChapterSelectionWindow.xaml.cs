using ReqComparer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for ChapterSelectionWindow.xaml
    /// </summary>
    public partial class ChapterSelectionWindow : Window
    {
        public ObservableCollection<(string chapter, int id)> Reqs { get; set; }
        public (string chapter, int id) Answer { get; private set; }
        public ChapterSelectionWindow(ObservableCollection<RequirementSingleView> requirements)
        {
            InitializeComponent();
            DataContext = this;

            Reqs = new ObservableCollection<(string chapter, int id)>();

            requirements
                .Where(x => x.Type == Requirement.Types.Head)
                .Select(x => (chapter: Regex.Match(x.Text, @"^\d+\.+[\d.]+").Value, id: x.IDValue))
                .Where(x => !string.IsNullOrWhiteSpace(x.chapter))
                .ToList()
                .ForEach(x => Reqs.Add(x));

            var ChapterView = CollectionViewSource.GetDefaultView(Reqs);
            ChapterView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(ChapterFilterTextBox.Text))
                    return true;

                return (x as (string chapter, int id)?).Value.chapter.StartsWith(ChapterFilterTextBox.Text);
            };
        }

        private void ReqsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ReqsListView.SelectedItem is null)
                return;

            Answer = (ValueTuple<string, int>)ReqsListView.SelectedItem;
            DialogResult = true;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReqsListView.SelectedItem == null)
            {
                MessageBox.Show("Please select chapter.");
                return;
            }

            Answer = (ValueTuple<string, int>)ReqsListView.SelectedItem;
            DialogResult = true;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(Reqs).Refresh();
        }
    }

    public class TupleDisplayChapterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tuple = value as (string chapter, int id)?;

            if (tuple == null)
                return null;
            return tuple.Value.chapter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
