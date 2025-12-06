using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class FindPCRTestAndPerson : Window
    {
        private readonly MainWindow mainW;
        public FindPCRTestAndPerson(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var (test, person) = mainW.guiController.FindPCRTest(TestIDBox.Value ?? default);
                mainW.baseOperation.SetNewResult((person != null) ? [person] : null, (test != null) ? [test] : null);
                mainW.ChangableContent.Content = mainW.baseOperation.Content;
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

