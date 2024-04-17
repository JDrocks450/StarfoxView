using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MAP.EVT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP
{
    public class MAPImporter : BasicCodeImporter<MAPFile>
    {
        public override string[] ExpectedIncludes => new string[]
        {
            "MAPMACS.INC", // MAPMACS.INC is expected
            "BGS.ASM", // BGS.ASM contains data what what levels should appear like
            "BGMACS.INC", // Contains macros related to rendering backgrounds
            "VARS.INC", // contains various variables
            "STRATEQU.INC" // contains constraints like the level XMax, YMax, etc.
        };
        private MAPContextFile? mapContextDefinitions;
        /// <summary>
        /// Using <see cref="ProcessLevelContexts"/> functions will populate this value.
        /// <para>You should use this property to explore the loaded contexts, but not edit them.</para>
        /// </summary>
        public MAPContextFile? LoadedContextDefinitions => mapContextDefinitions;
        /// <summary>
        /// Dictates whether or not this <see cref="MAPImporter"/> has contexts set.
        /// <para>See: <see cref="ProcessLevelContexts(string, ASMFile)"/></para>
        /// </summary>
        public bool MapContextsSet => mapContextDefinitions != null;
        public MAPImporter()
        {

        }
        public MAPImporter(string FilePath) : this()
        {
            _ = ImportAsync(FilePath).Result;
        }
        /// <summary>
        /// Optionally, this importer can attach <see cref="CONTEXT.MAPContextDefinition"/> info
        /// to this MAP import by getting data from <c>BGS.ASM</c> data.
        /// </summary>
        /// <param name="BGSASM">The full path to the <c>BGS.ASM</c> file.</param>
        /// <param name="BGMACS">The BGMACS file imported for finding symbols.</param>
        /// <returns></returns>
        public async Task<MAPContextFile> ProcessLevelContexts(string BGSASM, ASMFile BGMACS, ASMFile STRATEQU)
        {
            var importer = new MAPContextImporter();
            importer.SetImports(BGMACS,STRATEQU);
            var message = importer.CheckWarningMessage(BGSASM);
            if (message != default) throw new Exception(message);
            var bgsASM = mapContextDefinitions = await importer.ImportAsync(BGSASM);
            return bgsASM;
        }
        /// <summary>
        /// Optionally, this importer can attach <see cref="CONTEXT.MAPContextDefinition"/> info
        /// to this MAP import by getting data from <c>BGS.ASM</c> data.
        /// <para>This will use <see cref="SetImports(ASMFile[])"/> to find the <c>BGMACS.INC</c> file. 
        /// If it is not imported, this method will throw an exception.</para>
        /// </summary>
        /// <param name="BGSASM">The full path to the <c>BGS.ASM</c> file.</param>
        public Task<MAPContextFile> ProcessLevelContexts(string BGSASM)
        {
            var bgmacs = baseImporter.Context.Includes.FirstOrDefault(x =>
                Path.GetFileName(x.OriginalFilePath).ToUpper() == "BGMACS.INC");
            if (bgmacs == default) throw new FileNotFoundException("BGMACS.INC is not imported.");
            var stratequ = baseImporter.Context.Includes.FirstOrDefault(x =>
                Path.GetFileName(x.OriginalFilePath).ToUpper() == "STRATEQU.INC");
            if (stratequ == default) throw new FileNotFoundException("STRATEQU.INC is not imported.");
            return ProcessLevelContexts(BGSASM, bgmacs, stratequ);
        }
        /// <summary>
        /// Optionally, this importer can attach <see cref="CONTEXT.MAPContextDefinition"/> info
        /// to this MAP import by getting data from <c>BGS.ASM</c> data.
        /// <para>This will use <see cref="SetImports(ASMFile[])"/> to find the <c>BGMACS.INC</c> file
        /// and <c>BGS.ASM</c>. If they are not imported, this method will throw an exception.</para>       
        /// </summary>
        public Task<MAPContextFile> ProcessLevelContexts()
        {
            var bgsasm = baseImporter.Context.Includes.FirstOrDefault(x =>
                Path.GetFileName(x.OriginalFilePath).ToUpper() == "BGS.ASM");
            if (bgsasm == default) throw new FileNotFoundException("BGS.ASM is not imported.");
            return ProcessLevelContexts(bgsasm.OriginalFilePath);
        }
        /// <summary>
        /// Optionally, this importer can attach <see cref="CONTEXT.MAPContextDefinition"/> info
        /// to this MAP import by getting data from <c>BGS.ASM</c> data.
        /// </summary>
        public void ProcessLevelContexts(MAPContextFile MapContextFile) =>
            mapContextDefinitions = MapContextFile;       
        /// <summary>
        /// Finds the specified <see cref="MAPContextDefinition"/> by name (as it appears in code, not 
        /// <see cref="MAPSetBG.TranslateNameToMAPContext(in string, string)"/>)
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public MAPContextDefinition? FindContext(string Name)
        {
            var bgname = MAPSetBG.TranslateNameToMAPContext(Name);
            return mapContextDefinitions?.Definitions?.FirstOrDefault(
                x => x.Key.ToLower() == bgname.ToLower()).Value;
        }
        /// <summary>
        /// Attempts to import the given file and interpret the code as a MAP file
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public override async Task<MAPFile> ImportAsync(string FilePath)
        {
            ErrorOut.Clear();
            var baseImport = await baseImporter.ImportAsync(FilePath);
            if (baseImport == default) throw new InvalidOperationException("That file could not be parsed.");
            var file = ImportedObject = new MAPFile(baseImport); // from ASM file                                                
            
            int runningDelay = 0;
            MAPScript? currentScript = null;

            void SetUpNewMapScript()
            {
                if (currentScript != null)
                    file.Scripts.Add(currentScript.Header.LevelMacroName, currentScript);
                currentScript = new MAPScript((file.Scripts.Count + 1).ToString());
                runningDelay = 0;
            }

            //Appends a new background to this file
            bool AppendBackground(string BackgroundName)
            {
                var context = FindContext(BackgroundName);
                if (context == default)
                {
                    ErrorOut.AppendLine($"Can't add a context: {BackgroundName} because it wasn't found.\n" +
                        $"Attempted to translate it to be: {MAPSetBG.TranslateNameToMAPContext(BackgroundName)}, no luck.");
                    return false;                 
                }
                currentScript.AttachContext(context, runningDelay);
                return true;
            }

            //Use this in place of traditional Foreach. It allows you to jump to any expression by it's index in the file.
            uint ASMChunkIndex = 0;
            var chunksArray = file.Chunks.OfType<ASMLine>();
            //For MapLoop
            MAPLoopEvent? CurrentLoop = default;
            int loopedAmount = 0;

            //Set up the first script
            SetUpNewMapScript();

            for(ASMChunkIndex = 0; ASMChunkIndex < chunksArray.Count(); ASMChunkIndex++)
            { // go through chunks looking for Map objects
                ASMLine? line = chunksArray.ElementAt((int)ASMChunkIndex);
                //Find occurances of sections to map loops
                if (line.HasInlineLabel && !string.IsNullOrWhiteSpace(line.InlineLabel))
                {
                    currentScript.LevelData.SectionMarkers.TryAdd(line.InlineLabel.ToLowerInvariant(),
                        new MAPData.MAPRegionContext(line.InlineLabel, ASMChunkIndex, runningDelay));
                    if (currentScript.LevelData.SectionMarkers.Count == 1)
                        currentScript.Header.LevelMacroName = line.InlineLabel;
                    continue;
                }
                if (!line.HasStructureApplied) continue;
                if (line.Structure is not ASMMacroInvokeLineStructure) continue; // we can't do much with these right now
                // ** begin macro invoke line                
                if (MAPEvent.TryParse<MAPObjectEvent>(line, out var mapobj))
                    currentScript.LevelData.Events.Add(mapobj); // Spawn Event Object call
                else if (MAPEvent.TryParse<MAPJSREvent>(line, out var mapjsr))
                    currentScript.LevelData.Events.Add(mapjsr); // Map Jump Subroutine call
                else if (MAPEvent.TryParse<MAPPathObjectEvent>(line, out var mappath))
                    currentScript.LevelData.Events.Add(mappath); // Map Path'd Object call
                else if (MAPEvent.TryParse<MAPAlVarEvent>(line, out var alvar))
                    currentScript.LevelData.Events.Add(alvar); // Map AL VAR set call
                else if (MAPEvent.TryParse<MAPWaitEvent>(line, out var wait))
                    currentScript.LevelData.Events.Add(wait); // Map Wait Call
                else if (MAPEvent.TryParse<MAPInitLevelEvent>(line, out var init))
                { // Initialize Map Call
                    currentScript.Header.LevelName = ((IMAPNamedEvent)init).Name;
                    currentScript.LevelData.Events.Add(init);
                    AppendBackground(init.Background);
                }
                else if (MAPEvent.TryParse<MAPSetBG>(line, out var setBG))
                { // Map Set Background Call
                    currentScript.LevelData.Events.Add(setBG);
                    AppendBackground(setBG.Background);
                }
                else if (MAPEvent.TryParse<MAPLoopEvent>(line, out var maploop))
                { // Map Loop
                    //Find the section pointer in the file
                    if (!currentScript.LevelData.SectionMarkers.TryGetValue(maploop.LoopMacroName.ToLowerInvariant(), out var loopContext))
                        throw new InvalidOperationException($"MapLoop requested {maploop.LoopMacroName} which wasn't found in the file.");                        
                    //are we already looping
                    if (CurrentLoop == null || 
                        //In the below situation, we somehow traversed from one maploop to a completely different one.
                        //how? no idea. should never happen
                        CurrentLoop.LoopMacroName != maploop.LoopMacroName)
                    { // set new loop with zero loops
                        CurrentLoop = maploop;
                        loopedAmount = -1;
                    }
                    loopContext.ReferencedLoops.Add(CurrentLoop); // add this referenced loop
                    loopedAmount++;
                    int loopsLeft = CurrentLoop.LoopAmount - loopedAmount; // loops left
                    if (loopsLeft <= 0) // 0 or lower
                    { // we have no loops left
                        currentScript.LevelData.Events.Add(CurrentLoop);
                        loopContext.EstimatedTimeEnd = runningDelay;
                        CurrentLoop = null;
                    }
                    else
                    {
                        //goto the line that has the label
                        ASMChunkIndex = loopContext.ASMChunkIndex;                        
                        continue;
                    }
                }
                else if (MAPEvent.TryParse<MAPEndEvent>(line, out var mapend))
                { // Map End Event
                    SetUpNewMapScript();
                    continue;
                }
                else currentScript.LevelData.Events.Add(new MAPUnknownEvent(line)); // default add unknown map event

                currentScript.LevelData.EventsByDelay.Add(currentScript.LevelData.Events.Count - 1, runningDelay);                
                var latestNode = currentScript.LevelData.Events.Last();
                if (latestNode is IMAPDelayEvent delay)
                    runningDelay += delay.Delay;
                latestNode.LevelDelay = runningDelay;                
            }
            if (currentScript.LevelData.Events.Count > 1)
                file.Scripts.TryAdd(currentScript.Header.LevelMacroName, currentScript);
            
            return ImportedObject;
        }        
    }
}
