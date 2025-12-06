using AUS2_S2.GUIControllers;
using GUI_PCRSystem.Menu;
using GUI_PCRSystem.OperationsWindows;
using GUI_PCRSystem.OperationsWindows.ContentWindows;
using GUI_PCRSystem.OperationsWindows.PrintFile;
using Microsoft.Win32;
using System.Windows;

namespace GUI_PCRSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PCRSytemGUIController? guiController { get; private set; }
        public BaseOperation baseOperation { get; } = new();
        public PersonEdit personEdit { get; }
        public PCRTestEdit pcrTestEdit { get; }
        public MainWindow()
        {
            InitializeComponent();
            personEdit = new(this);
            pcrTestEdit = new(this);
            Closed += MW_Closed;
        }

        private void MW_Closed(object? sender, EventArgs e)
        {
            guiController?.Close();
            Application.Current.Shutdown();
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
            int personsMainBlockSize;
            int personsOverflowBlockSize;
            int pcrtTestsMainBlockSize;
            int pcrtTestsOverflowBlockSize;
            var newBlockSizeDialog = new NewFileSizesSetter();
            newBlockSizeDialog.ShowDialog();

            if (newBlockSizeDialog.DialogResult == true)
            {
                personsMainBlockSize = newBlockSizeDialog.PersonsMainBlockSize_;
                personsOverflowBlockSize = newBlockSizeDialog.PersonsOverflowBlockSize_;
                pcrtTestsMainBlockSize = newBlockSizeDialog.PCRTestsMainBlockSize_;
                pcrtTestsOverflowBlockSize = newBlockSizeDialog.PCRTestsOverflowBlockSize_;
            }
            else
            {
                MessageBox.Show("Not valid inputs.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Choose new file",
            };

            if (dialog.ShowDialog() == true)
            {
                guiController?.Close();
                string path = dialog.FileName;
                guiController = new(path, personsMainBlockSize, personsOverflowBlockSize, pcrtTestsMainBlockSize, pcrtTestsOverflowBlockSize,
                    2, 2);
                OperationsButton.IsEnabled = true;
                EnablePrintButtons();
            }
            else
            {
                MessageBox.Show("Not correctly choosed file.");
                return;
            }
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose file to open",
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;
                guiController?.Close();
                guiController = new(path);
                OperationsButton.IsEnabled = true;
                EnablePrintButtons();
            }
            else
            {
                MessageBox.Show("Not correctly choosed file.");
                return;
            }
        }

        private void SaveAndCloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            guiController?.Close();
            guiController = null;
            ChangableContent.Content = null;
            OperationsButton.IsEnabled = false;
            DisablePrintButtons();
        }

        private void GenerateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            guiController?.Generate();
        }

        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EnablePrintButtons()
        {
            PersonsMainFileButton.IsEnabled = true;
            PersonsOverflowFileButton.IsEnabled = true;
            PCRTestsMainFileButton.IsEnabled = true;
            PCRTestsMOverflowFileButton.IsEnabled = true;
        }

        private void DisablePrintButtons()
        {
            PersonsMainFileButton.IsEnabled = false;
            PersonsOverflowFileButton.IsEnabled = false;
            PCRTestsMainFileButton.IsEnabled = false;
            PCRTestsMOverflowFileButton.IsEnabled = false;
        }

        private void PrintPersonsMainFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
            {
                var printFileDialog = new FilePrint(guiController.GetPrintPersonsMainFile());
                printFileDialog.ShowDialog();
            }    
        }

        private void PrintPersonsOverflowFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
            {
                var printFileDialog = new FilePrint(guiController.GetPrintPersonsOverflowFile());
                printFileDialog.ShowDialog();
            }
        }

        private void PrintPCRTestsMainFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
            {
                var printFileDialog = new FilePrint(guiController.GetPrintPCRTestsMainFile());
                printFileDialog.ShowDialog();
            }
        }

        private void PrintPCRTestsMOverflowFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
            {
                var printFileDialog = new FilePrint(guiController.GetPrintPCRTestsOverflowFile());
                printFileDialog.ShowDialog();
            }
        }
    }
}