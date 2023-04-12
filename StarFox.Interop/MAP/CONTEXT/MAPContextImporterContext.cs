﻿using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;

namespace StarFox.Interop.MAP.CONTEXT
{
    internal class MAPContextImporterContext : ImporterContext<ASMFile>
    {
        internal new MAPContextFile CurrentFile { get; }
        internal MAPContextDefinition CurrentDefinition { get; private set; }
        private bool BGINITIALIZED = false;

        internal MAPContextImporterContext(MAPContextFile ParentFile) { 
            CurrentFile = ParentFile;
        }
        /// <summary>
        /// Uses the current line to start a definition, if possible
        /// </summary>
        /// <param name="line"></param>
        internal bool CheckStartDefinition(ASMLine line)
        {
            if (!line.HasInlineLabel) return false; // needs to have a label
            //has label
            StartDefinition(line.InlineLabel);
            return true;    
        }
        internal bool StartDefinition(string Name)
        {
            var mapInitName = Name;
            CurrentDefinition = new MAPContextDefinition(mapInitName);
            CurrentFile.Definitions.Add(mapInitName, CurrentDefinition);
            BGINITIALIZED = false;
            return true;
        }
        /// <summary>
        /// Reads the contents of the line to check for whether or not any recognizable information is given.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        internal bool CheckLineContents(ASMLine line)
        {
            if (CurrentDefinition == null) return false;
            if (!line.HasStructureApplied) return false;
            if (!(line.Structure is ASMMacroInvokeLineStructure macroInvokation)) return false;
            string? paramStr(int index) => macroInvokation.TryGetParameter(index)?.ParameterContent;
            string[] paramsStr = macroInvokation.Parameters.
                Select(x => x?.ParameterContent).Where(y => !string.IsNullOrWhiteSpace(y)).ToArray();                
            string? param1str = paramStr(0);
            string macroName = macroInvokation.MacroReference.Name.ToLower();
            if (!BGINITIALIZED && macroName != "init_bg") return false; // if the bg isn't initialized this could be unsafe code.
            switch (macroName)
            {
                case "init_bg":
                    BGINITIALIZED = true;
                    return true;
                case "bg2chr": // SET SCREEN CHARS (TILES)
                    SetChr(param1str);
                    return true;
                case "bg2scr": // SET SCREEN ITSELF
                    SetScr(param1str); return true;                    
                case "palette": // Background Palette
                    SetBGPalette(param1str);
                    return true;
                case "gamepal": // Game 2D Palette
                    Set2DPalette(param1str);
                    return true;
                case "info": // Game 2D Palette                    
                    SetGameplayFlags(paramsStr);
                    return true;
                case "bg3chr": // SET SCREEN CHARS BG3 (TILES)
                    SetChr3(param1str);
                    return true;
                case "bg3scr": // SET SCREEN ITSELF BG3
                    SetScr3(param1str); 
                    return true;
                case "bgm": // background music
                    SetBgm(param1str);
                    return true;
                case "water": // map appearances
                case "tunnel":
                case "planet":
                case "space":
                case "nucleus":
                case "final":
                case "undergnd":
                    SetupMapAppearance(macroName, param1str);
                    return true;
            }
            return false;
        }
        private void SetupMapAppearance(string LevelMode, string? Palette3D)
        {
            CurrentDefinition.AppearancePreset = LevelMode;
            if (Palette3D == null) return;
            CurrentDefinition.ShapePalette = Palette3D;            
        }
        private void SetBgm(string? BGM)
        {
            if (BGM == null) return;
            CurrentDefinition.BackgroundMusic = BGM;
        }
        private void SetGameplayFlags(string[] paramsStr) => CurrentDefinition.ImportFlags(paramsStr);
        private void Set2DPalette(string? GamePalette)
        {
            if (GamePalette == null) return;
            CurrentDefinition.GamePalette = GamePalette;
        }
        private void SetBGPalette(string? PalName)
        {
            if (PalName == null) return;
            CurrentDefinition.BackgroundPalette = PalName;
        }
        private void SetChr(string? ChrFileName)
        {
            if (ChrFileName== null) return;
            CurrentDefinition.BG2ChrFile = ChrFileName;
        }
        private void SetScr(string? ScrFileName)
        {
            if (ScrFileName == null) return;
            CurrentDefinition.BG2ScrFile = ScrFileName;
        }
        private void SetChr3(string? ChrFileName)
        {
            if (ChrFileName == null) return;
            CurrentDefinition.BG3ChrFile = ChrFileName;
        }
        private void SetScr3(string? ScrFileName)
        {
            if (ScrFileName == null) return;
            CurrentDefinition.BG3ScrFile = ScrFileName;
        }
        internal void EndDefinition()
        {
            CurrentDefinition = null;
            BGINITIALIZED = false;
        }
    }
}