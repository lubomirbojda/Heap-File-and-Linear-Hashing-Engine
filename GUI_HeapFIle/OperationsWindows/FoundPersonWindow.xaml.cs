using AUS2_S2.HeapFile.DataClasses;
using System.Windows;

namespace GUI_HeapFIle.OperationsWindows
{
    /// <summary>
    /// Interaction logic for FoundPersonWindow.xaml
    /// </summary>
    public partial class FoundPersonWindow : Window
    {
        public FoundPersonWindow(Person person)
        {
            InitializeComponent();
            PersonBox.Text = person.GetPrintString(); 
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
