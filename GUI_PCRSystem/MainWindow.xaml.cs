using AUS2_S2.GUIControllers;
using GUI_PCRSystem.OperationsWindows;
using GUI_PCRSystem.OperationsWindows.ContentWindows;
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
        public PersonEdit personEdit { get; }
        public PCRTestEdit pcrTestEdit { get; }
        public MainWindow()
        {
            InitializeComponent();
            personEdit = new(this);
            pcrTestEdit = new(this);
        }

        private void InsertPCRTest_Click(object sender, RoutedEventArgs e)
        {
            var window = new InsertPCRTest(this);
            window.ShowDialog();
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
            var window = new InsertPerson(this);
            window.ShowDialog();
        }

        private void DeletePCRTest_Click(object sender, RoutedEventArgs e)
        {
            var deleteWindow = new DeletePCRTest(this);
            deleteWindow.ShowDialog();
        }

        private void DeletePerson_Click(object sender, RoutedEventArgs e)
        {
            var deleteWindow = new DeletePerson(this);
            deleteWindow.ShowDialog();
        }

        private void FindPersonToEdit_Click(object sender, RoutedEventArgs e)
        {
            var personEdit = new FindPersonToEdit(this);
            personEdit.ShowDialog();
        }

        private void FindPCRTestToEdit_Click(object sender, RoutedEventArgs e)
        {
            var pctTestEdit = new FindPCRTestToEdit(this);
            pctTestEdit.ShowDialog();
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

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}