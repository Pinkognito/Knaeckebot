using System;
using Knaeckebot.Services;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Possible mouse actions
    /// </summary>
    public enum MouseActionType
    {
        LeftClick,
        RightClick,
        MiddleClick,   // Click on the mouse wheel (middle mouse button)
        DoubleClick,
        MouseDown,
        MouseUp,
        MouseMove,
        MouseWheel
    }

    /// <summary>
    /// Represents a mouse action
    /// </summary>
    public class MouseAction : ActionBase
    {
        private int _x;
        private int _y;
        private int _wheelDelta;
        private MouseActionType _actionType = MouseActionType.LeftClick;

        /// <summary>
        /// X-coordinate on the screen
        /// </summary>
        public int X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    _x = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Y-coordinate on the screen
        /// </summary>
        public int Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    _y = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Mouse wheel delta (positive = up, negative = down)
        /// </summary>
        public int WheelDelta
        {
            get => _wheelDelta;
            set
            {
                if (_wheelDelta != value)
                {
                    _wheelDelta = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of mouse action
        /// </summary>
        public MouseActionType ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType != value)
                {
                    _actionType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Executes the mouse action
        /// </summary>
        public override void Execute()
        {
            try
            {
                LogManager.Log($"Executing mouse action: {this}");

                switch (ActionType)
                {
                    case MouseActionType.MouseWheel:
                        // Delegate mouse wheel action to the MouseService
                        MouseService.Instance.ScrollMouseWheel(X, Y, WheelDelta);
                        break;

                    default:
                        // Other mouse actions as before
                        MouseService.Instance.ExecuteMouseAction(this);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error in mouse action: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Creates a copy of this mouse action
        /// </summary>
        public override ActionBase Clone()
        {
            return new MouseAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                X = this.X,
                Y = this.Y,
                WheelDelta = this.WheelDelta,
                ActionType = this.ActionType
            };
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            return ActionType switch
            {
                MouseActionType.MouseWheel => $"Mouse wheel ({X}, {Y}) Delta: {WheelDelta}",
                MouseActionType.MiddleClick => $"Middle click ({X}, {Y})",
                _ => $"{ActionType} ({X}, {Y})"
            };
        }
    }
}