using AUS2_S2.DataClasses;
using System.Windows;
using System.Windows.Controls;

namespace GUI_PCRSystem.OperationsWindows.ContentWindows
{
    /// <summary>
    /// Interaction logic for PersonEdit.xaml
    /// </summary>
    public partial class PersonEdit : Window
    {
        private readonly MainWindow mainW;
        private Person? person;
        public PersonEdit(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        public void SetNewResult(Person personToEdit)
        {
            person = personToEdit;
            FirstNameTextBox.Text = person.FirstName;
            LastNameTextBox.Text = person.LastName;
            DatePicker.SelectedDate = person.BirthDate.ToDateTime(TimeOnly.MinValue);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null && person != null)
            {
                person.SetNewFirstName(FirstNameTextBox.Text);
                person.SetNewLastName(LastNameTextBox.Text);
                person.SetNewBirthdate(DateOnly.FromDateTime(DatePicker.SelectedDate ?? default));
                var result = mainW.guiController.UpdatePerson(person);
                if (result)
                    MessageBox.Show("Person updated.", "Success", MessageBoxButton.OK);
                else
                    MessageBox.Show("Person not updated.", "Failed", MessageBoxButton.OK);
            }
        }
    }
}
