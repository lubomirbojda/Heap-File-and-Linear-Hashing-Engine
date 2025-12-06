using AUS2_S2.GUIControllers;
using GUI_PCRSystem.OperationsWindows;
using System.Windows;

namespace GUI_PCRSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PCRSytemGUIController? guiController { get; }
        public BaseOperation baseOperation { get; } = new();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InsertPCRTest_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void FindPersonAndTests_Click(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindPersonAndTests(this);
            findWindow.ShowDialog();
        }

        private void FindPCRTestAndPerson_Click(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindPCRTestAndPerson(this);
            findWindow.ShowDialog();
        }

        private void InsertPerson_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void DeletePCRTest_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void DeletePerson_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FindPersonToEdit_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void FindPCRTestToEdit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ChooseOperation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}