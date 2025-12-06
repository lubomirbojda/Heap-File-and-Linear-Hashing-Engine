using AUS2_S2.DataClasses;
using System.Windows;
using System.Windows.Controls;

namespace GUI_PCRSystem.OperationsWindows.ContentWindows
{
    /// <summary>
    /// Interaction logic for PersonEdit.xaml
    /// </summary>
    public partial class PCRTestEdit : Window
    {
        private readonly MainWindow mainW;
        private int testCode;
        public PCRTestEdit(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        public void SetTestCode(int code)
        {
            testCode = code;
        }

        public void SetNewResult(PCRTest test)
        {
            DateTimePicker.Value = test.TestDateTime;
            PersonIDTextBox.Text = test.PersonId;
            ResultCheckBox.IsChecked = test.Result;
            ValueBox.Value = test.Value;
            NoteTextBox.Text = test.Note;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var result = mainW.guiController.UpdatePCRTest(new PCRTest(
                        DateTimePicker.Value ?? default,
                        PersonIDTextBox.Text,
                        testCode,
                        ResultCheckBox.IsChecked ?? default,
                        ValueBox.Value ?? default,
                        NoteTextBox.Text
                    ));
                if (result)
                    MessageBox.Show("PCR test updated.", "Success", MessageBoxButton.OK);
                else
                    MessageBox.Show("PCR test not updated.", "Failed", MessageBoxButton.OK);
            }
        }
    }
}

