using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class FindPCRTestToEdit : Window
    {
        private readonly MainWindow mainW;
        public FindPCRTestToEdit(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var test = mainW.guiController.FindPCRTestToEdit(TestIDBox.Value ?? default);
                if (test != null)
                {
                    mainW.pcrTestEdit.SetNewResult(test);
                    mainW.pcrTestEdit.SetTestCode(test.TestCode);
                    mainW.ChangableContent.Content = mainW.pcrTestEdit.Content;
                }
                else
                {
                    MessageBox.Show("Test not found.", "Failed", MessageBoxButton.OK);
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


