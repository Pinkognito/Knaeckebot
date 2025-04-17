using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Base class for all automation actions
    /// </summary>
    public abstract class ActionBase : INotifyPropertyChanged
    {
        private string? _name = string.Empty;
        private string? _description = string.Empty;
        private int _delayBefore;
        private bool _isEnabled = true;

        /// <summary>
        /// Unique identifier for the action
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Name of the action
        /// </summary>
        public string Name
        {
            get => _name ?? string.Empty;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Description of the action
        /// </summary>
        public string Description
        {
            get => _description ?? string.Empty;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Delay before executing this action (in milliseconds)
        /// </summary>
        public int DelayBefore
        {
            get => _delayBefore;
            set
            {
                if (_delayBefore != value)
                {
                    _delayBefore = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether the action is enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Executes the action
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Executes the action with cancellation support
        /// </summary>
        public virtual void Execute(CancellationToken cancellationToken)
        {
            // Check cancellation before executing
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            Execute();
        }

        /// <summary>
        /// Creates a copy of this action
        /// </summary>
        public abstract ActionBase Clone();

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}