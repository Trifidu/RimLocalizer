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

namespace RimLocalizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Event for notification of changes to properties 
        public event PropertyChangedEventHandler PropertyChanged;

        // Mods Collection
        private ObservableCollection<string> _mods;
        public ObservableCollection<string> Mods
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
            Mods = new ObservableCollection<string>();
            LoadModsCommand = new RelayCommand(LoadMods);
            GamePath = Properties.Settings.Default.GamePath;
            ModsPath = Properties.Settings.Default.ModsPath;
        }

        // Method for finding mods
        public void LoadMods()
        {
            Mods.Clear();

            // Search for local mods (game folder)
            if (!string.IsNullOrEmpty(GamePath))
            {
                string localModsPath = Path.Combine(GamePath, "Mods");
                if (Directory.Exists(localModsPath))
                {
                    foreach (var dir in Directory.GetDirectories(localModsPath))
                    {
                        string modName = Path.GetFileName(dir);
                        Mods.Add($"{modName} [L]");
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
                        string modName = Path.GetFileName(dir);
                        Mods.Add($"{modName} [S]");
                    }
                }
            }
        }

        // Method to notify the interface of a property change
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
