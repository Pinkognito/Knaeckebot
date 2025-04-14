using Knaeckebot.Services;
using System;
using System.Diagnostics;

namespace Knaeckebot.Models
{
    /// <summary>
    /// Possible browser action types
    /// </summary>
    public enum BrowserActionType
    {
        FindElementAndClick,
        ExecuteJavaScript,
        GetCoordinates
    }

    /// <summary>
    /// Represents a browser interaction action
    /// </summary>
    public class BrowserAction : ActionBase
    {
        private string? _selector = string.Empty;
        private string? _javaScript = string.Empty;
        private BrowserActionType _actionType = BrowserActionType.FindElementAndClick;
        private int _xResult;
        private int _yResult;
        private bool _useLastResults = false;

        /// <summary>
        /// CSS or XPath selector for the element
        /// </summary>
        public string? Selector
        {
            get => _selector;
            set
            {
                if (_selector != value)
                {
                    _selector = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// JavaScript code to be executed
        /// </summary>
        public string? JavaScript
        {
            get => _javaScript;
            set
            {
                if (_javaScript != value)
                {
                    _javaScript = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of browser action
        /// </summary>
        public BrowserActionType ActionType
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
        /// X-coordinate of the found element (result)
        /// </summary>
        public int XResult
        {
            get => _xResult;
            set
            {
                if (_xResult != value)
                {
                    _xResult = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Y-coordinate of the found element (result)
        /// </summary>
        public int YResult
        {
            get => _yResult;
            set
            {
                if (_yResult != value)
                {
                    _yResult = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether to use the last determined coordinates
        /// </summary>
        public bool UseLastResults
        {
            get => _useLastResults;
            set
            {
                if (_useLastResults != value)
                {
                    _useLastResults = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Default JavaScript for finding the last copy button
        /// </summary>
        public static string DefaultCopyButtonScript = @"
// Find all SVG elements with data-testid=""action-bar-copy""
var copyIcons = document.querySelectorAll('svg[data-testid=""action-bar-copy""]');
var lastSvgElement = copyIcons[copyIcons.length - 1];
var buttonElement = lastSvgElement.closest('button');

// Determine position of the element
var rect = buttonElement.getBoundingClientRect();
var relX = Math.round(rect.left + rect.width/2);
var relY = Math.round(rect.top + rect.height/2);

// Determine browser position
var browserX = window.screenX || window.screenLeft;
var browserY = window.screenY || window.screenTop;
var browserHeaderHeight = 72; // Adjust according to browser

// Calculate absolute coordinates
var screenX = Math.round(browserX + relX);
var screenY = Math.round(browserY + browserHeaderHeight + relY);

// Return result
return {x: screenX, y: screenY};";

        /// <summary>
        /// Executes the browser action
        /// </summary>
        public override void Execute()
        {
            // This method will be implemented later and uses the BrowserService
            // For example:
            // BrowserService.Instance.ExecuteBrowserAction(this);
            LogManager.Log($"Execute browser action: {ActionType}");

            if (ActionType == BrowserActionType.GetCoordinates && UseLastResults)
            {
                // Create and execute a mouse action with the saved coordinates
                var mouseAction = new MouseAction
                {
                    X = XResult,
                    Y = YResult,
                    ActionType = MouseActionType.LeftClick
                };
                mouseAction.Execute();
            }
        }

        /// <summary>
        /// Creates a copy of this browser action
        /// </summary>
        public override ActionBase Clone()
        {
            return new BrowserAction
            {
                Name = this.Name,
                Description = this.Description,
                DelayBefore = this.DelayBefore,
                IsEnabled = this.IsEnabled,
                Selector = this.Selector,
                JavaScript = this.JavaScript,
                ActionType = this.ActionType,
                XResult = this.XResult,
                YResult = this.YResult,
                UseLastResults = this.UseLastResults
            };
        }

        /// <summary>
        /// String representation of the action
        /// </summary>
        public override string ToString()
        {
            return ActionType switch
            {
                BrowserActionType.FindElementAndClick => $"Find & Click: {Selector}",
                BrowserActionType.ExecuteJavaScript => "Execute JS",
                BrowserActionType.GetCoordinates => $"Coordinates: ({XResult}, {YResult})",
                _ => "Browser Action"
            };
        }
    }
}