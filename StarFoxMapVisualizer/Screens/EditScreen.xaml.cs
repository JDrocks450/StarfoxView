using Microsoft.WindowsAPICodePack.Dialogs;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.MAP;
using StarFox.Interop.MSG;
using StarFox.Interop.SPC;
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
            NONE,
            ASM,
            MAP,
            OBJ,
            GFX,
            MSG,
            BRR
        }        

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
        /// <summary>
        /// Will Load (or Reload) the current project
        /// <para></para>
        /// </summary>
        /// <param name="Flush"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        internal async Task ImportCodeProject(bool Flush = false)
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
                importItem.Click += delegate
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
                    var result = await FILEStandard.IncludeFile<object>(new FileInfo(FileNode.FilePath), SFFileType.ASMFileTypes.ASM) != default;
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
                    var result = await FILEStandard.TryIncludeColorTable(new FileInfo(FileNode.FilePath));
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
                            if (await FILEStandard.IncludeFile<object>(fileInfo)!=default)
                                goto retry;
                            CreateINCContextMenu(FileNode, in contextMenu);
                            item.Foreground = Brushes.White; // Indicate with white that it isn't included yet
                        }
                        break;
                    case SFCodeProjectFileTypes.SCR:
                        item.SetResourceReference(StyleProperty, "ScreenTreeStyle");
                        if (AppResources.OpenFiles.ContainsKey(fileInfo.FullName))
                            CreateClosableContextMenu(FileNode, in contextMenu);
                        break;
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
                            CreateINCContextMenu(FileNode, in contextMenu);
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
        /// <summary>
        /// Should be called after changing the <see cref="CurrentMode"/> property
        /// <para>Will update the interface to match the new Mode</para>
        /// </summary>
        /// <returns></returns>
        public DispatcherOperation HandleViewModes() => Dispatcher.InvokeAsync(async delegate
        {
            //UNSUBSCRIBE FROM ALL BUTTONS FIRST 
            ViewASMButton.Checked -= ViewASMButton_Checked;
            ViewMapButton.Checked -= ViewMapButton_Checked;
            ViewBSTButton.Checked -= ViewBSTButton_Checked;
            ViewGFXButton.Checked -= ViewGFXButton_Checked;
            ViewMSGButton.Checked -= ViewMSGButton_Checked;
            ViewBRRButton.Checked -= ViewBRRButton_Checked;
            
            //UNCHECK EM ALL
            ViewGFXButton.IsChecked = false;
            ViewASMButton.IsChecked = false;
            ViewMapButton.IsChecked = false;
            ViewBSTButton.IsChecked = false;
            ViewMSGButton.IsChecked = false;
            ViewBRRButton.IsChecked = false;

            //VIEW MODES ENABLED
            ViewModeHost.Visibility = Visibility.Visible;
            MAPViewer.Pause();
            ASMViewer.Pause();
            OBJViewer.Pause();
            switch (CurrentMode)
            {
                default:
                case ViewMode.NONE:
                    ViewModeHost.Visibility = Visibility.Collapsed;
                    break;
                case ViewMode.ASM:
                    await ASMViewer.Unpause();                
                    ViewModeHost.SelectedItem = ASMTab;
                    ViewASMButton.IsChecked = true;
                    TitleBlock.Text = "Assembly Viewer";
                    break;
                case ViewMode.MAP:
                    MAPViewer.Unpause();
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
                    ViewModeHost.SelectedItem = OBJTab;
                    ViewBSTButton.IsChecked = true;
                    TitleBlock.Text = "Shape Viewer";
                    break;
                case ViewMode.GFX:                    
                    GFXViewer.RefreshFiles();
                    ViewModeHost.SelectedItem = GFXTab;
                    ViewGFXButton.IsChecked = true;
                    TitleBlock.Text = "Graphics Viewer";
                    break;
                case ViewMode.MSG:
                    await MSGViewer.RefreshFiles();
                    ViewModeHost.SelectedItem = MSGTab;
                    ViewMSGButton.IsChecked = true;
                    TitleBlock.Text = "Message Viewer";
                    break;
                case ViewMode.BRR:
                    BRRViewer.RefreshFiles();
                    ViewModeHost.SelectedItem = BRRTab;
                    ViewBRRButton.IsChecked = true;
                    TitleBlock.Text = "SFX Viewer";
                    break;
            }
            ViewASMButton.Checked += ViewASMButton_Checked;
            ViewMapButton.Checked += ViewMapButton_Checked;
            ViewBSTButton.Checked += ViewBSTButton_Checked;
            ViewGFXButton.Checked += ViewGFXButton_Checked;
            ViewMSGButton.Checked += ViewMSGButton_Checked;
            ViewBRRButton.Checked += ViewBRRButton_Checked;
        });               
        /// <summary>
        /// Will handle known file types and return true if handled.
        /// <para>Returns false if not handled. Returns default if the user cancels.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        private async Task<bool?> HandleKnownFileTypes(FileInfo File)
        {
            switch (File.GetSFFileType())
            { // YEAH?
                case SFCodeProjectFileTypes.Palette: // HANDLE PALETTE
                    await FILEStandard.OpenPalette(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    return true;
                case SFCodeProjectFileTypes.BRR:
                    await AUDIOStandard.OpenBRR(File);
                    UpdateInterface();
                    CurrentMode = ViewMode.BRR;
                    await HandleViewModes();
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    BRRViewer.SelectFile(File.FullName);
                    return true;
                case SFCodeProjectFileTypes.SPC:
                    await AUDIOStandard.OpenSPCProperties(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    return true;
                case SFCodeProjectFileTypes.BINFile: // EXTRACT BIN
                    // DOUBT AS TO FILE TYPE
                    //CREATE THE MENU WINDOW
                    SFFileType.BINFileTypes selectFileType = SFFileType.BINFileTypes.COMPRESSED_CGX;
                    BINImportMenu importMenu = new()
                    {
                        Owner = Application.Current.MainWindow
                    };
                    if (!importMenu.ShowDialog() ?? true) return default; // USER CANCEL
                    selectFileType = importMenu.FileType;
                    switch (selectFileType)
                    {
                        case SFFileType.BINFileTypes.COMPRESSED_CGX:
                            await SFGFXInterface.TranslateDATFile(File.FullName);
                            UpdateInterface(true); // Files changed!
                            break;
                        case SFFileType.BINFileTypes.BRR:
                            await AUDIOStandard.OpenBRR(File);
                            UpdateInterface();
                            CurrentMode = ViewMode.BRR;
                            await HandleViewModes();
                            break;
                        case SFFileType.BINFileTypes.SPC:
                            if (await AUDIOStandard.ConvertBINToSPC(File))
                                UpdateInterface(true);
                            break;
                    }
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    return true;
                case SFCodeProjectFileTypes.CCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await GFXStandard.ExtractCCR(File);
                        LoadingSpan.Visibility = Visibility.Collapsed;
                        UpdateInterface(true); // Files changed!
                    }
                    return true;
                case SFCodeProjectFileTypes.PCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await SFGFXInterface.TranslateCompressedPCR(File.FullName);
                        LoadingSpan.Visibility = Visibility.Collapsed;
                        UpdateInterface(true); // Files changed!
                    }
                    return true;
                case SFCodeProjectFileTypes.SCR: // screens
                    //OPEN THE SCR FILE
                    GFXStandard.OpenSCR(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
                    return true;
                case SFCodeProjectFileTypes.CGX: // graphics
                    //OPEN THE CGX FILE
                    await GFXStandard.OpenCGX(File);
                    LoadingSpan.Visibility = Visibility.Collapsed;
                    UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
                    return true;
            }
            return false;
        }
        /// <summary>
        /// A file has been selected in the GUI
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        private async Task FileSelected(FileInfo File)
        {                        
            LoadingSpan.Visibility = Visibility.Visible;
            //CHECK IF ITS A KNOWN FILE
            var result = await HandleKnownFileTypes(File);
            if (!result.HasValue || result.Value) return; // Handled or User Cancelled
            //SWITCH TO ASM VIEWER IF WE HAVEN'T ALREADY
            CurrentMode = ViewMode.ASM;
            //HANDLE VIEW MODES -- PAUSE / ENABLE VIEW MODE CONTROLS
            await HandleViewModes();
            //DO FILE PARSE NOW            
            var asmfile = await FILEStandard.OpenASMFile(File);
            bool isMap = asmfile is MAPFile;
            bool isObj = asmfile is BSPFile;
            bool isMSG = asmfile is MSGFile;
            if (asmfile == default)
            {
                LoadingSpan.Visibility = Visibility.Collapsed;
                return;
            }
            // FILE INCLUDE ROUTINE
            if (File.GetSFFileType() is SFCodeProjectFileTypes.Include && !isMSG)
            { // INC files should be included automatically -- generally.
                FILEStandard.IncludeFile(asmfile);
            }
            // GET DEFAULT ACTION
            if (isMap)
            { // IF THIS IS A MAP -- SWITCH VIEW, INCUR UPDATE. THE MAP VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.MAP;
                await HandleViewModes();
            }
            else if (isObj)
            { // IF THIS IS AN OBJ -- SWITCH VIEW, INCUR UPDATE. THE OBJ VIEW WILL SEE THE NEWLY ADDED FILE
                CurrentMode = ViewMode.OBJ;
                await HandleViewModes();
            }
            else if (isMSG)
            {
                CurrentMode = ViewMode.MSG;
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
        /// <summary>
        /// Shows what Macros are defined in the current <see cref="ASMFile"/>
        /// </summary>
        /// <param name="file"></param>
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
        /// <summary>
        /// Raised when the User changes the file they are viewing Macros on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterFileChanged(object sender, SelectionChangedEventArgs e)
        {
            int newSelection = MacroFileCombo.SelectedIndex;
            if (newSelection < 0) return;
            var file = AppResources.Includes.ElementAtOrDefault(newSelection);
            if (file == null) return;
            ShowMacrosForFile(file);
        }
        /// <summary>
        /// Raised when the user selects a Macro to view in the ASMViewer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MacroExplorerView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MacroExplorerView.SelectedItem == null) return;
            if (((ListBoxItem)MacroExplorerView.SelectedItem).Tag is ASMChunk chunk)
            {
                CurrentMode = ViewMode.ASM;
                await HandleViewModes();
                await ASMViewer.OpenSymbol(chunk);
            }
        }
        /// <summary>
        /// Raised when the user changes the current file viewing Macros on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            FilterFileChanged(sender, null);
        }
        /// <summary>
        /// MapViewer Button Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewMapButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.MAP;
            HandleViewModes();
            UpdateInterface();
        }
        /// <summary>
        /// ASM Button Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewASMButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.ASM;
            HandleViewModes();
            UpdateInterface();
        }
        /// <summary>
        /// Model Viewer Button Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewBSTButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.OBJ;
            HandleViewModes();
            UpdateInterface();
        }
        /// <summary>
        /// Graphics Button Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewGFXButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.GFX;
            HandleViewModes();
            UpdateInterface();
        }
        /// <summary>
        /// Messages Button Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewMSGButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.MSG;
            HandleViewModes();
            UpdateInterface();
        }
        /// <summary>
        /// Sound (Samples) Button Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewBRRButton_Checked(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.BRR;
            HandleViewModes();
            UpdateInterface();
        }
        /// <summary>
        /// Prompts the user to select a new shapes directory
        /// </summary>
        /// <returns></returns>
        private DirectoryInfo? Generic_SelectShapeDirectory()
        {
            if (AppResources.ImportedProject.ShapesDirectoryPathSet)
                return new DirectoryInfo(AppResources.ImportedProject.ShapesDirectoryPath);
            CommonOpenFileDialog d = new CommonOpenFileDialog()
            {
                Title = "Select a Directory that contains Shape files",
                IsFolderPicker = true,
                Multiselect = false,
                InitialDirectory = AppResources.ImportedProject.WorkspaceDirectory.FullName
            }; // CREATE THE FOLDER PICKER
            if (d.ShowDialog() is not CommonFileDialogResult.Ok) return default; // OOPSIES x2
            var directory = d.FileName; // Selected DIR
            if (!Directory.Exists(directory)) return default; // Random error?
            AppResources.ImportedProject.ShapesDirectoryPath= directory;
            return new DirectoryInfo(directory);
        }
        /// <summary>
        /// Prompts the user to export all 3D models and will export them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            var dirInfo = Generic_SelectShapeDirectory();
            if (dirInfo == default) return; // OOPSIES!
            LoadingSpan.Visibility = Visibility.Visible;
            StringBuilder errorBuilder = new(); // ERRORS
            StringBuilder exportedBSPs = new(); // BSPS
            StringBuilder exportedFiles = new(); // ALL FILES
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in dirInfo.GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
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
        /// <summary>
        /// Refreshes the SHAPESMap SFOptimizer directory with the latest 3D model list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="FileNotFoundException"></exception>
        private async void SHAPEMapRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            //This function will create a SHAPE Map -- a file that links SHAPESX.ASM files to Shape Names
            var dirInfo = Generic_SelectShapeDirectory();
            if (dirInfo == default) return; // User Cancelled
            StringBuilder errorBuilder = new(); // ERRORS
            Dictionary<string, string> shapeMap = new();
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in dirInfo.GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
                    foreach(var shape in bspFile.Shapes)
                    {
                        var sName = shape.Header.UniqueName;
                        var fooSName = sName;
                        int tries = 1;
                        while (shapeMap.ContainsKey(fooSName))
                        {
                            fooSName = sName + "_" + tries;
                            tries++;
                        }
                        sName = fooSName.ToUpper();
                        shapeMap.Add(sName, file.Name);
                    }
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }
            }
            var dirNode = AppResources.ImportedProject.SearchDirectory(dirInfo.Name).FirstOrDefault();
            if (dirNode == default)
                throw new FileNotFoundException("Couldn't find the node that matches this directory in the Code Project.");
            dirNode.AddOptimizer("ShapesMap", new SFOptimizerDataStruct(
                SFOptimizerTypeSpecifiers.Shapes, shapeMap));
            MessageBox.Show("The ShapesMap Code Project Optimizer has been updated.");
            UpdateInterface(true); // files updated!
        }
        /// <summary>
        /// Opens the Level Background viewer dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BGSASMViewerButton_Click(object sender, RoutedEventArgs e)
        {
            var file = FILEStandard.MAPImport?.LoadedContextDefinitions;
            if (file == null)
            {
                MessageBox.Show("Level contexts have not been loaded yet. Open a level file to have this information populated.");
                return;
            }
            LevelContextViewer viewer = new LevelContextViewer(file)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            viewer.Show();
        }        
    }
}
