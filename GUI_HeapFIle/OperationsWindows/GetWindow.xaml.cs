using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.DataClasses;
using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for GetWindow.xaml
    /// </summary>
    public partial class GetWindow : Window
    {
        private BHeapFile<Person> file;
        public GetWindow(BHeapFile<Person> file)
        {
            InitializeComponent();
            this.file = file;
        }

        private void GetButton_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(AddressBox.Text, out long address))
            {
                var person = file.Get(address, new Person(IDBox.Text));
                if (person == null)
                {
                    MessageBox.Show("Person not found.", "Failed", MessageBoxButton.OK);
                }
                else
                {
                    var getWindow = new FoundPersonWindow(person);
                    getWindow.ShowDialog();
                }
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
