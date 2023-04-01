using Microsoft.WindowsAPICodePack.Dialogs;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
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
using System.Windows.Media;
using System.Windows.Threading;

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for EditScreen.xaml
    /// </summary>
    public partial class EditScreen : Page
    {
        public enum ViewMode
        {
            NONE,ASM,MAP,OBJ,
            GFX
        }

        private ASMImporter ASMImport = new();
        private MAPImporter MAPImport = new();
        private BSPImporter BSPImport = new();
        private COLTABImporter COLTImport = new();

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

        private async Task ImportCodeProject(bool Flush = false)
        {            
            var currentProject = AppResources.ImportedProject;
            if (currentProject == null)
                throw new InvalidDataException("No project loaded.");
            if (Flush)
                await currentProject.EnumerateAsync();
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
            void CreateClosableContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Close File")
            {
                //INCLUDE FILE ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO INCLUDE
                    var result = AppResources.ImportedProject.CloseFile(FileNode.FilePath);
                    if (!result)
                    {
                        MessageBox.Show("That file could not be closed at this time.", "File Close Error");
                    }
                    UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
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
            async Task<TreeViewItem> AddProjectNode(SFCodeProjectNode Node)
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
                            await AddDirectory(node, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            await AddFile(node, child);
                            break;
                    }
                }
                return node;
            }
            async Task AddDirectory(TreeViewItem Parent, SFCodeProjectNode DirNode)
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
                            await AddDirectory(thisTreeNode, child);
                            break;
                        case SFCodeProjectNodeTypes.File:
                            await AddFile(thisTreeNode, child);
                            break;
                    }
                }
                if (expandedHeaders.Contains(DirNode.FilePath)) // was expanded
                    thisTreeNode.IsExpanded = true;
                Parent.Items.Add(thisTreeNode);
            }
            async Task AddFile(TreeViewItem Parent, SFCodeProjectNode FileNode)
            {
                var fileInfo = new FileInfo(FileNode.FilePath);
                var contextMenu = new ContextMenu();
                var item = new TreeViewItem()
                {
                    Header = fileInfo.Name,
                    Tag = FileNode,
                    ContextMenu= contextMenu
                };
                switch (FileNode.RecognizedFileType)
                {
                    case SFCodeProjectFileTypes.Palette:
                        retry:
                        item.SetResourceReference(StyleProperty, "PaletteTreeStyle");
                        if (!AppResources.IsFileIncluded(fileInfo))
                        {
                            if (await IncludeFile(fileInfo))
                                goto retry;
                            CreateINCContextMenu(FileNode, in contextMenu);
                            item.Foreground = Brushes.White; // Indicate with white that it isn't included yet
                        }
                        break;
                    case SFCodeProjectFileTypes.SCR:
                    case SFCodeProjectFileTypes.CGX:
                        item.SetResourceReference(StyleProperty, "SpriteTreeStyle");
                        if (AppResources.OpenFiles.ContainsKey(fileInfo.FullName))
                            CreateClosableContextMenu(FileNode, in contextMenu);
                        break;
                    case SFCodeProjectFileTypes.Include:
                        if (!AppResources.IsFileIncluded(fileInfo))
                        {
                            item.SetResourceReference(StyleProperty, "FileTreeStyle");
                            CreateINCContextMenu(FileNode, in contextMenu);
                        }
                        else item.SetResourceReference(StyleProperty, "FileImportTreeStyle");
                        break;
                    default:
                        // allow other files to be included under certain circumstances
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
                        break;
                }
                item.Selected += async delegate
                {
                    try
                    {
                        await FileSelected(fileInfo);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"The importer responsible for this type of file says: \n" +
                            $"{ex}\n" +
                            $"Sorry!", "Error Loading File");
                        return;
                    }
                    finally
                    {
                        LoadingSpan.Visibility = Visibility.Collapsed;
                    }
                };
                if (selectedItemHeader != default && selectedItemHeader.FilePath == FileNode.FilePath)// was selected
                    item.BringIntoView();
                Parent.Items.Add(item);
            }
            SolutionExplorerView.Items.Add(await AddProjectNode(currentProject.ParentNode));
        }

        private void ReadyImporters()
        {
            COLTImport.SetImports(AppResources.Includes.ToArray());
            ASMImport.SetImports(AppResources.Includes.ToArray());
            MAPImport.SetImports(AppResources.Includes.ToArray());
            BSPImport.SetImports(AppResources.Includes.ToArray());
        }

        public DispatcherOperation HandleViewModes() => Dispatcher.InvokeAsync(async delegate
        {
            ViewASMButton.Checked -= ViewASMButton_Checked;
            ViewMapButton.Checked -= ViewMapButton_Checked;
            ViewBSTButton.Checked -= ViewBSTButton_Checked;
            ViewGFXButton.Checked -= ViewGFXButton_Checked;
            ViewGFXButton.IsChecked = false;
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
                case ViewMode.GFX:
                    MAPViewer.Pause();
                    ASMViewer.Pause();
                    OBJViewer.Pause();
                    GFXViewer.RefreshFiles();
                    ViewModeHost.SelectedItem = GFXTab;
                    ViewGFXButton.IsChecked = true;
                    TitleBlock.Text = "Graphics Viewer";
                    break;
            }
            ViewASMButton.Checked += ViewASMButton_Checked;
            ViewMapButton.Checked += ViewMapButton_Checked;
            ViewBSTButton.Checked += ViewBSTButton_Checked;
            ViewGFXButton.Checked += ViewGFXButton_Checked;
        });

        private bool SearchProjectForFile(string FileName, out FileInfo? File)
        {
            File = null;
            var results = AppResources.ImportedProject.SearchFile(FileName);
            if (results.Count() == 0) return false;
            if (results.Count() > 1) // ambiguous
                return false;
            File = new FileInfo(results.First().FilePath);
            return true;
        }

        private async Task<bool> HandleImportMessages<T>(FileInfo File, CodeImporter<T> importer) where T : IImporterObject
        {
            async Task AutoIncludeNow(string message, IEnumerable<string> ExpectedIncludes)
            {
                //**AUTO INCLUDE
                List<string> autoIncluded = new List<string>();
                if (!string.IsNullOrWhiteSpace(message))
                { // attempt to silence the warning
                    var includes = ExpectedIncludes;
                    foreach (var include in includes)
                    {
                        if (!SearchProjectForFile(include, out var file)) continue;
                        await IncludeFile(file);
                        autoIncluded.Add(file.Name);
                    }
                }
                if (autoIncluded.Any())
                    MessageBox.Show($"Auto-Include included these files to the project:\n" +
                        $" {string.Join(", ", autoIncluded)}");
                //** END AUTO INCLUDE
            }
            var message = importer.CheckWarningMessage(File.FullName);
            await AutoIncludeNow(message, importer.ExpectedIncludes);
            ReadyImporters();
            message = BSPImport.CheckWarningMessage(File.FullName);
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (MessageBox.Show(message, "Continue?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return false;
            }
            return true;
        }
        //3D IMPORT LOGIC
        async Task<BSPFile?> doBSPImport(FileInfo File)
        {
            if (!await HandleImportMessages(File, BSPImport)) return default;
            //**AUTO-INCLUDE COLTABS.ASM
            if (SearchProjectForFile("coltabs.asm", out var projFile))
                await TryIncludeColorTable(projFile);
            //**
            var file = await BSPImport.ImportAsync(File.FullName);
            return file;
        }
        public async Task<ASMFile?> ParseFile(FileInfo File)
        {                        
            //MAP IMPORT LOGIC
            async Task<ASMFile?> doMAPImport()
            {
                if (!await HandleImportMessages(File, MAPImport)) return default;
                return await MAPImport.ImportAsync(File.FullName);
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
                        asmfile = await doBSPImport(File);
                        break;
                }                
                goto import;
            }
        general:
            asmfile = await ASMImport.ImportAsync(File.FullName);
        import:
            if (asmfile == default) return default;
            AppResources.OpenFiles.Add(File.FullName,asmfile);
            return asmfile;
        }
        private async Task<bool> TryIncludeColorTable(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                ReadyImporters();
                if (!await HandleImportMessages(File, COLTImport)) return false;
                var result = await COLTImport.ImportAsync(File.FullName);
                if (result == default) return false;
                AppResources.Includes.Add(result); // INCLUDE FILE FOR SYMBOL LINKING
                var msg = string.Join(Environment.NewLine, result.Groups);
                MessageBox.Show(msg, "Success!", MessageBoxButton.OKCancel);
                if (!AppResources.ImportedProject.Palettes.Any())
                {
                    if (!SearchProjectForFile("night.col", out var file)) return false;
                    await IncludeFile(file);
                }
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
            if (AppResources.OpenFiles.ContainsKey(File.FullName))
                AppResources.ImportedProject.CloseFile(File.FullName);
            //CHECK IF ITS A KNOWN FILE
            switch (File.GetSFFileType())
            { // YEAH?
                case SFCodeProjectFileTypes.Palette: // HANDLE PALETTE
                    await OpenPalette(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    return;
                case SFCodeProjectFileTypes.BINFile: // EXTRACT BIN
                    await SFGFXInterface.TranslateDATFile(File.FullName);            
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface(true); // Files changed!
                    return;
                case SFCodeProjectFileTypes.CCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        BPPDepthMenu menu = new()
                        {
                            Owner = Application.Current.MainWindow
                        };
                        if (!menu.ShowDialog() ?? true) return;
                        await SFGFXInterface.TranslateCompressedCCR(File.FullName, menu.FileType);
                        LoadingSpan.Visibility = Visibility.Collapsed;
                        UpdateInterface(true); // Files changed!
                    }
                    return;
                case SFCodeProjectFileTypes.PCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await SFGFXInterface.TranslateCompressedPCR(File.FullName);
                        LoadingSpan.Visibility = Visibility.Collapsed;
                        UpdateInterface(true); // Files changed!
                    }
                    return;
                case SFCodeProjectFileTypes.SCR: // screens
                    //OPEN THE SCR FILE
                    if (!AppResources.OpenFiles.ContainsKey(File.FullName))
                    {
                        //ATTEMPT TO OPEN THE FILE AS WELL-FORMED
                        var fxGFX = await SFGFXInterface.OpenSCR(File.FullName);
                        if (fxGFX == null)
                        { // NOPE CAN'T DO THAT
                            //OKAY, TRY TO IMPORT IT
                            fxGFX = await SFGFXInterface.ImportSCR(File.FullName);
                        }
                        if (fxGFX == null) throw new Exception("That file cannot be opened or imported."); // GIVE UP
                        //ADD IT AS AN OPEN FILE
                        AppResources.OpenFiles.Add(File.FullName, fxGFX);
                    }
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
                    return;
                case SFCodeProjectFileTypes.CGX: // graphics
                    //OPEN THE CGX FILE
                    if (!AppResources.OpenFiles.ContainsKey(File.FullName))
                    {                        
                        //ATTEMPT TO OPEN THE FILE AS WELL-FORMED
                        var fxGFX = await SFGFXInterface.OpenCGX(File.FullName);
                        if (fxGFX == null)
                        { // NOPE CAN'T DO THAT
                            BPPDepthMenu menu = new()
                            {
                                Owner = Application.Current.MainWindow
                            };
                            if (!menu.ShowDialog() ?? true) return; // USER CANCELLED!
                            //OKAY, TRY TO IMPORT IT WITH THE SPECIFIED BITDEPTH
                            fxGFX = await SFGFXInterface.ImportCGX(File.FullName, menu.FileType);
                        }
                        if (fxGFX == null) throw new Exception("That file cannot be opened or imported."); // GIVE UP
                        //ADD IT AS AN OPEN FILE
                        AppResources.OpenFiles.Add(File.FullName, fxGFX);
                    }
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
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

        private async void UpdateInterface(bool FlushFiles = false)
        {
            //update explorer
            await ImportCodeProject(FlushFiles);
            //UPDATE INCLUDES
            MacroFileCombo.ItemsSource = AppResources.Includes.Select(x => System.IO.Path.GetFileNameWithoutExtension(x.OriginalFilePath));
            //VIEW MODE
            if (CurrentMode is ViewMode.MAP) MAPViewer.InvalidateFiles();
            if (CurrentMode is ViewMode.GFX) GFXViewer.RefreshFiles();
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

        private void ViewGFXButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.GFX;
            HandleViewModes();
            UpdateInterface();
        }

        private async void ExportAll3DButton_Click(object sender, RoutedEventArgs e)
        {
            // EXPORT 3D FUNCTION -- I MADE HISTORY HERE TODAY. 11:53PM 03/31/2023 JEREMY GLAZEBROOK.
            // I EXTRACTED STARFOX SHAPES SUCCESSFULLY AND DUMPED THEM ALL IN READABLE FORMAT.
            var r = MessageBox.Show($"Welcome to the Export 3D Assets Wizard!\n" +
                $"This tool will do the following: Export all 3D assets from the selected directory to *.sfshape files and palettes.\n" +
                $"It will dump them to the exports/models directory.\n" +
                $"You will get a manifest of all files dumped with their model names as well.\n" +
                $"Happy hacking! - Love Bisquick <3", "Export 3D Assets Wizard", MessageBoxButton.OKCancel); // WELCOME MSG
            if (r is MessageBoxResult.Cancel) return; // OOPSIES!
            CommonOpenFileDialog d = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Multiselect = false,
                InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName
            }; // CREATE THE FOLDER PICKER
            if (d.ShowDialog() is not CommonFileDialogResult.Ok) return; // OOPSIES x2
            var directory = d.FileName; // Selected DIR
            if (!Directory.Exists(directory)) return; // Random error?
            var dirInfo = new DirectoryInfo(directory); 
            LoadingSpan.Visibility = Visibility.Visible;
            StringBuilder errorBuilder = new(); // ERRORS
            StringBuilder exportedBSPs = new(); // BSPS
            StringBuilder exportedFiles = new(); // ALL FILES
            //GET IMPORTS SET
            ReadyImporters();
            foreach (var file in dirInfo.GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await doBSPImport(file); // IMPORT THE BSP
                    foreach (var shape in bspFile.Shapes) // FIND ALL SHAPES
                    {
                        var files =await SHAPEStandard.ExportShapeToSfShape(shape); // USE STANDARD EXPORT FUNC
                        if (files.Count() == 0) continue; // HUH, WEIRD?
                        foreach(var eFile in files) // EXPORTED FILES
                            exportedFiles.AppendLine(eFile);
                        var bspFileAddr = files.ElementAt(0);
                        exportedBSPs.AppendLine(bspFileAddr); // BSP ONLY
                    }
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }                
            }
            //CREATE THE MANIFEST FILE
            File.WriteAllText(System.IO.Path.Combine(SHAPEStandard.DefaultShapeExtractionDirectory, "manifest.txt"), exportedBSPs.ToString());
            MessageBox.Show($"{exportedFiles}", "Exported Files");
            if (!string.IsNullOrWhiteSpace(errorBuilder.ToString()))
                MessageBox.Show($"{errorBuilder}", "Errors");
            if (MessageBox.Show($"Files exported to:\n" +
                $"{SHAPEStandard.DefaultShapeExtractionDirectory}\n" +
                $"Do you want to copy its location to the clipboard?", "Complete",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Clipboard.SetText(SHAPEStandard.DefaultShapeExtractionDirectory);
            LoadingSpan.Visibility = Visibility.Collapsed;
        }
    }
}
