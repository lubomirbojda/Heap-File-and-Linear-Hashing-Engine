using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class InsertPerson : Window
    {
        private readonly MainWindow mainW;
        public InsertPerson(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var id = mainW.guiController.InsertPerson(
                    FirstNameTextBox.Text,
                    LastNameTextBox.Text,
                    DateOnly.FromDateTime(DatePicker.SelectedDate ?? default)
                    );

                MessageBox.Show("Inserted person id: " + id, "Person insertion", MessageBoxButton.OK);
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}


