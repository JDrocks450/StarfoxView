using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Screens;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for interacting with GFX files in the editor environment
    /// </summary>
    internal static class GFXStandard
    {
        /// <summary>
        /// Extracts a CCR file, shows the BitDepth dialog, and finally returns the file path of the created object.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        internal static async Task<string?> ExtractCCR(FileInfo File) {
            BPPDepthMenu menu = new()
            {
                Owner = Application.Current.MainWindow
            };
            if (!menu.ShowDialog() ?? true) return default;
            var ccr = await SFGFXInterface.TranslateCompressedCCR(File.FullName, menu.FileType);
            await EditScreen.Current.ImportCodeProject(true); // UPDATE PROJECT FILES
            return ccr;
        }
        /// <summary>
        /// Extracts a PCR file and returns the file path of the created object.
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        internal static async Task<string> ExtractPCR(FileInfo File)
        {
            var pcr = await SFGFXInterface.TranslateCompressedPCR(File.FullName);
            await EditScreen.Current.ImportCodeProject(true); // UPDATE PROJECT FILES
            return pcr;
        }
        /// <summary>
        /// Translates the BGS.ASM naming scheme of palettes to be the actual name of the palette and returns the palette
        /// <para>Also returns the system file path of the palette so you can find it.</para>
        /// </summary>
        /// <param name="MAPContextColorPaletteName"></param>
        /// <param name="PaletteFullPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static COL? MAPContext_GetPaletteByName(string? MAPContextColorPaletteName, out string? PaletteFullPath)
        {
            var ColorPaletteName = MAPContextColorPaletteName;
            if (ColorPaletteName == default)
                throw new ArgumentNullException(nameof(ColorPaletteName) + " was not set on this context." +
                    " Can't render this without a palette.");
            ColorPaletteName = ColorPaletteName.ToLower() switch
            {
                "2a" => "BG2-A",
                "2b" => "BG2-B",
                "2c" => "BG2-C",
                "2d" => "BG2-D",
                "2e" => "BG2-E",
                "2f" => "BG2-F",
                "2g" => "BG2-G",
                "tm" => "T-M",
                "tm2" => "T-M-2",
                "tm3" => "T-M-3",
                "tm4" => "T-M-4",
                _ => ColorPaletteName
            };
            PaletteFullPath = default;
            //CHECK IF THE PALETTE IS INCLUDED FIRST
            var results = AppResources.ImportedProject.Palettes.FirstOrDefault(
                x => Path.GetFileNameWithoutExtension(x.Key).Replace("-", "").ToUpper() == ColorPaletteName.Replace("-", "").ToUpper());
            if (results.Value == default) return default;
            PaletteFullPath = results.Key;
            return results.Value;
        }
        /// <summary>
        /// Will search through project files searching for the CGX with the provided name.
        /// <para>If the CGX is extracted, and <paramref name="ForceExtractCCR"/> is false, it will return the extracted one.</para>
        /// <para>If it isn't extracted, it will extract the *.CCR, if found. Returns the CGX that was extracted.</para>
        /// </summary>
        /// <param name="CHRName"></param>
        /// <param name="ForceExtractCCR"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        internal static async Task<FileInfo> FindProjectCGXByName(string CHRName, bool ForceExtractCCR = false)
        {
            if (!FILEStandard.SearchProjectForFile($"{CHRName}.CGX", out var CGXFileInfo, true) || ForceExtractCCR) // CGX File Search
            {
                if (ForceExtractCCR && CGXFileInfo != default)
                    AppResources.ImportedProject.CloseFile(CGXFileInfo.FullName);
                if (!FILEStandard.SearchProjectForFile($"{CHRName}.CCR", out var CCRFileInfo, true)) // CCR File Search                    
                    throw new FileNotFoundException($"The CGX file(s) (or CCR files) requested were not found.\n" +
                        $"{CHRName}");
                //EXTRACT CCR
                var cgxPath = await ExtractCCR(CCRFileInfo);
                if (string.IsNullOrWhiteSpace(cgxPath))
                    throw new Exception($"{cgxPath} could not be found, or {CHRName} could not be extracted.");
                CGXFileInfo = new FileInfo(cgxPath);
            }
            return CGXFileInfo;
        }
        /// <summary>
        /// Will search through project files searching for the SCR with the provided name.
        /// <para>If the SCR is extracted, and <paramref name="ForceExtractPCR"/> is false, it will return the extracted one.</para>
        /// <para>If it isn't extracted, it will extract the *.PCR, if found. Returns the SCR that was extracted.</para>
        /// </summary>
        /// <param name="SCRName"></param>
        /// <param name="ForceExtractPCR"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        internal static async Task<FileInfo> FindProjectSCRByName(string SCRName, bool ForceExtractPCR = false)
        {
            if (!FILEStandard.SearchProjectForFile($"{SCRName}.SCR", out var SCRFileInfo, true) || ForceExtractPCR) // SCR File Search
            {
                if (ForceExtractPCR && SCRFileInfo != default)
                    AppResources.ImportedProject.CloseFile(SCRFileInfo.FullName);
                if (!FILEStandard.SearchProjectForFile($"{SCRName}.PCR", out var PCRFileInfo, true)) // PCR File Search
                    throw new FileNotFoundException($"The SCR file(s) (or PCR files) requested were not found.\n" +
                        $"{SCRName}");
                //EXTRACT PCR
                var scrPath = await ExtractPCR(PCRFileInfo);
                if (string.IsNullOrWhiteSpace(scrPath))
                    throw new Exception($"{scrPath} could not be found, or {SCRName} could not be extracted.");
                SCRFileInfo = new FileInfo(scrPath);
            }
            return SCRFileInfo;
        }
        /// <summary>
        /// Will create a <see cref="BitmapSource"/> containing the rendered *.SCR file using only the Color Palette's
        /// name, the SCR's name, and optionally the *.CGX file's name.
        /// <para>In reference to <see cref="FindProjectCGXByName(string, bool)"/> and <see cref="FindProjectSCRByName(string, bool)"/>:</para>
        /// <para>Will search through project files searching for the Graphics Resource with the provided name.
        /// <para>If the Graphics Resource is extracted, and <paramref name="ForceExtractPCR"/> is false, it will return the extracted one.</para>
        /// <para>If it isn't extracted, it will extract the *.Compressed Graphics Resource, if found. Returns the Graphics Resource that was extracted.</para>
        /// </para>
        /// </summary>
        /// <param name="ColorPaletteName">The name of the color palette (not a file path)</param>
        /// <param name="SCRName">The name of the SCR file (not a file path)</param>
        /// <param name="CHRName">The name of the CGX file (not a file path). If default will just use the SCR name.</param>
        /// <returns></returns>
        internal static async Task<Bitmap> RenderSCR(string ColorPaletteName, string SCRName, string? CHRName, 
            bool ForceExtractCCR = false, bool ForceExtractPCR = false)            
        {          
            var palette = MAPContext_GetPaletteByName(ColorPaletteName, out _);
            if (palette == default) throw new FileNotFoundException($"{ColorPaletteName} was not found as" +
                $" an included Palette in this project."); // NOPE IT WASN'T
            //SET THE CHRName TO BE THE SCR NAME IF DEFAULT
            if (CHRName == null) CHRName = SCRName;
            //MAKE SURE BOTH OF THESE FILES ARE EXTRACTED AND EXIST
            //SEARCH AND EXTRACT CGX FIRST
            var CGXFileInfo = await FindProjectCGXByName(CHRName, ForceExtractCCR);
            //THEN SCR
            var SCRFileInfo = await FindProjectSCRByName(SCRName, ForceExtractPCR);
            return await RenderSCR(palette, CGXFileInfo, SCRFileInfo);
        }
        /// <summary>
        /// Will render the provided <paramref name="SCR"/> file, using the given <paramref name="CGX"/> file as a base,
        /// and colorized with the provided <paramref name="Palette"/>.
        /// <para>All GFX resources must be extracted. Need help extracting? Use: <see cref="RenderSCR(string, string, string?, bool, bool)"/></para>
        /// </summary>
        /// <param name="Palette">The palette to use.</param>
        /// <param name="CGX">The file name of the CGX file to use. Has to be extracted.</param>
        /// <param name="SCR">The file name of the SCR file to use. Has to be extracted.</param>
        /// <returns></returns>
        internal static async Task<Bitmap> RenderSCR(COL Palette, FileInfo CGX, FileInfo SCR)
        {
            //LOAD THE CGX
            var fxCGX = await OpenCGX(CGX);
            //LOAD THE SCR
            var fxSCR = OpenSCR(SCR);
            //RENDER OUT
            return fxSCR.Render(fxCGX, Palette);
        }
        /// <summary>
        /// Includes a *.CGX file into the project. 
        /// <para>This function will NOT extract a *.CGR file.</para>
        /// <para>This function spawns dialog modals.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static async Task<FXCGXFile?> OpenCGX(FileInfo File)
        {
            if (!AppResources.OpenFiles.ContainsKey(File.FullName))
            {
                //ATTEMPT TO OPEN THE FILE AS WELL-FORMED
                var fxGFX = SFGFXInterface.OpenCGX(File.FullName);
                if (fxGFX == null)
                { // NOPE CAN'T DO THAT
                    BPPDepthMenu menu = new()
                    {
                        Owner = Application.Current.MainWindow
                    };
                    if (!menu.ShowDialog() ?? true) return default; // USER CANCELLED!
                    //OKAY, TRY TO IMPORT IT WITH THE SPECIFIED BITDEPTH
                    fxGFX = await SFGFXInterface.ImportCGX(File.FullName, menu.FileType);
                }
                if (fxGFX == null) throw new Exception("That file cannot be opened or imported."); // GIVE UP
                //ADD IT AS AN OPEN FILE
                AppResources.OpenFiles.Add(File.FullName, fxGFX);
                return fxGFX;
            }
            else return AppResources.OpenFiles[File.FullName] as FXCGXFile;
        }

        /// <summary>
        /// Includes a *.SCR file into the project. 
        /// <para>This function will NOT extract a *.PCR file.</para>
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static FXSCRFile OpenSCR(FileInfo File)
        {
            if (!AppResources.OpenFiles.ContainsKey(File.FullName))
            {
                //ATTEMPT TO OPEN THE FILE AS WELL-FORMED
                var fxGFX = SFGFXInterface.OpenSCR(File.FullName);
                if (fxGFX == null)
                { // NOPE CAN'T DO THAT
                  //OKAY, TRY TO IMPORT IT
                    fxGFX = SFGFXInterface.ImportSCR(File.FullName);
                }
                if (fxGFX == null) throw new Exception("That file cannot be opened or imported."); // GIVE UP
                //ADD IT AS AN OPEN FILE
                AppResources.OpenFiles.Add(File.FullName, fxGFX);
                return fxGFX;
            }
            else return (FXSCRFile)AppResources.OpenFiles[File.FullName];
        }
    }
}
