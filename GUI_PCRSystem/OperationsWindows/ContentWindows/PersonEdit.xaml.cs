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
        private string personID = "";
        public PersonEdit(MainWindow mainWindow)
        {
            InitializeComponent();
            mainW = mainWindow;
        }

        public void SetPersonID(string id)
        {
            personID = id;
        }

        public void SetNewResult(Person person)
        {
            FirstNameTextBox.Text = person.FirstName;
            LastNameTextBox.Text = person.LastName;
            DatePicker.SelectedDate = person.BirthDate.ToDateTime(TimeOnly.MinValue);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainW.guiController != null)
            {
                var result = mainW.guiController.UpdatePerson(new Person(
                    FirstNameTextBox.Text,
                    LastNameTextBox.Text,
                    DateOnly.FromDateTime(DatePicker.SelectedDate ?? default),
                    personID
                ));
                if (result)
                    MessageBox.Show("Person updated.", "Success", MessageBoxButton.OK);
                else
                    MessageBox.Show("Person not updated.", "Failed", MessageBoxButton.OK);
            }
        }
    }
}
