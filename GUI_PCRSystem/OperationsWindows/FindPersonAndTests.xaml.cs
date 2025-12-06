using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class FindPersonAndTests : Window
    {
        private readonly MainWindow mainW;
        public FindPersonAndTests(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var (person, tests) = mainW.guiController.FindPerson(PersonIDBox.Text);
                mainW.baseOperation.SetNewResult((person != null) ? [person] : null, tests);
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
