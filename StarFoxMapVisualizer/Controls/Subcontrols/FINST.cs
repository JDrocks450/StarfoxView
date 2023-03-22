using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls.Subcontrols;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StarFoxMapVisualizer.Controls
{
    public partial class ASMControl
    {
        /// <summary>
        /// A class that represents an instance of a file opened in the ASMControl
        /// <para>One per tab at the top of the editor</para>
        /// </summary>
        public class FINST
        {
            internal FileInfo OpenFile;
            internal ASMFile? FileImportData;
            internal Dictionary<ASMChunk, Run>? symbolMap;
            internal TabItem Tab;
            internal ASMCodeEditor EditorScreen;
        }
    }
}
