using Microsoft.WindowsAPICodePack.Dialogs;
using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFox.Interop.MAP;
using StarFox.Interop.MSG;
using StarFox.Interop.SPC;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Controls2;
using StarFoxMapVisualizer.Dialogs;
using StarFoxMapVisualizer.Misc;
using StarFoxMapVisualizer.Renderers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using static StarFox.Interop.GFX.DAT.MSPRITES.MSprite;

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for EditScreen.xaml
    /// </summary>
    public partial class EditScreen : Page
    {
        /// <summary>
        /// See: <see cref="EDITORStandard.CurrentEditorScreen"/>
        /// </summary>
        internal static EditScreen Current => EDITORStandard.CurrentEditorScreen;

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
        
        public ViewMode CurrentMode { get; set; }

        public EditScreen()
        {
            InitializeComponent();

            SolutionExplorerView.Items.Clear();
            MacroExplorerView.Items.Clear();            

            Loaded += OnLoad;
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            CurrentMode = ViewMode.NONE;
            await HandleViewModes();
            await UpdateInterface();
            EDITORStandard.HideLoadingWindow();
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

            //Show welcome wagon if not shown once to the user yet this session
            bool changesMade = await EDITORStandard.WelcomeWagon();            

            if (Flush || changesMade)
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

            void CreateClosableContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Close File")
            {
                //CLOSABLE FILE ITEM
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
                    _ = UpdateInterface();
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
                    await UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
            void CreateIncludeDirectoryAsBRRContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Open All *.BRR Files")
            {
                //INCLUDE DIRECTORY ITEM
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    EDITORStandard.ShowLoadingWindow();
                    foreach (var brrNode in FileNode.ChildNodes.Where(x => x.RecognizedFileType is SFCodeProjectFileTypes.BRR))
                        await AUDIOStandard.OpenBRR(new FileInfo(brrNode.FilePath), false, false, true);
                    await UpdateInterface();
                    CurrentMode = ViewMode.BRR;
                    await HandleViewModes();
                    EDITORStandard.HideLoadingWindow();                    
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
                    await UpdateInterface();
                };
                contextMenu.Items.Add(importItem);
            }
            void CreateExploreContextMenu(SFCodeProjectNode FileNode, in ContextMenu contextMenu, string Message = "Show in File Explorer")
            {
                //FILE EXPLORER CONTEXT MENU
                var importItem = new MenuItem()
                {
                    Header = Message
                };
                importItem.Click += async delegate
                {
                    //DO ACTION
                    Process.Start("explorer.exe", $"/select,\"{FileNode.FilePath}\"");
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
                ContextMenu menu = new();
                TreeViewItem thisTreeNode = new()
                {
                    Header = System.IO.Path.GetFileName(DirNode.FilePath),
                    Tag = DirNode,
                    ContextMenu = menu
                };
                thisTreeNode.SetResourceReference(TreeViewItem.StyleProperty, "FolderTreeStyle");
                CreateIncludeDirectoryAsBRRContextMenu(DirNode, menu);
                CreateExploreContextMenu(DirNode, menu);
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
                CreateExploreContextMenu(FileNode, contextMenu);
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
                        AppResources.ShowCrash(ex, false, "The plug-in selected could not complete that task.");
                        return;
                    }
                    finally
                    {
                        EDITORStandard.HideLoadingWindow();
                    }
                };
                if (selectedItemHeader != default && selectedItemHeader.FilePath == FileNode.FilePath)// was selected
                    item.BringIntoView();
                Parent.Items.Add(item);
            }
            SolutionExplorerView.Items.Add(await AddProjectNode(currentProject.ParentNode));            
        }
        /// <summary>
        /// Changes the current Editor View Mode to the one provided
        /// </summary>
        /// <param name="View"></param>
        /// <returns></returns>
        public async Task SwitchView(ViewMode View)
        {
            if (View == CurrentMode) return;
            CurrentMode = View;
            await HandleViewModes();
        }
        /// <summary>
        /// Should be called after changing the <see cref="CurrentMode"/> property
        /// <para>Will update the interface to match the new Mode</para>
        /// </summary>
        /// <returns></returns>
        public DispatcherOperation HandleViewModes() => Dispatcher.InvokeAsync(async delegate
        {
            //FIRST LOAD
            MainViewerBorder.Visibility = Visibility.Visible;

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
                    //ViewModeHost.Visibility = Visibility.Collapsed;
                    MainViewerBorder.Visibility = Visibility.Collapsed;
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
                    OBJViewer.Unpause();
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
                    EDITORStandard.HideLoadingWindow();
                    await UpdateInterface();
                    return true;
                case SFCodeProjectFileTypes.BRR:
                    await AUDIOStandard.OpenBRR(File);
                    await UpdateInterface();
                    CurrentMode = ViewMode.BRR;
                    await HandleViewModes();
                    EDITORStandard.HideLoadingWindow();
                    BRRViewer.SelectFile(File.FullName);
                    return true;
                case SFCodeProjectFileTypes.SPC:
                    await AUDIOStandard.OpenSPCProperties(File);
                    EDITORStandard.HideLoadingWindow();
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
                            await UpdateInterface(true); // Files changed!
                            break;
                        case SFFileType.BINFileTypes.BRR:
                            await AUDIOStandard.OpenBRR(File);
                            await UpdateInterface();
                            CurrentMode = ViewMode.BRR;
                            await HandleViewModes();
                            break;
                        case SFFileType.BINFileTypes.SPC:
                            if (await AUDIOStandard.ConvertBINToSPC(File))
                                await UpdateInterface(true);
                            break;
                    }
                    EDITORStandard.HideLoadingWindow();
                    return true;
                case SFCodeProjectFileTypes.CCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await GFXStandard.ExtractCCR(File);
                        EDITORStandard.HideLoadingWindow();
                        await UpdateInterface(true); // Files changed!
                    }
                    return true;
                case SFCodeProjectFileTypes.PCR: // EXTRACT COMPRESSED GRAPHICS
                    {
                        await SFGFXInterface.TranslateCompressedPCR(File.FullName);
                        EDITORStandard.HideLoadingWindow();
                        await UpdateInterface(true); // Files changed!
                    }
                    return true;
                case SFCodeProjectFileTypes.SCR: // screens
                    //OPEN THE SCR FILE
                    GFXStandard.OpenSCR(File);
                    EDITORStandard.HideLoadingWindow();
                    await UpdateInterface();
                    CurrentMode = ViewMode.GFX;
                    await HandleViewModes();
                    return true;
                case SFCodeProjectFileTypes.CGX: // graphics
                    //OPEN THE CGX FILE
                    await GFXStandard.OpenCGX(File);
                    EDITORStandard.HideLoadingWindow();
                    await UpdateInterface();
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
            EDITORStandard.ShowLoadingWindow();
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
            bool isDEFSPR = asmfile is MSpritesDefinitionFile;
            if (asmfile == default)
            {
                EDITORStandard.HideLoadingWindow();
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
            { // COMMUNICATIONS VIEWER
                CurrentMode = ViewMode.MSG;
                await HandleViewModes();
            }
            else if (isDEFSPR)
            { // 3D MSPRITES VIEWER
                MSpritesViewer viewer = new MSpritesViewer(asmfile as MSpritesDefinitionFile);
                viewer.Show();
            }
            else
            {
                //ENQUEUE THIS FILE TO BE OPENED BY THE ASM VIEWER
                await ASMViewer.OpenFileContents(File, asmfile); // tell the ASMControl to look at the new file            
            }
            await UpdateInterface();
            EDITORStandard.HideLoadingWindow();
            MacroFileCombo.SelectedValue = System.IO.Path.GetFileNameWithoutExtension(File.Name);
        }
        /// <summary>
        /// Refreshes the Workspace Explorer, Macros and Open Files for some Editors
        /// </summary>
        /// <param name="FlushFiles"></param>
        private async Task UpdateInterface(bool FlushFiles = false)
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
        /// Prompts the user to export all 3D models and will export them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExportAll3DButton_Click(object sender, RoutedEventArgs e) => await EDITORStandard.Editor_ExportAll3DShapes();

        private async Task SFOptimRefreshBase(SFOptimizerTypeSpecifiers Type, string Noun)
        {
            _ = await EDITORStandard.Editor_RefreshMap(Type);            
            UpdateInterface(true); // files updated!
        }

        /// <summary>
        /// Refreshes the SHAPESMap SFOptimizer directory with the latest 3D model list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="FileNotFoundException"></exception>
        private async void SHAPEMapRefreshButton_Click(object sender, RoutedEventArgs e) => 
            await SFOptimRefreshBase(SFOptimizerTypeSpecifiers.Shapes, "ShapesMap");
        /// <summary>
        /// Refreshes the STAGESMAP SFOptimizer directory with the latest level list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="FileNotFoundException"></exception>
        private async void STAGEMapRefreshButton_Click(object sender, RoutedEventArgs e) => 
            await SFOptimRefreshBase(SFOptimizerTypeSpecifiers.Maps, "StagesMap");
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

        private void ExitItem_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private async void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var file = FILEStandard.ShowGenericFileBrowser("Select a File to View");
            if (file == default || !file.Any()) return;
            await FileSelected(new FileInfo(file.First()));
        }

        private void CloseProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {            
            //Delete old project
            AppResources.ImportedProject = null;
            //switch to landing screen
            ((MainWindow)Application.Current.MainWindow).Content = new LandingScreen();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog settings = new();
            settings.Show();
        }

        /// <summary>
        /// Fired when the Go... item is opened (this loads Maps, stages, etc.)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoItem_Load(object sender, RoutedEventArgs e)
        {            
            GoItem.Items.Clear();
            foreach(var map in AppResources.ImportedProject.Optimizers)
            {
                if (map.OptimizerData == default) continue;
                MenuItem item = new MenuItem()
                {
                    Header = Enum.GetName(map.OptimizerData.TypeSpecifier),
                };                
                foreach(var mapItem in map.OptimizerData.ObjectMap.OrderBy(x => x.Key))
                {
                    var subItem = new MenuItem()
                    {
                        Header = mapItem.Key
                    };
                    subItem.Click += async delegate
                    {
                        var name = mapItem.Key;
                        try
                        {
                            EDITORStandard.ShowLoadingWindow();
                            await EDITORStandard.InvokeOptimizerMapItem(map.OptimizerData.TypeSpecifier, name);
                        }
                        catch (Exception e)
                        {
                            AppResources.ShowCrash(e, false, $"Couldn't open this {map.OptimizerData.TypeSpecifier} item.");
                        }
                        finally
                        {
                            EDITORStandard.HideLoadingWindow();
                        }
                    };
                    item.Items.Add(subItem);
                }
                GoItem.Items.Add(item);
            }
            GoItem.SubmenuOpened -= GoItem_Load;
        }

        /// <summary>
        /// Fired when the Level Select Menu item is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LevelSelectItem_Click(object sender, RoutedEventArgs e)
        {
            LevelSelectWindow wnd = new();
            wnd.Show();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new AboutBox() { Owner = Application.Current.MainWindow }.ShowDialog();
        }
    }
}
