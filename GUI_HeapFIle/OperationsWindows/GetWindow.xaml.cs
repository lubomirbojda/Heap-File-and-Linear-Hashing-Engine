using AUS2_S2.DataClasses;
using AUS2_S2.GUIControllers;
using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for GetWindow.xaml
    /// </summary>
    public partial class GetWindow : Window
    {
        private readonly PersonHF_GUIController guiController;
        public GetWindow(PersonHF_GUIController guiCont)
        {
            InitializeComponent();
            guiController = guiCont;
        }

        private void GetButton_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(AddressBox.Text, out long address))
            {
                var person = guiController.Get(address, new Person(IDBox.Text));
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
