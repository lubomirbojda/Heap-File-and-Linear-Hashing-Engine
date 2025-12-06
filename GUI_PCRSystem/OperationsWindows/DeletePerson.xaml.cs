using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class DeletePerson : Window
    {
        private readonly MainWindow mainW;
        public DeletePerson(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var result = mainW.guiController.DeletePerson(PersonIDBox.Text);

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




