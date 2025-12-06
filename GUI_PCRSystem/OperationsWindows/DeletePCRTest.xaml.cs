using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class DeletePCRTest : Window
    {
        private readonly MainWindow mainW;
        public DeletePCRTest(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var result = mainW.guiController.DeletePCRTest(TestCodeBox.Value ?? default);

                if (result)
                    MessageBox.Show("Test deleted successfully.", "Success", MessageBoxButton.OK);
                else
                    MessageBox.Show("Test not deleted.", "Failed", MessageBoxButton.OK);
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}



