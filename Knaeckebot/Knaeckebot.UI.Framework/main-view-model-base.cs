using Knaeckebot.Models;
using Knaeckebot.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace Knaeckebot.ViewModels
{
    /// <summary>
    /// ViewModel for the main window
    /// </summary>
    public partial class MainViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private ObservableCollection<Sequence> _sequences = new ObservableCollection<Sequence>();
        private ObservableCollection<Sequence> _selectedSequences = new ObservableCollection<Sequence>();
        private Sequence? _selectedSequence;
        private ActionBase? _selectedAction;
        private ObservableCollection<ActionBase> _selectedActions = new ObservableCollection<ActionBase>();
        private SequenceVariable? _selectedVariable;
        private bool _isRecording = false;
        private string _statusMessage = "Ready";
        private bool _isPlaying = false;
        private bool _recordMouse = true;
        private bool _recordKeyboard = true;
        private KeyboardActionViewModel _keyboardActionViewModel = new KeyboardActionViewModel();
        private KeyCombinationViewModel _keyCombinationViewModel = new KeyCombinationViewModel();

        private Dictionary<Guid, object> _actionStateCache = new Dictionary<Guid, object>(); // Cache for action states


        // Clipboard for copied actions
        private List<ActionBase> _copiedActions = new List<ActionBase>();
        public List<ActionBase> CopiedActions => _copiedActions;


        // Dictionary to track Key values for each action by its ID
        private Dictionary<Guid, Key[]?> _actionKeysTracker = new Dictionary<Guid, Key[]?>();
        private bool _isChangingAction = false; // Flag to prevent recursive calls

        // Thread for playback
        private Thread? _playbackThread;
        private CancellationTokenSource? _cancellationTokenSource;

        // New command for copy or duplicate with Ctrl+C
        private ICommand? _copyOrDuplicateCommand;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel()
        {
            LogManager.Log("MainViewModel is being initialized", LogLevel.Debug);

            // Initialize commands
            InitializeCommands();

            // Initialize SequenceManager with the sequence list
            SequenceManager.Initialize(Sequences);

            // Event handlers for recorded mouse actions
            MouseService.Instance.MouseActionRecorded += OnMouseActionRecorded;
            MouseService.Instance.MouseWheelRecorded += OnMouseWheelRecorded;

            // Event handlers for recorded keyboard inputs
            KeyboardService.Instance.KeyActionRecorded += OnKeyActionRecorded;
            KeyboardService.Instance.KeyCombinationRecorded += OnKeyCombinationRecorded;

            // Start with an empty sequence
            NewSequence();

            LogManager.Log("Knaeckebot Pro started", LogLevel.Info);
            LogManager.CleanupOldLogs();
        }

        /// <summary>
        /// Command for copying actions or duplicating sequences (Ctrl+C)
        /// </summary>
        public ICommand CopyOrDuplicateCommand => _copyOrDuplicateCommand ??= new RelayCommand(CopyOrDuplicate);

        /// <summary>
        /// Executes the appropriate action based on context (selected actions or sequences)
        /// </summary>
        private void CopyOrDuplicate()
        {
            // Debug output for diagnosis
            LogManager.Log("CopyOrDuplicate Command executed", LogLevel.Debug);
            LogManager.Log($"AreActionsSelected: {AreActionsSelected}, AreSequencesSelected: {AreSequencesSelected}", LogLevel.Debug);

            // If actions are selected, copy them
            if (AreActionsSelected)
            {
                LogManager.Log("Copying selected actions (Ctrl+C)", LogLevel.Info);
                CopyActions();
            }
            // If sequences are selected, duplicate them
            else if (AreSequencesSelected || IsSequenceSelected)
            {
                LogManager.Log("Duplicating selected sequences (Ctrl+C)", LogLevel.Info);
                DuplicateSequence();
            }
            else
            {
                LogManager.Log("Neither actions nor sequences selected for Ctrl+C", LogLevel.Warning);
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Debug logging for specific properties
            if (propertyName == nameof(SelectedAction) ||
                propertyName == nameof(SelectedActions) ||
                propertyName == nameof(MainKey) ||
                propertyName == nameof(MainKeyItem) ||
                propertyName == nameof(IsCtrlModifierPressed) ||
                propertyName == nameof(IsAltModifierPressed) ||
                propertyName == nameof(IsShiftModifierPressed) ||
                propertyName == nameof(SavedKeys))
            {
                LogManager.Log($"PropertyChanged: {propertyName}", LogLevel.KeyDebug);
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple implementation of the ICommand interface
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}