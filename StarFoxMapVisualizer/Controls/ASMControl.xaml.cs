using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MISC;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for ASMControl.xaml
    /// </summary>
    public partial class ASMControl : UserControl
    {
        private const double BASE_TEXT_SIZE = 12;

        private ASM_FINST? current;        
        private ASMCodeEditor EditorScreen => current?.EditorScreen;        
        private Dictionary<string, ASM_FINST> fileInstanceMap = new();
        /// <summary>
        /// The queue of <see cref="OpenFileContents(FileInfo, ASMFile?)"/> calls made while paused
        /// </summary>
        private Queue<FOPENCALL> chewQueue = new();

        /// <summary>
        /// Gets whether this control is paused. See: <see cref="Pause"/>
        /// </summary>
        public bool Paused { get; private set; }

        public ASMControl()
        {
            InitializeComponent();            
            FileBrowserTabView.Items.Clear();
        }                

        /// <summary>
        /// Any calls made to <see cref="OpenFileContents(FileInfo, ASMFile?)"/> are queued until this control is <see cref="Unpause"/>'d 
        /// </summary>
        public void Pause()
        {
            Paused = true;
            IsEnabled = false;
        }
        /// <summary>
        /// Unpauses the control and runs all calls to <see cref="OpenFileContents(FileInfo, ASMFile?)"/> asyncronously
        /// </summary>
        /// <returns></returns>
        public async Task Unpause()
        {
            Paused = false;
            IsEnabled = true;
            while (chewQueue.TryDequeue(out var call))
                await doFileOpenTaskAsync(call);            
        }
        private class FOPENCALL
        {
            public FileInfo FileSelected;
            public ASMFile? FileData = default;
            /// <summary>
            /// The symbol to jump to after opening, if applicable
            /// </summary>
            public ASMChunk chunk;
        }
        private async Task doFileOpenTaskAsync(FOPENCALL Call)
        {
            void TabShown()
            {
                current.EditorScreen.Focus();
                if (Call.chunk != default)
                    current.EditorScreen.JumpToSymbol(Call.chunk);
                _ = Dispatcher.InvokeAsync(current.EditorScreen.Focus, DispatcherPriority.ApplicationIdle);
            }
            void OpenTab(ASM_FINST inst)
            {
                FileBrowserTabView.SelectedItem = inst.Tab; // select the tab
                FilePathBlock.Text = Call.FileSelected.Name;
                current = inst;
                TabShown();
            }
            if (fileInstanceMap.TryGetValue(Call.FileSelected.FullName, out var finst))
            {
                OpenTab(finst);// select the tab
                return;
            }
            foreach (var fileInstance in fileInstanceMap.Values)
            {
                if (Call.FileSelected.FullName == fileInstance.OpenFile.FullName) // FILE Opened?
                {
                    OpenTab(fileInstance);// select the tab
                    return;
                }
            }
            TabItem tab = new()
            {
                Header = Call.FileSelected.Name,
            };
            ASM_FINST instance = current = new ASM_FINST()
            {
                OpenFile = Call.FileSelected,
                symbolMap = new(),
                Tab = tab,
                FileImportData = Call.FileData
            };
            tab.Tag = instance;
            var newEditZone = new ASMCodeEditor(this, instance)
            {
                FontSize = BASE_TEXT_SIZE
            };
            instance.StateObject = newEditZone;
            tab.Content = newEditZone;

            fileInstanceMap.Add(Call.FileSelected.FullName, instance);
            FileBrowserTabView.Items.Add(tab);
            FileBrowserTabView.SelectedItem = tab;
            FilePathBlock.Text = Call.FileSelected.Name;
            await ParseAsync(Call.FileSelected);
            TabShown();
        }

        public async Task OpenFileContents(FileInfo FileSelected, ASMFile? FileData = default, ASMChunk? Symbol = default)
        {
            var call = new FOPENCALL()
            {
                FileSelected = FileSelected,
                FileData = FileData,
                chunk = Symbol,
            };
            if (Paused)
            {
                chewQueue.Enqueue(call);
                return;
            }
            await doFileOpenTaskAsync(call);
        }
        public Task OpenSymbol(ASMChunk chunk) => OpenFileContents(new FileInfo(chunk.OriginalFileName), null, chunk);

        private DispatcherOperation ParseAsync(FileInfo File)
        {            
            return Dispatcher.InvokeAsync(async delegate
            {
                await EditorScreen.InvalidateFileContents();
            });
        }

        private void ButtonZoomRestore_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize = BASE_TEXT_SIZE;
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize--;
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize+=1;
        }
    }
}
