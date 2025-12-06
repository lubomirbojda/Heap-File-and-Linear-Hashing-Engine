using System.Windows;

namespace GUI_PCRSystem.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FindPersonAndTests.xaml
    /// </summary>
    public partial class InsertPCRTest : Window
    {
        private readonly MainWindow mainW;
        public InsertPCRTest(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var (result, message) = mainW.guiController.InsertPCRTest(
                        DateTimePicker.Value ?? default,
                        PersonIDTextBox.Text,
                        ResultCheckBox.IsChecked ?? default,
                        ValueBox.Value ?? default,
                        NoteTextBox.Text
                    );

                if (result)
                    MessageBox.Show(message, "Success", MessageBoxButton.OK);
                else
                    MessageBox.Show(message, "Failed", MessageBoxButton.OK);
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

