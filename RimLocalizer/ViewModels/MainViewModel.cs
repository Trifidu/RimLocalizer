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
namespace RimLocalizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Event for notification of changes to properties 
        public event PropertyChangedEventHandler PropertyChanged;

        private ModItem _selectedMod; // Поле для хранения выбранного мода

        public ModItem SelectedMod
        {
            get => _selectedMod;
            set
            {
                _selectedMod = value; // Устанавливаем выбранный мод
                OnPropertyChanged(nameof(SelectedMod)); // Уведомляем интерфейс об изменении
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
            public string Name { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public string PreviewPath { get; set; }
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
                    Author = null,
                    PreviewPath = null,
                };
            }
            var xml = XDocument.Load(aboutPath);

            string name = xml.Root.Element("name")?.Value ?? "Неизвестный мод";
            string description = xml.Root.Element("description")?.Value ?? "Описание отсутствует";
            string author = xml.Root.Element("author")?.Value ?? "Автор отсутствует";

            return new ModInfo
            {
                Name = name,
                Description = description,
                Author = author,
                PreviewPath = File.Exists(previewPath) ? previewPath : null
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


    }
}

namespace RimLocalizer
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        // RelayCommand constructor
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
