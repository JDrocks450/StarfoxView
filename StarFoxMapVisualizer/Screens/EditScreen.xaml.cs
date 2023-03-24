using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX.COL;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Controls;
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
            NONE,ASM,MAP,OBJ
        }

        private ASMImporter ASMImport = new();
        private MAPImporter MAPImport = new();
        private BSPImporter BSPImport = new();
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

        private void ImportCodeProject()
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
            void CreateINCContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Include File")
            {
                //INCLUDE FILE ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO INCLUDE
                    var result = await IncludeFile(new FileInfo(FileNode.FilePath));
                    if (!result)
                    {
                        MessageBox.Show("That file could not be imported at this time.", "File Include Error");
                    }
                    UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
            void CreateCOLTABContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Include File as Color Table")
            {
                //INCLUDE FILE ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO INCLUDE
                    var result = await TryIncludeColorTable(new FileInfo(FileNode.FilePath));
                    if (!result)
                    {
                        MessageBox.Show("That file could not be imported at this time.", "File Include Error");
                    }
                    UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
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
                var contextMenu = new ContextMenu();
                var item = new TreeViewItem()
                {
                    Header = fileInfo.Name,
                    Tag = FileNode,
                    ContextMenu= contextMenu
                };
                if (FileNode.RecognizedFileType is SFCodeProjectFileTypes.Palette)
                {
                    item.SetResourceReference(StyleProperty, "PaletteTreeStyle");
                    if (!AppResources.IsFileIncluded(fileInfo))
                    {
                        CreateINCContextMenu(FileNode, in contextMenu);
                        item.Foreground = Brushes.White; // Indicate with white that it isn't included yet
                    }
                }
                else if (FileNode.RecognizedFileType is SFCodeProjectFileTypes.Include)
                {
                    if (!AppResources.IsFileIncluded(fileInfo))
                    {
                        item.SetResourceReference(StyleProperty, "FileTreeStyle");
                        CreateINCContextMenu(FileNode, in contextMenu);
                    }
                    else item.SetResourceReference(StyleProperty, "FileImportTreeStyle");
                }
                else
                { // allow other files to be included under certain circumstances
                    var include = AppResources.ImportedProject?.GetInclude(fileInfo);
                    if (include != default) // file is included
                    {
                        if (include is COLTABFile) // color tables
                            item.SetResourceReference(StyleProperty, "ColorTableTreeStyle");
                        else // all other files
                            item.SetResourceReference(StyleProperty, "FileImportTreeStyle");
                    }
                    else
                    {
                        CreateCOLTABContextMenu(FileNode, in contextMenu);
                        item.SetResourceReference(StyleProperty, "FileTreeStyle");
                    }
                }
                item.Selected += async delegate
                {
                    await FileSelected(fileInfo);
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
            BSPImport.SetImports(AppResources.Includes.ToArray());
        }

        public DispatcherOperation HandleViewModes() => Dispatcher.InvokeAsync(async delegate
        {
            ViewASMButton.Checked -= ViewASMButton_Checked;
            ViewMapButton.Checked -= ViewMapButton_Checked;
            ViewBSTButton.Checked -= ViewBSTButton_Checked;
            ViewASMButton.IsChecked = false;
            ViewMapButton.IsChecked = false;
            ViewBSTButton.IsChecked = false;
            ViewModeHost.Visibility = Visibility.Visible;
            switch (CurrentMode)
            {
                default:
                case ViewMode.NONE:
                    ViewModeHost.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.ASM:
                    OBJViewer.Pause();
                    await ASMViewer.Unpause();
                    MAPViewer.Pause();
                    ViewModeHost.SelectedItem = ASMTab;
                    ViewASMButton.IsChecked = true;
                    TitleBlock.Text = "Assembly Viewer";
                    break;
                case ViewMode.MAP:
                    OBJViewer.Pause();
                    MAPViewer.Unpause();
                    ASMViewer.Pause();
                    ViewModeHost.SelectedItem = MAPTab;
                    ViewMapButton.IsChecked = true;
                    TitleBlock.Text = "Map Event Node Viewer";
                    break;
                case ViewMode.OBJ:
                    try
                    {
                        OBJViewer.Unpause();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"The Shape Viewer has reported an error: {ex.Message}");
                    }
                    MAPViewer.Pause();
                    ASMViewer.Pause();
                    ViewModeHost.SelectedItem = OBJTab;
                    ViewBSTButton.IsChecked = true;
                    TitleBlock.Text = "Shape Viewer";
                    break;
            }
            ViewASMButton.Checked += ViewASMButton_Checked;
            ViewMapButton.Checked += ViewMapButton_Checked;
            ViewBSTButton.Checked += ViewBSTButton_Checked;
        });       

        public async Task<ASMFile?> ParseFile(FileInfo File)
        {
            //MAP IMPORT LOGIC
            async Task<ASMFile?> doMAPImport()
            {
                var message = MAPImport.CheckWarningMessage(File.FullName);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (MessageBox.Show(message, "Continue?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        return default;
                }
                return await MAPImport.ImportAsync(File.FullName);
            }
            //3D IMPORT LOGIC
            async Task<ASMFile?> doBSPImport()
            {
                var message = BSPImport.CheckWarningMessage(File.FullName);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (MessageBox.Show(message, "Continue?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        return default;
                }
                var file = await BSPImport.ImportAsync(File.FullName);               
            skipTree:
                return file;
            }

            //GET IMPORTS SET
            ReadyImporters();
            //DO FILE PARSE NOW            
            ASMFile? asmfile = default;
            if (File.GetSFFileType() is SFCodeProjectFileTypes.Assembly) // assembly file
            { // DOUBT AS TO FILE TYPE
                //CREATE THE MENU WINDOW
                FileImportMenu importMenu = new()
                {
                    Owner = Application.Current.MainWindow
                };
                if (!importMenu.ShowDialog() ?? true) return default; // USER CANCEL
                switch (importMenu.FileType)
                {
                    default: return default;
                    case StarFox.Interop.SFFileType.FileTypes.ASM:
                        goto general;
                    case StarFox.Interop.SFFileType.FileTypes.MAP:
                        asmfile = await doMAPImport();
                        break;
                    case StarFox.Interop.SFFileType.FileTypes.BSP:
                        asmfile = await doBSPImport();
                        break;
                }                
                goto import;
            }
        general:
            asmfile = await ASMImport.ImportAsync(File.FullName);
        import:
            if (asmfile == default) return default;
            AppResources.OpenFiles.Add(asmfile);
            return asmfile;
        }
        private async Task<bool> TryIncludeColorTable(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                var importer = new COLTABImporter();
                importer.SetImports(AppResources.Includes.ToArray());
                var message = importer.CheckWarningMessage(File.FullName);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (MessageBox.Show(message, "Continue?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        return default;
                }
                var result = await importer.ImportAsync(File.FullName);
                if (result == default) return false;
                AppResources.Includes.Add(result); // INCLUDE FILE FOR SYMBOL LINKING
                var msg = string.Join(Environment.NewLine, result.Groups);
                MessageBox.Show(msg, "Success!", MessageBoxButton.OKCancel);
            }
            return true;
        }
        private async Task<bool> IncludeFile(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                switch (File.GetSFFileType())
                {
                    case SFCodeProjectFileTypes.Include:
                    case SFCodeProjectFileTypes.Assembly:
                        var asmFile = await ParseFile(File);
                        if (asmFile == default) return false; // USER CANCEL                                                              
                        AppResources.Includes.Add(asmFile); // INCLUDE FILE FOR SYMBOL LINKING
                        return true;
                    case SFCodeProjectFileTypes.Palette:
                        using (var file = File.OpenRead())
                        {
                            var palette = StarFox.Interop.GFX.CAD.COL.Load(file);
                            if (palette == default) return false;
                            AppResources.ImportedProject.Palettes.Add(File.FullName, palette);
                        }
                        break;
                }
            }
            return true;
        }
        private void IncludeFile(ASMFile asmFile)
        {
            if (!AppResources.IsFileIncluded(new FileInfo(asmFile.OriginalFilePath)))
            {
                //INCLUDE FILE FOR SYMBOL LINKING
                AppResources.Includes.Add(asmFile);
            }
        }

        private async Task OpenPalette(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                var success = await IncludeFile(File);
                if (!success) return;                
            }
            var col = AppResources.ImportedProject.Palettes[File.FullName];
            PaletteView view = new()
            {                
                Owner = Application.Current.MainWindow
            };
            view.SetupControl(col);
            view.ShowDialog();
            UpdateInterface();
        }

        private async Task FileSelected(FileInfo File)
        {                        
            LoadingSpan.Visibility = Visibility.Visible;
            //IS THIS A PALETTE
            if (File.GetSFFileType() is SFCodeProjectFileTypes.Palette)
            { // YEAH!
                await OpenPalette(File); // GOOD, OPEN IT
                LoadingSpan.Visibility = Visibility.Collapsed;
                return;
            }        
            //SWITCH TO ASM VIEWER IF WE HAVEN'T ALREADY
            CurrentMode = ViewMode.ASM;
            //HANDLE VIEW MODES -- PAUSE / ENABLE VIEW MODE CONTROLS
            await HandleViewModes();
            //DO FILE PARSE NOW            
            var asmfile = await ParseFile(File);
            bool isMap = asmfile is MAPFile;
            bool isObj = asmfile is BSPFile;
            if (asmfile == default)
            {
                LoadingSpan.Visibility = Visibility.Collapsed;
                return;
            }
            // FILE INCLUDE ROUTINE
            if (File.GetSFFileType() is SFCodeProjectFileTypes.Include)
            { // INC files should be included automatically
                IncludeFile(asmfile);
            }
            // GET DEFAULT ACTION
            if (isMap)
            { // IF THIS IS A MAP -- SWITCH VIEW, INCUR UPDATE. THE MAP VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.MAP;
                await HandleViewModes();
            }
            if (isObj)
            { // IF THIS IS AN OBJ -- SWITCH VIEW, INCUR UPDATE. THE OBJ VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.OBJ;
                await HandleViewModes();
            }
            else
            {
                //ENQUEUE THIS FILE TO BE OPENED BY THE ASM VIEWER
                await ASMViewer.OpenFileContents(File, asmfile); // tell the ASMControl to look at the new file            
            }
            UpdateInterface();
            LoadingSpan.Visibility = Visibility.Collapsed;
            MacroFileCombo.SelectedValue = System.IO.Path.GetFileNameWithoutExtension(File.Name);
        }

        private void UpdateInterface()
        {
            //update explorer
            ImportCodeProject();
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
        private void ViewBSTButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.OBJ;
            HandleViewModes();
            UpdateInterface();
        }
    }
}
