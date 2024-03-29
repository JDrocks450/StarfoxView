using Starfox.Editor;
using StarFox.Interop.ASM;
using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Dialogs;
using StarFoxMapVisualizer.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for creating an interactive editor environment
    /// </summary>
    internal static class EDITORStandard
    {
        /// <summary>
        /// There should only ever be one instance of the Edit screen at any given time
        /// </summary>
        internal static EditScreen? CurrentEditorScreen { get; set; }
        private static LoadingWindow? _loadingWindow;

        /// <summary>
        /// Shows a Loading... Window in the middle of the Window.
        /// <para/> If a EditScreen is created, it will be hidden using the <see cref="DimEditorScreen"/> method
        /// </summary>
        public static void ShowLoadingWindow()
        {
            DimEditorScreen();
            if (_loadingWindow == null)
                _loadingWindow = new LoadingWindow()
                {
                    Owner = Application.Current.MainWindow
                };
            _loadingWindow.Show();
        }
        /// <summary>
        /// Hides the <see cref="ShowLoadingWindow"/> window
        /// </summary>
        public static void HideLoadingWindow()
        {
            UndimEditorScreen();
            if (_loadingWindow == null) return;
            _loadingWindow?.Hide();
        }
        /// <summary>
        /// If an <see cref="EditScreen"/> is added to this program, this will dim it.
        /// </summary>
        public static void DimEditorScreen()
        {
            if (CurrentEditorScreen != default)
                CurrentEditorScreen.LoadingSpan.Visibility = System.Windows.Visibility.Visible;
        }
        /// <summary>
        /// See: <see cref="DimEditorScreen"/>
        /// </summary>
        public static void UndimEditorScreen()
        {
            if (CurrentEditorScreen != default)
                CurrentEditorScreen.LoadingSpan.Visibility = System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Changes the interface to be on the Editor passed as an argument
        /// </summary>
        public static async Task SwitchEditorView(EditScreen.ViewMode View) => 
            await CurrentEditorScreen.SwitchView(View);
        /// <summary>
        /// See: <see cref="SHAPEControl.ShowShape(string, int)"/>
        /// </summary>
        /// <param name="ShapeName"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public static async Task<bool> ShapeEditor_ShowShapeByName(string ShapeName, int Frame = -1)
        {
            if (!await CurrentEditorScreen.OBJViewer.ShowShape(ShapeName, Frame))
                return false;
            await SwitchEditorView(EditScreen.ViewMode.OBJ);
            return true;
        }
        /// <summary>
        /// This function is used when the user selects a MapNode in the MAPControl.
        /// <para/>Map Nodes can represent many types of information, using the <paramref name="ComponentSelected"/>
        /// can narrow down what the user actually meant to select to get more info on.
        /// <para/><paramref name="ComponentSelected"/> being null indicates it's unclear what they meant to select
        /// and the most generic action should be taken
        /// </summary>
        /// <param name="MapEvent"></param>
        /// <param name="ComponentSelected"></param>
        /// <returns></returns>
        public static Task<bool> MapEditor_MapNodeSelected(MAPEvent MapEvent, Type? Component) => 
            CurrentEditorScreen.MAPViewer.MapNodeSelected(MapEvent, Component);
        /// <summary>
        /// Opens the ASMViewer in the editor to the passed <see cref="ASMChunk"/>
        /// </summary>
        /// <param name="Symbol"></param>
        /// <returns></returns>
        public static async Task AsmEditor_OpenSymbol(ASMChunk Symbol)
        {
            await SwitchEditorView(EditScreen.ViewMode.ASM);
            await CurrentEditorScreen.ASMViewer.OpenSymbol(Symbol);            
        }

        /// <summary>
        /// Prompts the user to select a new shapes directory
        /// </summary>
        /// <returns></returns>
        private static DirectoryInfo? Editor_SelectShapeDirectory()
        {
            if (AppResources.ImportedProject.ShapesDirectoryPathSet)
                return new DirectoryInfo(AppResources.ImportedProject.ShapesDirectoryPath);
            var directory = FILEStandard.ShowGenericFileBrowser("Select a Directory that contains Shape files", true);
            if (directory == default) return default;
            AppResources.ImportedProject.ShapesDirectoryPath = directory;
            return new DirectoryInfo(directory);
        }
        /// <summary>
        /// Refreshes the SHAPESMap SFOptimizer directory with the latest 3D model list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="FileNotFoundException"></exception>
        private static async Task<SFOptimizerDataStruct?> Editor_RefreshShapeMap(string? InitialDirectory = default)
        {
            //This function will create a SHAPE Map -- a file that links SHAPESX.ASM files to Shape Names
            string dirString = InitialDirectory;
            DirectoryInfo? dirInfo = default;
            if (dirString != null) 
                dirInfo = new DirectoryInfo(dirString);
        retry:
            if (dirInfo == default)
                dirInfo = Editor_SelectShapeDirectory();
            if (dirInfo == default) return default; // User Cancelled
            StringBuilder errorBuilder = new(); // ERRORS
            Dictionary<string, string> shapeMap = new();
            //TEST SOMETHING OUT
            if (!File.Exists(Path.Combine(dirInfo.FullName, "shapes.asm")))
            {
                if (MessageBox.Show("It looks like the directory you selected doesn't have at least " +
                    "a SHAPES.ASM file in it. Have you selected the SHAPES directory in your workspace?\n" +
                    "\n" +
                    "Would you like to continue anyway? No will go back to file selection.", "Directory Selection Message",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    AppResources.ImportedProject.ShapesDirectoryPath = default;
                    dirInfo = null;
                    goto retry;
                }
            }
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in dirInfo.GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var bspFile = await FILEStandard.OpenBSPFile(file); // IMPORT THE BSP
                    foreach (var shape in bspFile.Shapes)
                    {
                        var sName = shape.Header.Name.ToUpper();
                        var fooSName = sName;
                        int tries = 1;
                        while (shapeMap.ContainsKey(fooSName))
                        {
                            fooSName = sName + "_" + tries;
                            tries++;
                        }
                        sName = fooSName;
                        shapeMap.Add(sName, file.Name);
                    }
                    int delta = bspFile.ShapeHeaderEntries.Count - bspFile.Shapes.Count;
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }
            }
            return new SFOptimizerDataStruct(SFOptimizerTypeSpecifiers.Shapes, dirInfo.FullName, shapeMap);
        }
        /// <summary>
        /// Prompts the user to export all 3D models and will export them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static async Task Editor_ExportAll3DShapes()
        {
            // EXPORT 3D FUNCTION -- I MADE HISTORY HERE TODAY. 11:53PM 03/31/2023 JEREMY GLAZEBROOK.
            // I MADE A GUI PROGRAM THAT EXTRACTED STARFOX SHAPES SUCCESSFULLY AND DUMPED THEM ALL IN READABLE FORMAT.
            var r = MessageBox.Show($"Welcome to the Export 3D Assets Wizard!\n" +
                $"This tool will do the following: Export all 3D assets from the selected directory to *.sfshape files and palettes.\n" +
                $"It will dump them to the exports/models directory.\n" +
                $"You will get a manifest of all files dumped with their model names as well.\n" +
                $"Happy hacking! - Love Bisquick <3", "Export 3D Assets Wizard", MessageBoxButton.OKCancel); // WELCOME MSG
            if (r is MessageBoxResult.Cancel) return; // OOPSIES!
            var dirInfo = Editor_SelectShapeDirectory();
            if (dirInfo == default) return; // OOPSIES!
            EDITORStandard.ShowLoadingWindow();
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
                        var files = await SHAPEStandard.ExportShapeToSfShape(shape); // USE STANDARD EXPORT FUNC
                        if (files.Count() == 0) continue; // HUH, WEIRD?
                        foreach (var eFile in files) // EXPORTED FILES
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
            HideLoadingWindow();
        }

        private static async Task<SFOptimizerDataStruct?> Editor_RefreshLevelMap(string? InitialDirectory = default)
        {
            //This function will create a STAGE Map -- a file that links LEVEL.ASM files to Level Macro Names
            string? dirString = InitialDirectory;
        retry:
            if (dirString == default)
                dirString = FILEStandard.ShowGenericFileBrowser("Select your LEVELS Directory", true);
            if (dirString == default) return default; // User Cancelled
            StringBuilder errorBuilder = new(); // ERRORS
            Dictionary<string, string> stageMap = new();
            //TEST SOMETHING OUT
            if (!File.Exists(Path.Combine(dirString, "level1_1.asm")))
            {
                if (MessageBox.Show("It looks like the directory you selected doesn't have at least " +
                    "a level1_1.ASM file in it. Have you selected the LEVELS directory in your workspace?\n" +
                    "\n" +
                    "Would you like to continue anyway? No will go back to file selection.", "Directory Selection Message",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    dirString = null;
                    goto retry;
                }
            }
            //GET IMPORTS SET
            FILEStandard.ReadyImporters();
            foreach (var file in new DirectoryInfo(dirString).GetFiles()) // ITERATE OVER DIR FILES
            {
                try
                {
                    var mapFile = await FILEStandard.OpenMAPFile(file); // IMPORT THE MAP
                    foreach (var level in mapFile.Scripts)
                    {
                        var sName = level.Key;
                        stageMap.TryAdd(sName, file.Name);
                    }
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine($"The file: {file.FullName} could not be exported.\n" +
                        $"***\n{ex.ToString()}\n***"); // ERROR INFO
                }
            }
            return new SFOptimizerDataStruct(SFOptimizerTypeSpecifiers.Levels, dirString, stageMap);
        }

        /// <summary>
        /// Refreshes the map that is provided using the <paramref name="Type"/> parameter
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        internal static Task<SFOptimizerDataStruct?> Editor_RefreshMap(SFOptimizerTypeSpecifiers Type, string? InitialDirectory = null)
        {
            switch (Type)
            {
                case SFOptimizerTypeSpecifiers.Shapes:
                    return Editor_RefreshShapeMap(InitialDirectory);
                case SFOptimizerTypeSpecifiers.Levels:
                    return Editor_RefreshLevelMap(InitialDirectory);
            }
            return default;
        }
    }
}
