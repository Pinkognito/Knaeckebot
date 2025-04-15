using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Knaeckebot.Models;

namespace Knaeckebot.Services
{
    /// <summary>
    /// Service for browser interactions
    /// </summary>
    public class BrowserService
    {
        #region Singleton

        private static readonly Lazy<BrowserService> _instance = new Lazy<BrowserService>(() => new BrowserService());

        /// <summary>
        /// Singleton instance of BrowserService
        /// </summary>
        public static BrowserService Instance => _instance.Value;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        private BrowserService()
        {
        }

        /// <summary>
        /// Executes a browser action
        /// </summary>
        public void ExecuteBrowserAction(BrowserAction action)
        {
            if (action == null) return;

            try
            {
                switch (action.ActionType)
                {
                    case BrowserActionType.FindElementAndClick:
                        FindElementAndClick(action.Selector ?? string.Empty);
                        break;

                    case BrowserActionType.ExecuteJavaScript:
                        var result = ExecuteJavaScript(action.JavaScript ?? string.Empty);
                        ProcessJavaScriptResult(result, action);
                        break;

                    case BrowserActionType.GetCoordinates:
                        // If UseLastResults is true, use the saved position
                        if (action.UseLastResults)
                        {
                            ClickAtCoordinates(action.XResult, action.YResult);
                        }
                        else
                        {
                            GetElementCoordinates(action);
                        }
                        break;
                }

                LogManager.Log($"Browser action executed: {action.ActionType}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing browser action: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Finds an element in the browser by a selector and clicks on it
        /// </summary>
        private void FindElementAndClick(string selector)
        {
            // This method would normally use a browser automation framework,
            // e.g. Selenium WebDriver. For an example, we use JavaScript:
            string script = $@"
                var element = document.querySelector('{selector}');
                if (!element) {{
                    throw new Error('Element not found: {selector}');
                }}
                
                var rect = element.getBoundingClientRect();
                var x = Math.round(window.screenX + rect.left + rect.width/2);
                var y = Math.round(window.screenY + 72 + rect.top + rect.height/2);
                
                return {{x: x, y: y}};";

            var result = ExecuteJavaScript(script);
            if (result != null && result.Contains("x") && result.Contains("y"))
            {
                try
                {
                    var coordinates = JsonSerializer.Deserialize<CoordinatesResult>(result);
                    if (coordinates != null)
                    {
                        ClickAtCoordinates(coordinates.X, coordinates.Y);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"Error parsing coordinates: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Executes JavaScript in the browser
        /// </summary>
        /// <param name="script">The JavaScript code to execute</param>
        /// <returns>The JSON representation of the result</returns>
        private string? ExecuteJavaScript(string script)
        {
            // This method would normally use a browser automation framework,
            // For this demo we use a simple approach that creates a temporary HTML file

            try
            {
                // Temp folder for the HTML file
                string tempPath = Path.Combine(Path.GetTempPath(), "Knaeckebot_script.html");

                // Create HTML file that executes the JavaScript
                string html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Knaeckebot Script</title>
                        <script>
                            function runScript() {{
                                try {{
                                    var result = (function() {{
                                        {script}
                                    }})();
                                    
                                    document.getElementById('result').value = JSON.stringify(result);
                                }} catch (error) {{
                                    document.getElementById('result').value = JSON.stringify({{ error: error.message }});
                                }}
                            }}
                        </script>
                    </head>
                    <body onload='runScript()'>
                        <textarea id='result' style='width:100%; height:300px;'></textarea>
                    </body>
                    </html>";

                File.WriteAllText(tempPath, html);

                // Open the file in the default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                // This implementation is for demonstration purposes only
                // In a real application, you would control the browser via WebDriver or other APIs
                LogManager.Log("JavaScript executed in browser");

                // NOTE: Since we don't have real browser control, we return null here
                // In a real implementation, you would return the actual result
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error executing JavaScript: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determines the coordinates of an element in the browser
        /// </summary>
        private void GetElementCoordinates(BrowserAction action)
        {
            // Standard script for copy button if no custom JavaScript is defined
            string script = !string.IsNullOrEmpty(action.JavaScript)
                ? action.JavaScript
                : BrowserAction.DefaultCopyButtonScript;

            var result = ExecuteJavaScript(script);
            ProcessJavaScriptResult(result, action);
        }

        /// <summary>
        /// Processes the result of a JavaScript call
        /// </summary>
        private void ProcessJavaScriptResult(string? result, BrowserAction action)
        {
            if (result != null && result.Contains("x") && result.Contains("y"))
            {
                try
                {
                    var coordinates = JsonSerializer.Deserialize<CoordinatesResult>(result);
                    if (coordinates != null)
                    {
                        action.XResult = coordinates.X;
                        action.YResult = coordinates.Y;
                        LogManager.Log($"Coordinates determined: X={coordinates.X}, Y={coordinates.Y}");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"Error parsing coordinates: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clicks on screen coordinates
        /// </summary>
        private void ClickAtCoordinates(int x, int y)
        {
            MouseService.Instance.ClickOnPosition(x, y);
        }

        /// <summary>
        /// Class for parsing coordinates from the JavaScript result
        /// </summary>
        private class CoordinatesResult
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}