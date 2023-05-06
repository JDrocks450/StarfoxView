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
            "BGMACS.INC",
            "VARS.INC"
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
        public async Task<MAPContextFile> ProcessLevelContexts(string BGSASM, ASMFile BGMACS)
        {
            var importer = new MAPContextImporter();
            importer.SetImports(BGMACS);
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
            return ProcessLevelContexts(BGSASM, bgmacs);
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
            var title = Path.GetFileNameWithoutExtension(FilePath);
            file.LevelData.Title = title;
            int runningDelay = 0;

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
                file.AttachContext(context, runningDelay);
                return true;
            }

            foreach (var line in file.Chunks.OfType<ASMLine>()) // get all lines
            { // go through chunks looking for Map objects
                if (!line.HasStructureApplied) continue;
                if (line.Structure is not ASMMacroInvokeLineStructure) continue; // we can't do much with these right now
                // ** begin macro invoke line                
                if (MAPEvent.TryParse<MAPObjectEvent>(line, out var mapobj))
                    file.LevelData.Events.Add(mapobj);
                else if (MAPEvent.TryParse<MAPJSREvent>(line, out var mapjsr))                
                    file.LevelData.Events.Add(mapjsr);                
                else if (MAPEvent.TryParse<MAPPathObjectEvent>(line, out var mappath))
                    file.LevelData.Events.Add(mappath);
                else if (MAPEvent.TryParse<MAPAlVarEvent>(line, out var alvar))
                    file.LevelData.Events.Add(alvar);
                else if (MAPEvent.TryParse<MAPWaitEvent>(line, out var wait))
                    file.LevelData.Events.Add(wait);
                else if (MAPEvent.TryParse<MAPInitLevelEvent>(line, out var init))
                {
                    file.LevelData.Events.Add(init);
                    AppendBackground(init.Background);
                }
                else if (MAPEvent.TryParse<MAPSetBG>(line, out var setBG))
                {
                    file.LevelData.Events.Add(setBG);
                    AppendBackground(setBG.Background);
                }
                else
                    file.LevelData.Events.Add(new MAPUnknownEvent(line)); // default add unknown map event                
                file.LevelData.EventsByDelay.Add(file.LevelData.Events.Count - 1, runningDelay);                
                var latestNode = file.LevelData.Events.Last();
                if (latestNode is IMAPDelayEvent delay)
                    runningDelay += delay.Delay;
            }
            return ImportedObject;
        }        
    }
}
