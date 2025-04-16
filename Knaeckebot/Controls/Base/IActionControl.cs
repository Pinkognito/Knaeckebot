using Knaeckebot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knaeckebot.Controls.Base
{
        /// <summary>
        /// Interface for all specialized action control types
        /// </summary>
        public interface IActionControl
        {
            /// <summary>
            /// Initialize the control with an action
            /// </summary>
            void Initialize(ActionBase action);

            /// <summary>
            /// Update the control's UI elements from the action's properties
            /// </summary>
            void UpdateControlFromAction();

            /// <summary>
            /// Update the action's properties from the control's UI elements
            /// </summary>
            void UpdateActionFromControl();
       }
    
}

