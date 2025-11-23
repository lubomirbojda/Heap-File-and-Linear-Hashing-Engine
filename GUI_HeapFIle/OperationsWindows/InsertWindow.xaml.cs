using AUS2_S2.HeapFile;
using AUS2_S2.HeapFile.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for InsertWindow.xaml
    /// </summary>
    public partial class InsertWindow : Window
    {
        private BHeapFile<Person> file;
        public InsertWindow(BHeapFile<Person> file)
        {
            InitializeComponent();
            this.file = file;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            var person = new Person(FirstNameBox.Text, LastNameBox.Text, DateOnly.FromDateTime(BirthdatePicker.SelectedDate ?? default), IDBox.Text);
            file.Insert(person);

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
