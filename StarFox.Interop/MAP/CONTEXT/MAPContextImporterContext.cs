using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;

namespace StarFox.Interop.MAP.CONTEXT
{
    internal class MAPContextImporterContext : ImporterContext<ASMFile>
    {
        internal new MAPContextFile CurrentFile { get; }
        internal MAPContextDefinition? CurrentDefinition { get; private set; }
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
        /// Uses <c>STRATEQU.INC</c> to find constants like ViewCY, etc.
        /// </summary>
        internal void SetLevelConstraints()
        {
            var stratequ = Includes.FirstOrDefault(x => ((IImporterObject)x).FileName == "STRATEQU");
            if (stratequ == null) throw new NullReferenceException("STRATEQU was not imported.");

            var Definition = CurrentDefinition;

            string noun = Definition.AppearancePreset;

            if (!stratequ.ConstantExists(noun + "_viewCY"))
                noun = "planet"; // default to planet

            Definition.ViewCY = stratequ[noun + "_viewCY"];
            Definition.MinX = stratequ[noun + "_minX"];
            Definition.MaxX = stratequ[noun + "_maxX"];
            Definition.M_MinX = stratequ[noun + "_MminX"];
            Definition.M_MaxX = stratequ[noun + "_MmaxX"];
            Definition.MinY = stratequ[noun + "_minY"];
            Definition.MaxY = stratequ[noun + "_maxY"];
            Definition.M_MaxY = stratequ[noun + "_MmaxY"];
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
                    SetChr(macroInvokation);
                    return true;
                case "bg2scr": // SET SCREEN ITSELF
                    SetScr(macroInvokation); return true;                    
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
                    SetChr3(macroInvokation);
                    return true;
                case "bg3scr": // SET SCREEN ITSELF BG3
                    SetScr3(macroInvokation); 
                    return true;
                case "bg2xscroll":
                case "bg2hoff":
                    {
                        var hoff = macroInvokation.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
                        CurrentDefinition.BG2.HorizontalOffset = hoff;
                    }
                    return true;
                case "bg2yscroll":
                    {
                        var vofs = macroInvokation.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
                        CurrentDefinition.BG2.VerticalOffset = vofs;
                    }
                    return true;
                case "bg3xscroll":
                    {
                        var hoff = macroInvokation.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
                        CurrentDefinition.BG3.HorizontalOffset = hoff;
                    }
                    return true;
                case "bg3yscroll":
                    {
                        var vofs = macroInvokation.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
                        CurrentDefinition.BG3.VerticalOffset = vofs;
                    }
                    return true;
                case "setbg3vofs":
                    {
                        var vofs = macroInvokation.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
                        CurrentDefinition.BG3.VerticalOffset = vofs*100;
                    }
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
                    SetLevelConstraints();
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
        private void SetChr(ASMMacroInvokeLineStructure BGChrLine)
        {
            var bgName = BGChrLine.TryGetParameter(0)?.ParameterContent;
            if(bgName == null) return;
            CurrentDefinition.BG2.BGChrFile = bgName;
        }
        private void SetScr(ASMMacroInvokeLineStructure BGScrLine)
        {
            var bgName = BGScrLine.TryGetParameter(0)?.ParameterContent;
            if (bgName == null) return;
            CurrentDefinition.BG2.BGScrFile = bgName;
        }
        private void SetChr3(ASMMacroInvokeLineStructure BGChrLine)
        {
            var bgName = BGChrLine.TryGetParameter(0)?.ParameterContent;
            if (bgName == null) return;
            CurrentDefinition.BG3.BGChrFile = bgName;
        }
        private void SetScr3(ASMMacroInvokeLineStructure BGScrLine)
        {
            var bgName = BGScrLine.TryGetParameter(0)?.ParameterContent;
            if (bgName == null) return;
            CurrentDefinition.BG3.BGScrFile = bgName;
        }
        internal void EndDefinition()
        {
            CurrentDefinition = null;
            BGINITIALIZED = false;
        }
    }
}