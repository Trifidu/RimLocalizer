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
using System.Windows.Navigation;
using System.IO;
using Microsoft.Win32;
using RimLocalizer.ViewModels;
using RimLocalizer.Properties;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace RimLocalizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); // Connecting ViewModel
            GamePathTextBox.Text = Properties.Settings.Default.GamePath;
            ModsPathTextBox.Text = Properties.Settings.Default.ModsPath;
        }

        // Open file XML and copy to original Dialog Window
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialogForLoad = new OpenFileDialog();
            openFileDialogForLoad.Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*";
            if (openFileDialogForLoad.ShowDialog() == true)
            {
                string filePath = openFileDialogForLoad.FileName;
                if (filePath != null)
                {
                    OriginalTextBox.Text = File.ReadAllText(filePath);
                }
            }
        }

        // Save new file XML to specific path
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

            // Check that TextBox is not empty
            if (string.IsNullOrEmpty(TranslatedTextBox.Text))
            {
                MessageBox.Show("Переведенный текст пустой. Нечего сохранять!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Chosing file path
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*";
            saveFileDialog.Title = "Сохранить переведенный файл";

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // Saving text from TranslatedTextBox in file
                File.WriteAllText(filePath, TranslatedTextBox.Text);

                // Show success msg
                MessageBox.Show("Файл успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        // Chosing game path
        private void SelectGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Выберите папку с игрой RimWorld",
                InitialDirectory = Properties.Settings.Default.GamePath ?? "C:\\"
            })
            {
                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var selectedPath = folderDialog.FileName;

                    if (DataContext is MainViewModel viewModel)
                    {
                        viewModel.GamePath = selectedPath;
                        Properties.Settings.Default.GamePath = selectedPath;
                        Properties.Settings.Default.Save();
                    }
                }
            }
        }

        // Chosing mods path
        private void SelectModsPathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Выберите папку с модами",
                InitialDirectory = Properties.Settings.Default.ModsPath ?? "C:\\"
            })
            {
                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var selectedPath = folderDialog.FileName;

                    if (DataContext is MainViewModel viewModel)
                    {
                        viewModel.ModsPath = selectedPath;
                        Properties.Settings.Default.ModsPath = selectedPath;
                        Properties.Settings.Default.Save();
                    }
                }
            }
        }
    }
}
