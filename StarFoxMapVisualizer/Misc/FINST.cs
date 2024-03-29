using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Controls.Subcontrols;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Represents an instance of a file opened in an editor
    /// </summary>
    /// <typeparam name="File">The type of file this is interpreted as</typeparam>
    /// <typeparam name="State">The type of editor it is used in</typeparam>
    /// <typeparam name="Tag">The object used to find this instance, like a tab at the top of the screen</typeparam>
    public class FINST<File, State, Tag> where File : IImporterObject
    {
        internal FileInfo OpenFile;
        internal File? FileImportData;

        internal Tag Tab;
        internal State StateObject;
    }
    /// <summary>
    /// A class that represents an instance of a file opened in the ASMControl
    /// <para>One per tab at the top of the editor</para>
    /// </summary>
    public class ASM_FINST : FINST<ASMFile, ASMCodeEditor, TabItem>
    {
        internal Dictionary<ASMChunk, Run>? symbolMap;
        internal Dictionary<long, Inline> NewLineMap { get; } = new();
        internal ASMCodeEditor EditorScreen => StateObject;
    }
    public class MAP_FINST : FINST<MAPFile, MAP_FINST.MAPEditorState, TabItem>
    {
        public class MAPEditorState
        {
            public bool Loaded => ContentControl != default;
            public Panel? ContentControl { get; set; } = default;
            
            public double LevelWidth = 0;

            public List<MAPScript> Subsections { get; } = new();
        }

        public MAP_FINST()
        {
            StateObject = new MAPEditorState();
        }
    }
}
