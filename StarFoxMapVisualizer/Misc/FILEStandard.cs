using Starfox.Editor;
using StarFox.Interop;
using StarFox.Interop.ASM;
using StarFox.Interop.BSP;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Controls;
using StarFoxMapVisualizer.Controls.Subcontrols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer.Misc
{
    internal static class FILEStandard
    {
        private static ASMImporter ASMImport = new();
        private static MAPImporter MAPImport = new();
        private static BSPImporter BSPImport = new();
        private static COLTABImporter COLTImport = new();
        /// <summary>
        /// Includes a <see cref="SFCodeProjectFileTypes.Assembly"/>, <see cref="SFCodeProjectFileTypes.Include"/> or 
        /// <see cref="SFCodeProjectFileTypes.Palette"/>.
        /// <para>Note that passing generic type T is optional, since it will return default if it is not a matching type.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static async Task<T?> IncludeFile<T>(FileInfo File) where T : class
        {
            if (!AppResources.IsFileIncluded(File))
            {
                switch (File.GetSFFileType())
                {
                    case SFCodeProjectFileTypes.Include:
                    case SFCodeProjectFileTypes.Assembly:
                        var asmFile = await ParseFile(File);
                        if (asmFile == default) return default; // USER CANCEL                                                              
                        AppResources.Includes.Add(asmFile); // INCLUDE FILE FOR SYMBOL LINKING
                        return asmFile as T;
                    case SFCodeProjectFileTypes.Palette:
                        using (var file = File.OpenRead())
                        {
                            var palette = StarFox.Interop.GFX.CAD.COL.Load(file);
                            if (palette == default) return default;
                            AppResources.ImportedProject.Palettes.Add(File.FullName, palette);
                            return palette as T;
                        }
                }
            }
            else return AppResources.Includes.First(x => x.OriginalFilePath== File.FullName) as T;
            return default;
        }
        public static void IncludeFile(ASMFile asmFile)
        {
            if (!AppResources.IsFileIncluded(new FileInfo(asmFile.OriginalFilePath)))
            {
                //INCLUDE FILE FOR SYMBOL LINKING
                AppResources.Includes.Add(asmFile);
            }
        }
        public static bool SearchProjectForFile(string FileName, out FileInfo? File)
        {
            File = null;
            var results = AppResources.ImportedProject.SearchFile(FileName);
            if (results.Count() == 0) return false;
            if (results.Count() > 1) // ambiguous
                return false;
            File = new FileInfo(results.First().FilePath);
            return true;
        }
        public static void ReadyImporters()
        {
            COLTImport.SetImports(AppResources.Includes.ToArray());
            ASMImport.SetImports(AppResources.Includes.ToArray());
            MAPImport.SetImports(AppResources.Includes.ToArray());
            BSPImport.SetImports(AppResources.Includes.ToArray());
        }
        private static async Task<bool> HandleImportMessages<T>(FileInfo File, CodeImporter<T> importer) where T : IImporterObject
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
                        await IncludeFile<object>(file);
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
        public static async Task<bool> TryIncludeColorTable(FileInfo File)
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
                    await IncludeFile<ASMFile>(file);
                }
            }
            return true;
        }

        public static async Task OpenPalette(FileInfo File)
        {
            if (!AppResources.IsFileIncluded(File))
            {
                var success = await IncludeFile<object>(File) != default;
                if (!success) return;
            }
            var col = AppResources.ImportedProject.Palettes[File.FullName];
            PaletteView view = new()
            {
                Owner = Application.Current.MainWindow
            };
            view.SetupControl(col);
            view.ShowDialog();            
        }       
        public static bool CloseFileIfOpen(FileInfo File)
        {
            if (AppResources.OpenFiles.ContainsKey(File.FullName))
            {
                AppResources.ImportedProject.CloseFile(File.FullName);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Opens the specified BSP File, if it's already open, will return the file.
        /// </summary>
        /// <param name="File"></param>
        /// <param name="ForceReload">Forces the library to reload the file from disk.</param>
        /// <returns></returns>
        public static async Task<BSPFile?> OpenBSPFile(FileInfo File, bool ForceReload = false)
        {
            if (AppResources.ImportedProject.OpenFiles.TryGetValue(File.FullName, out var fileInstance))
            { // FILE IS OPEN
                if (fileInstance is BSPFile && !ForceReload) return fileInstance as BSPFile; // RETURN IT
                // WE HAVE TO RELOAD IT, SO CLOSE IT
                CloseFileIfOpen(File);
            }
            //3D IMPORT LOGIC
            if (!await HandleImportMessages(File, BSPImport)) return default;
            //**AUTO-INCLUDE COLTABS.ASM
            if (SearchProjectForFile("coltabs.asm", out var projFile))
                await TryIncludeColorTable(projFile);
            //**
            var file = await BSPImport.ImportAsync(File.FullName);
            return file;
        }
        public static async Task<ASMFile?> OpenMAPFile(FileInfo File)
        {
            //MAP IMPORT LOGIC   
            if (!await HandleImportMessages(File, MAPImport)) return default;
            return await MAPImport.ImportAsync(File.FullName);
        }
        private static async Task<ASMFile?> ParseFile(FileInfo File)
        {                     
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
                        asmfile = await OpenMAPFile(File);
                        break;
                    case StarFox.Interop.SFFileType.FileTypes.BSP:
                        asmfile = await OpenBSPFile(File);
                        break;
                }
                goto import;
            }
        general:
            asmfile = await ASMImport.ImportAsync(File.FullName);
        import:
            if (asmfile == default) return default;
            if (!AppResources.OpenFiles.ContainsKey(File.FullName))
                AppResources.OpenFiles.Add(File.FullName, asmfile);
            return asmfile;
        }
        internal static async Task<ASMFile?> OpenASMFile(FileInfo File)
        {
            //DO FILE PARSE NOW            
            return await ParseFile(File);            
        }
    }
}
