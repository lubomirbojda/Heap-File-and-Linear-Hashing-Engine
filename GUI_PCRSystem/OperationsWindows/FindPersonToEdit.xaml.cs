using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class FindPersonToEdit : Window
    {
        private readonly MainWindow mainW;
        public FindPersonToEdit(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var person = mainW.guiController.FindPersonToEdit(PersonIDBox.Text);
                if (person != null)
                {
                    mainW.personEdit.SetNewResult(person);
                    mainW.personEdit.SetPersonID(person.Id);
                    mainW.ChangableContent.Content = mainW.personEdit.Content;
                }
                else
                {
                    MessageBox.Show("Person not found.", "Failed", MessageBoxButton.OK);
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

