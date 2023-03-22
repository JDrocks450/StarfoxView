using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for EditScreen.xaml
    /// </summary>
    public partial class EditScreen : Page
    {
        public enum ViewMode
        {
            NONE,ASM,MAP
        }

        private ASMImporter ASMImport = new();
        private MAPImporter MAPImport = new();
        internal static EditScreen Current { get; private set; }
        public ViewMode CurrentMode { get; set; }
        public EditScreen()
        {
            InitializeComponent();

            SolutionExplorerView.Items.Clear();
            MacroExplorerView.Items.Clear();

            Current = this;

            Loaded += OnLoad;
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.NONE;
            await HandleViewModes();
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
        }

        private void ContextEnableDisable()
        {

        }

        private void Import()
        {
            var currentProject = AppResources.ImportedProject;
            var expandedHeaders = new List<string>();
            void CheckNode(in TreeViewItem Node)
            {
                if (Node.IsExpanded && Node.Tag != default && Node.Tag is SFCodeProjectNode sfNode)
                    expandedHeaders.Add(sfNode.FilePath);
                foreach (var treeNode in Node.Items.OfType<TreeViewItem>())
                    CheckNode(in treeNode);
            }
            foreach(var treeNode in SolutionExplorerView.Items.OfType<TreeViewItem>())
                CheckNode(in treeNode);
            var selectedItemHeader = (SolutionExplorerView.SelectedItem as TreeViewItem)?.Tag as SFCodeProjectNode;
            SolutionExplorerView.Items.Clear();
            if (currentProject == null) return;

            TreeViewItem AddProjectNode(SFCodeProjectNode Node)
            {
                TreeViewItem node = new()
                {
                    IsExpanded = true,
                    Tag = Node
                };
                //node.SetResourceReference(TreeViewItem.StyleProperty, "ProjectTreeStyle");
                foreach (var child in Node.ChildNodes)
                {
                    switch (child.Type)
                    {
                        case SFCodeProjectNodeTypes.Directory:
                            AddDirectory(in node, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            AddFile(in node, child);
                            break;
                    }
                }
                return node;
            }
            void AddDirectory(in TreeViewItem Parent, SFCodeProjectNode DirNode)
            {
                TreeViewItem thisTreeNode = new()
                {
                    Header = System.IO.Path.GetFileName(DirNode.FilePath),
                    Tag = DirNode
                };
                thisTreeNode.SetResourceReference(TreeViewItem.StyleProperty, "FolderTreeStyle");
                foreach (var child in DirNode.ChildNodes)
                {
                    switch (child.Type)
                    {
                        case SFCodeProjectNodeTypes.Directory:
                            AddDirectory(in thisTreeNode, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            AddFile(in thisTreeNode, child);
                            break;
                    }
                }
                if (expandedHeaders.Contains(DirNode.FilePath)) // was expanded
                    thisTreeNode.IsExpanded = true;
                Parent.Items.Add(thisTreeNode);
            }
            void AddFile(in TreeViewItem Parent, SFCodeProjectNode FileNode)
            {
                var fileInfo = new FileInfo(FileNode.FilePath);
                var item = new TreeViewItem()
                {
                    Header = fileInfo.Name,
                    Tag = FileNode
                };
                if (!AppResources.IsFileIncluded(fileInfo))
                    item.SetResourceReference(TreeViewItem.StyleProperty, "FileTreeStyle");
                else item.SetResourceReference(TreeViewItem.StyleProperty, "FileImportTreeStyle");
                item.Selected += async delegate
                {
                    await ASMFileSelected(fileInfo);
                };
                if (selectedItemHeader != default && selectedItemHeader.FilePath == FileNode.FilePath)// was selected
                    item.BringIntoView();
                Parent.Items.Add(item);
            }
            SolutionExplorerView.Items.Add(AddProjectNode(currentProject.ParentNode));
        }

        private void ReadyImporters()
        {
            ASMImport.SetImports(AppResources.Includes.ToArray());
            MAPImport.SetImports(AppResources.Includes.ToArray());
        }

        public DispatcherOperation HandleViewModes() => Dispatcher.InvokeAsync(async delegate
        {
            ViewASMButton.Checked -= ViewASMButton_Checked;
            ViewMapButton.Checked -= ViewMapButton_Checked;
            ViewASMButton.IsChecked = false;
            ViewMapButton.IsChecked = false;
            ViewModeHost.Visibility = Visibility.Visible;
            switch (CurrentMode)
            {
                default:
                case ViewMode.NONE:
                    ViewModeHost.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.ASM:
                    await ASMViewer.Unpause();
                    MAPViewer.Pause();
                    ViewModeHost.SelectedItem = ASMTab;
                    ViewASMButton.IsChecked = true;
                    break;
                case ViewMode.MAP:
                    MAPViewer.Unpause();
                    ASMViewer.Pause();
                    ViewModeHost.SelectedItem = MAPTab;
                    ViewMapButton.IsChecked = true;
                    break;
            }
            ViewASMButton.Checked += ViewASMButton_Checked;
            ViewMapButton.Checked += ViewMapButton_Checked;
        });

        private async Task ASMFileSelected(FileInfo File)
        {            
            LoadingSpan.Visibility = Visibility.Visible;
            //GET IMPORTS SET
            ReadyImporters();
            //SWITCH TO ASM VIEWER IF WE HAVEN'T ALREADY
            if (CurrentMode is ViewMode.NONE) CurrentMode = ViewMode.ASM;
            //HANDLE VIEW MODES -- PAUSE / ENABLE VIEW MODE CONTROLS
            await HandleViewModes();
            //DO FILE PARSE NOW            
            ASMFile asmfile = default;
            bool isMap = false;
            if (File.Extension.ToUpper().EndsWith("ASM"))
            {
                asmfile = await MAPImport.ImportAsync(File.FullName);
                isMap = true;
            }
            else asmfile = await ASMImport.ImportAsync(File.FullName); 
            AppResources.OpenFiles.Add(asmfile);
            
            // FILE INCLUDE ROUTINE
            if (File.Extension.ToUpper().EndsWith("INC"))
            { // INC files should be included automatically
                if (!AppResources.IsFileIncluded(File))
                {
                    //INCLUDE FILE FOR SYMBOL LINKING
                    AppResources.Includes.Add(asmfile);
                }
            }
            // GET DEFAULT ACTION
            if (isMap)
            { // IF THIS IS A MAP -- SWITCH VIEW, INCUR UPDATE. THE MAP VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.MAP;
                await HandleViewModes();
            }
            //ENQUEUE THIS FILE TO BE OPENED BY THE ASM VIEWER
            await ASMViewer.OpenFileContents(File, asmfile); // tell the ASMControl to look at the new file            
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
            MacroFileCombo.SelectedValue = System.IO.Path.GetFileNameWithoutExtension(File.Name);
        }

        private void UpdateInterface()
        {
            //update explorer
            Import();
            //UPDATE INCLUDES
            MacroFileCombo.ItemsSource = AppResources.Includes.Select(x => System.IO.Path.GetFileNameWithoutExtension(x.OriginalFilePath));
            //VIEW MODE
            if (CurrentMode is ViewMode.MAP) MAPViewer.InvalidateFiles();
        }

        private void ShowMacrosForFile(ASMFile file)
        {
            void AddSymbol<T>(T symbol) where T : ASMChunk, IASMNamedSymbol
            {
                MacroTooltip tooltip = new();
                tooltip.Attach(symbol);

                ListBoxItem item = new()
                {
                    Content = symbol.Name,
                    ToolTip = new ToolTip()
                    {
                        Background = null,
                        BorderBrush = null,
                        HasDropShadow = true,
                        Content = tooltip
                    },
                    Tag = symbol
                };
                MacroExplorerView.Items.Add(item);
            }
            MacroExplorerView.Items.Clear();
            if (MacroFilterRadio.IsChecked ?? false)
            {
                MacroExplorerView.SetBinding(Control.ForegroundProperty, new Binding("Foreground")
                {
                    Source = MacroFilterRadio,
                });
                var macros = file.Chunks.OfType<ASMMacro>(); // filter all chunks by macros only
                foreach (var macro in macros)
                    AddSymbol(macro);
            }
            else if (DefineFilterRadio.IsChecked ?? false)
            {
                MacroExplorerView.SetBinding(Control.ForegroundProperty, new Binding("Foreground")
                {
                    Source = DefineFilterRadio,
                });
                var defines = file.Constants; // constants are kept separate
                foreach (var define in defines)
                    if (define != default)
                        AddSymbol(define);
            }
        }

        private void FilterFileChanged(object sender, SelectionChangedEventArgs e)
        {
            int newSelection = MacroFileCombo.SelectedIndex;
            if (newSelection < 0) return;
            var file = AppResources.Includes.ElementAtOrDefault(newSelection);
            if (file == null) return;
            ShowMacrosForFile(file);
        }

        private async void MacroExplorerView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MacroExplorerView.SelectedItem == null) return;
            if (((ListBoxItem)MacroExplorerView.SelectedItem).Tag is ASMChunk chunk)
                await ASMViewer.OpenSymbol(chunk);
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            FilterFileChanged(sender, null);
        }

        private void ViewMapButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.MAP;
            HandleViewModes();
            UpdateInterface();
        }

        private void ViewASMButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.ASM;
            HandleViewModes();
            UpdateInterface();
        }
    }
}
