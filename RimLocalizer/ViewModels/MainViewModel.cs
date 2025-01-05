using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using RimLocalizer;
using System.Xml.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows;

namespace RimLocalizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Event for notification of changes to properties 
        public event PropertyChangedEventHandler? PropertyChanged;

        // Field for storing the selected mod
        private ModItem _selectedMod;

        public ModItem SelectedMod
        {
            get => _selectedMod;
            set
            {
                // Install the selected mod
                _selectedMod = value;
                // Notify the interface of the change
                OnPropertyChanged(nameof(SelectedMod));

                // Updating the file list
                UpdateTranslationFiles();
            }
        }

        // Mods Collection
        private ObservableCollection<ModItem> _mods;
        public ObservableCollection<ModItem> Mods
        {
            get { return _mods; }
            set
            {
                _mods = value;
                OnPropertyChanged(nameof(Mods));
            }
        }

        // Paths to find mods
        private string _gamePath;
        public string GamePath
        {
            get => _gamePath;
            set
            {
                _gamePath = value;
                OnPropertyChanged(nameof(GamePath));
            }
        }

        private string _modsPath;
        public string ModsPath
        {
            get => _modsPath;
            set
            {
                _modsPath = value;
                OnPropertyChanged(nameof(ModsPath));
            }
        }
        public ICommand LoadModsCommand { get; set; }

        // ViewModel
        public MainViewModel()
        {
            // Initializing fields
            _selectedMod = new ModItem();
            _mods = new ObservableCollection<ModItem>();
            _gamePath = string.Empty;
            _modsPath = string.Empty;
            _searchQuery = string.Empty;
            _filteredMods = new ObservableCollection<ModItem>();
            _originalFileContent = string.Empty;
            _translatedFileContent = string.Empty;
            _selectedTranslationFile = string.Empty;
            _originalFilePath = string.Empty;

            // Initialising the mod collection
            GamePath = Properties.Settings.Default.GamePath;
            ModsPath = Properties.Settings.Default.ModsPath;
            LoadModsCommand = new RelayCommand(LoadMods);
            Mods = new ObservableCollection<ModItem>();
            FilteredMods = new ObservableCollection<ModItem>();

            // Checking saved paths and autoloading mods
            if (!string.IsNullOrEmpty(GamePath) || !string.IsNullOrEmpty(ModsPath))
            {
                LoadMods();
            }

            // Initializing clear command
            ClearSearchCommand = new RelayCommand(() =>
            {
                SearchQuery = string.Empty;
            });
        }

        // Method for finding mods
        public void LoadMods()
        {
            Mods.Clear();

            var loadedMods = new List<ModItem>();


            // Search for local mods
            if (!string.IsNullOrEmpty(GamePath))
            {
                string localModsPath = Path.Combine(GamePath, "Mods");
                if (Directory.Exists(localModsPath))
                {
                    foreach (var dir in Directory.GetDirectories(localModsPath))
                    {
                        var modInfo = GetModInfo(dir); // Extracting information
                        loadedMods.Add(new ModItem
                        {
                            Name = modInfo.Name,
                            Description = modInfo.Description,
                            Author = modInfo.Author,
                            Path = dir,
                            PreviewPath = modInfo.PreviewPath,
                            Source = "[L]"
                        });
                    }
                }
            }

            // Search for mods from Steam Workshop
            if (!string.IsNullOrEmpty(ModsPath))
            {
                if (Directory.Exists(ModsPath))
                {
                    foreach (var dir in Directory.GetDirectories(ModsPath))
                    {
                        var modInfo = GetModInfo(dir); // Extracting information
                        loadedMods.Add(new ModItem
                        {
                            Name = modInfo.Name,
                            Description = modInfo.Description,
                            Author = modInfo.Author,
                            Path = dir,
                            PreviewPath = modInfo.PreviewPath,
                            Source = "[S]"
                        });
                    }
                }
            }

            // Sort by mods name
            foreach (var mod in loadedMods.OrderBy(m => m.Name))
            {
                Mods.Add(mod);
            }

            ApplySearchFilter();
        }

        // Method to notify the interface of a property change
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class ModInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string PreviewPath { get; set; } = string.Empty;
        }

        //Method to add desc from mods
        private ModInfo GetModInfo(string modPath)
        {
            string aboutPath = Path.Combine(modPath, "About", "About.xml");
            string previewPath = Path.Combine(modPath, "About", "Preview.png");

            if (!File.Exists(aboutPath))
            {
                return new ModInfo
                {
                    Name = "Неизвестный мод, проблема чтения About.xml",
                    Description = "Описание отсутствует",
                    Author = string.Empty,
                    PreviewPath = string.Empty,
                };
            }

            var xml = XDocument.Load(aboutPath);

            string name = xml.Root?.Element("name")?.Value ?? "Неизвестный мод";
            string description = xml.Root?.Element("description")?.Value ?? "Описание отсутствует";
            string author = xml.Root?.Element("author")?.Value ?? "Автор отсутствует";

            return new ModInfo
            {
                Name = name,
                Description = description,
                Author = author,
                PreviewPath = File.Exists(previewPath) ? previewPath : string.Empty
            };
        }

        // Adding a property for a search
        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged(nameof(SearchQuery));
                    ApplySearchFilter();
                }
            }
        }

        // Collection for storing filtered mods
        private ObservableCollection<ModItem> _filteredMods;
        public ObservableCollection<ModItem> FilteredMods
        {
            get => _filteredMods;
            set
            {
                _filteredMods = value;
                OnPropertyChanged(nameof(FilteredMods));
            }
        }

        // Method for applying the search filter
        private void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredMods = new ObservableCollection<ModItem>(Mods);
            }
            else
            {
                FilteredMods = new ObservableCollection<ModItem>(
                    Mods.Where(mod => mod.Name.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0));
            }
        }

        // Command to clear the search field
        public ICommand ClearSearchCommand { get; }

        // Search file to translate
        private List<string> GetTranslationFiles(string modPath)
        {
            List<string> translationFiles = new List<string>();

            // Find the latest version
            // Only folders with numbers and Sort by descending order
            var versionFolders = Directory.GetDirectories(modPath)
                .Where(dir => Regex.IsMatch(Path.GetFileName(dir), @"^\d+(\.\d+)*$"))
                .OrderByDescending(dir => dir, StringComparer.Ordinal);

            string? latestVersionPath = versionFolders.FirstOrDefault();
            if (!string.IsNullOrEmpty(latestVersionPath))
            {
                // Check Languages/English and Defs folders inside the latest version
                translationFiles.AddRange(FindTranslationFilesInPaths(latestVersionPath));
            }

            // If there is nothing inside the latest version, check the root folder
            if (translationFiles.Count == 0)
            {
                translationFiles.AddRange(FindTranslationFilesInPaths(modPath));
            }

            // If nothing is found
            if (translationFiles.Count == 0)
            {
                translationFiles.Add("Нет файлов для перевода.");
            }

            return translationFiles;
        }

        private string _translatedFileContent;
        public string TranslatedFileContent
        {
            get => _translatedFileContent;
            set
            {
                _translatedFileContent = value;
                OnPropertyChanged(nameof(TranslatedFileContent));
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith("\\") ? basePath : basePath + "\\");
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private IEnumerable<string> FindTranslationFilesInPaths(string basePath)
        {
            List<string> files = new List<string>();

            // Checking the defs folder
            string defsPath = Path.Combine(basePath, "Defs");
            if (Directory.Exists(defsPath))
            {
                files.AddRange(Directory.GetFiles(defsPath, "*.xml", SearchOption.AllDirectories)
                    .Select(file => Path.GetRelativePath(basePath, file)));
            }

            // Check Languages/English folder
            string languagesPath = Path.Combine(basePath, "Languages", "English");
            if (Directory.Exists(languagesPath))
            {
                files.AddRange(Directory.GetFiles(languagesPath, "*.xml", SearchOption.AllDirectories)
                    .Select(file => Path.GetRelativePath(basePath, file)));
            }

            return files;
        }

        // Collection of files for translation
        private ObservableCollection<string> _translationFiles = new ObservableCollection<string>();
        public ObservableCollection<string> TranslationFiles
        {
            get => _translationFiles;
            set
            {
                _translationFiles = value;
                OnPropertyChanged(nameof(TranslationFiles));
            }
        }

        // Open file to translate from filelist
        // property for storing file contents
        private string _originalFileContent;
        public string OriginalFileContent
        {
            get => _originalFileContent;
            set
            {
                _originalFileContent = value;
                OnPropertyChanged(nameof(OriginalFileContent));
            }
        }

        // select a file, read its contents and write it
        private string _selectedTranslationFile;
        public string SelectedTranslationFile
        {
            get => _selectedTranslationFile;
            set
            {
                if (_selectedTranslationFile != value)
                {
                    _selectedTranslationFile = value;
                    OnPropertyChanged(nameof(SelectedTranslationFile));

                    // Read the contents of the original file
                    if (!string.IsNullOrEmpty(_selectedTranslationFile))
                    {
                        string fullPath = Path.Combine(ModsPath, SelectedMod.Path, _selectedTranslationFile);
                        if (File.Exists(fullPath))
                        {
                            OriginalFileContent = File.ReadAllText(fullPath);
                        }
                        else
                        {
                            OriginalFileContent = "Файл не найден.";
                        }
                    }
                    else
                    {
                        OriginalFileContent = string.Empty;
                    }

                    // Read the translation of the file
                    LoadTranslationFile(_selectedTranslationFile);
                }
            }
        }

        // method to update file list for translate
        private void UpdateTranslationFiles()
        {
            if (SelectedMod != null)
            {
                string basePath = Path.Combine(ModsPath, SelectedMod.Path);

                // Looking for translation files
                var files = FindTranslationFilesInPaths(basePath).ToList();

                // If there are no files, add a message
                if (files.Count == 0)
                {
                    files.Add("Нет файлов для перевода.");
                }

                TranslationFiles = new ObservableCollection<string>(files);
            }
            else
            {
                // Clearing the collection if no mod is selected
                TranslationFiles = new ObservableCollection<string>();
            }

            OnPropertyChanged(nameof(TranslationFiles));
        }

        private string _originalFilePath;
        public string OriginalFilePath
        {
            get => _originalFilePath;
            set
            {
                _originalFilePath = value;
                OnPropertyChanged(nameof(OriginalFilePath));
            }
        }

        private void LoadTranslationFile(string selectedFile)
        {
            if (string.IsNullOrEmpty(selectedFile)) return;

            // Path to Languages/Russian folder
            string? directoryName = Path.GetDirectoryName(selectedFile);
            if (directoryName == null)
            {
                // If selectedFile does not contain a path, copy the original
                TranslatedFileContent = OriginalFileContent;
                return;
            }

            string russianPath = Path.Combine(directoryName, "Languages", "Russian");
            if (!Directory.Exists(russianPath))
            {
                // If there is no Russian folder, copy the original folder
                TranslatedFileContent = OriginalFileContent;
                return;
            }

            // Determine the relative path
            string relativePath;
            if (selectedFile.StartsWith(ModsPath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = Path.GetRelativePath(ModsPath, selectedFile);
            }
            else if (selectedFile.StartsWith(GamePath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = Path.GetRelativePath(Path.Combine(GamePath, "Mods"), selectedFile);
            }
            else
            {
                // If the path does not match either ModsPath or GamePath
                TranslatedFileContent = OriginalFileContent;
                return;
            }

            // Replace the Defs folder with DefInjected to match the structure
            relativePath = relativePath.Replace("Defs", "DefInjected");

            string translationFilePath = Path.Combine(russianPath, relativePath);

            // If the translation file exists, load its contents
            if (File.Exists(translationFilePath))
            {
                TranslatedFileContent = File.ReadAllText(translationFilePath);
            }
            else
            {
                // If there is no translation, copy the original
                TranslatedFileContent = OriginalFileContent;
            }
        }
    }
}

namespace RimLocalizer
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler? CanExecuteChanged;

        // RelayCommand constructor
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
