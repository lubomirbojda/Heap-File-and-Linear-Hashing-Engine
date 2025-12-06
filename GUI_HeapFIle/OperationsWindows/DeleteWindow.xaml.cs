using AUS2_S2.DataClasses;
using AUS2_S2.GUIControllers;
using AUS2_S2.HeapFile;
using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for DeleteWindow.xaml
    /// </summary>
    public partial class DeleteWindow : Window
    {
        private readonly PersonHF_GUIController guiController;
        public DeleteWindow(PersonHF_GUIController guiCont)
        {
            InitializeComponent();
            guiController = guiCont;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(AddressBox.Text, out long address))
            {
                var result = guiController.Delete(address, new Person(IDBox.Text));
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
