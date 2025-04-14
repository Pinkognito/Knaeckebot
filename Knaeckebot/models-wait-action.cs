using System;
using System.Threading;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Represents a wait operation
    /// </summary>
    public class WaitAction : ActionBase
    {
        private int _waitTime = 1000;

        /// <summary>
        /// Wait time in milliseconds
        /// </summary>
        public int WaitTime
        {
            get => _waitTime;
            set
            {
                if (_waitTime != value)
                {
                    _waitTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Constructor with default values
        /// </summary>
        public WaitAction()
        {
            Name = "Wait";
            Description = "Waits for a specific time";
        }

        /// <summary>
        /// Executes the wait operation
        /// </summary>
        public override void Execute()
        {
            Console.WriteLine($"Waiting for {WaitTime} ms");
            Thread.Sleep(WaitTime);
        }

        /// <summary>
        /// Creates a copy of this wait operation
        /// </summary>
        public override ActionBase Clone()
        {
            return new WaitAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                WaitTime = this.WaitTime
            };
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            return $"Wait: {WaitTime} ms";
        }
    }
}