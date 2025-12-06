using AUS2_S2.DataClasses;
using AUS2_S2.GUIControllers;
using AUS2_S2.HeapFile;
using GUI_HeapFIle.OperationsWindows;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace GUI_HeapFIle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PersonHF_GUIController? guiController;
        public MainWindow()
        {
            InitializeComponent();
            Closed += MW_Closed;
        }

        private void MW_Closed(object? sender, EventArgs e)
        {
            guiController?.CloseFile();
            Application.Current.Shutdown();
        }

        private void PrintFileButton_Click(object sender, RoutedEventArgs e)
        {
            PrintFile();
        }

        private void PrintFile()
        {
            TextBoxHeapFile.Text = guiController?.GetAllBlocks().allBlocks;
            TextBoxPartlyClearBlocks.Text = guiController?.GetAllBlocks().partlyClearBlocks;
            TextBoxFullyClearBlocks.Text = guiController?.GetAllBlocks().fullyClearBlocks;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
                new InsertWindow(guiController).ShowDialog();
        }

        private void GetButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
                new GetWindow(guiController).ShowDialog();
        }

        private void DeleteButton_Click_2(object sender, RoutedEventArgs e)
        {
            if (guiController != null)
                new DeleteWindow(guiController).ShowDialog();
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            guiController?.CloseFile();
            Close();
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int newblockSize;
            var newBlockSizeDialog = new NewFileBlockSizeWindow();
            newBlockSizeDialog.ShowDialog();

            if (newBlockSizeDialog.DialogResult == true)
            {
                newblockSize = newBlockSizeDialog.BlockSize;
            }
            else
            {
                MessageBox.Show("Not valid block size.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Choose new file",
                //Filter = "Heap file (*.heap)|*.heap|Všetky súbory (*.*)|*.*",
                //DefaultExt = ".heap",
                //AddExtension = true
            };

            if (dialog.ShowDialog() == true)
            {
                guiController?.CloseFile();
                string path = dialog.FileName;
                guiController = new(new BHeapFile<Person>(newblockSize, path, path + ".metadata"));
            }
            else
            {
                MessageBox.Show("Not correctly choosed file.");
                return;
            }
        }

        private void GenerateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            guiController?.GenerateDataForHeapFile();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose file to open",
            };

            if (dialog.ShowDialog() == true)
            {
                var metadataFilePath = dialog.FileName + ".metadata";
                if (!File.Exists(metadataFilePath))
                {
                    MessageBox.Show("Metadata file missing.");
                    return;
                }

                guiController?.CloseFile();
                guiController = new(new BHeapFile<Person>(dialog.FileName, metadataFilePath));
            }
            else
            {
                MessageBox.Show("Not correctly choosed file.");
                return;
            }
        }
    }
}