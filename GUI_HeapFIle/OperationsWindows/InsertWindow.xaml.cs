using AUS2_S2.DataClasses;
using AUS2_S2.GUIControllers;
using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for InsertWindow.xaml
    /// </summary>
    public partial class InsertWindow : Window
    {
        private readonly PersonHF_GUIController guiController;
        public InsertWindow(PersonHF_GUIController guiCont)
        {
            InitializeComponent();
            guiController = guiCont;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            var person = new Person(FirstNameBox.Text, LastNameBox.Text, DateOnly.FromDateTime(BirthdatePicker.SelectedDate ?? default), IDBox.Text);
            guiController.Insert(person);

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
