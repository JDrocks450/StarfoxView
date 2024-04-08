using StarFox.Interop.MAP.CONTEXT;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFoxMapVisualizer.Renderers
{
    internal interface ISCRRendererBase
    {
        /// <summary>
        /// The current <see cref="MAPContextDefinition"/> this control is displaying
        /// <para/>Make sure to use the <see cref="SetContext(MAPContextDefinition?, bool, bool)"/> function
        /// </summary>
        public MAPContextDefinition? LevelContext { get; set; }
        /// <summary>
        /// References to any files on the disk are kept here for clarity to the end user
        /// <para/>FILE PATH -> FILE TYPE
        /// </summary>
        Dictionary<string, string> ReferencedFiles { get; }
        /// <summary>
        /// Updates the <see cref="LevelContext"/> of this control and rerenders the control        
        /// </summary>
        /// <param name="SelectedContext"></param>
        /// <param name="ExtractCCR"></param>
        /// <param name="ExtractPCR"></param>
        /// <returns></returns>
        Task SetContext(MAPContextDefinition? SelectedContext, bool ExtractCCR = false, bool ExtractPCR = false);        
    }
}
