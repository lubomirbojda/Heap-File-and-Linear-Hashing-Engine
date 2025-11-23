using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.DataClasses;
using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for DeleteWindow.xaml
    /// </summary>
    public partial class DeleteWindow : Window
    {
        private BHeapFile<Person> file;
        public DeleteWindow(BHeapFile<Person> file)
        {
            InitializeComponent();
            this.file = file;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(AddressBox.Text, out long address))
            {
                var result = file.Delete(address, new Person(IDBox.Text));
                if (result)
                    MessageBox.Show("Person deleted successfully.", "Success", MessageBoxButton.OK);
                else
                    MessageBox.Show("Person not deleted.", "Failed", MessageBoxButton.OK);
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
