using StarFox.Interop.ASM;
using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MAP.EVT;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP
{
    /// <summary>
    /// Represents the data used to create a level in Starfox.
    /// </summary
    public class MAPData
    {
        /// <summary>
        /// The title of the MAP file
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The events that make up this level script
        /// <para>This is also sometimes referred to as a ZDepth Table</para>
        /// </summary>
        public HashSet<MAPEvent> Events { get; set; } = new();
        /// <summary>
        /// Get only events that have attached shape data
        /// </summary>
        public IEnumerable<IMAPShapeEvent> ShapeEvents => Events.OfType<IMAPShapeEvent>();
        /// <summary>
        /// All of the events of this MAPScript, in order, with accompanying DELAY calculated based on the previous events.
        /// <para>KEY is the index of the event in the <see cref="Events"/> property.</para>
        /// </summary>
        public Dictionary<int, int> EventsByDelay { get; set; } = new();

        /// <summary>
        /// Merges all the events into one *new* MAPData instance, keeps context data from Parent map
        /// </summary>
        /// <param name="ParentMap"></param>
        /// <param name="MergeChild"></param>
        /// <returns></returns>
        public static MAPData Combine(MAPData ParentMap, MAPData MergeChild)
        {
            MAPData newMap = new MAPData()
            {
                Title = ParentMap.Title,
            };
            if (ParentMap.Events.Count != ParentMap.EventsByDelay.Count)
                throw new InvalidDataException("PARENT Events and EventsByDelay are not the same!!!");
            if (MergeChild.Events.Count != MergeChild.EventsByDelay.Count)
                throw new InvalidDataException("CHILD Events and EventsByDelay are not the same!!!");
            int runningIndex = 0;
            for (int i = 0; i < ParentMap.Events.Count; i++)
            {
                var evt = ParentMap.Events.ElementAt(i);
                newMap.Events.Add(evt);
                var dlyevt = ParentMap.EventsByDelay.ElementAt(i);
                newMap.EventsByDelay.Add(runningIndex + dlyevt.Key, dlyevt.Value);
            }
            runningIndex = newMap.Events.Count;
            for (int i = 0; i < MergeChild.Events.Count; i++)
            {
                var evt = MergeChild.Events.ElementAt(i);
                newMap.Events.Add(evt);
                var dlyevt = MergeChild.EventsByDelay.ElementAt(i);
                newMap.EventsByDelay.Add(runningIndex + dlyevt.Key, dlyevt.Value);
            }
            return newMap;
        }

        [Serializable]
        private class Intermediary
        {
            public string Title { get; set; }
            public Dictionary<int, int> EventsByDelay { get; set; } = new();
            public byte[] SerializedData { get; set; }
        }

        /// <summary>
        /// Serializes this object to the given stream
        /// </summary>
        /// <param name="Destination"></param>
        public void Serialize(Utf8JsonWriter Destination)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                StrongTypeSerialization.SerializeObjects(mem, Events);
                Intermediary inter = new()
                {
                    Title = Title,
                    EventsByDelay = EventsByDelay,
                    SerializedData = mem.ToArray()
                };
                using (var doc = JsonSerializer.SerializeToDocument(inter, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                }))
                    doc.WriteTo(Destination);
            }            
        }
        public static async Task<MAPData> Deserialize(Stream Json)
        {
            Intermediary? inter = await JsonSerializer.DeserializeAsync<Intermediary>(Json);
            if (inter == null) throw new Exception("Couldn't create the intermediary!");
            MAPData data = new()
            {
                Title = inter.Title,
                EventsByDelay = inter.EventsByDelay,
            };
            using (MemoryStream stream = new MemoryStream(inter.SerializedData))
            {
                var evts = await StrongTypeSerialization.DeserializeObjects(stream);
                data.Events = new HashSet<MAPEvent>(evts.Cast<MAPEvent>());
            }
            return data;
        }
    }
    /// <summary>
    /// Represents a Map Script File
    /// </summary>
    public class MAPFile : ASMFile
    {
        /// <summary>
        /// The main level data for this level. 
        /// <para>Note: Levels can often reference sub-sections of the level in other files / compilation units.</para>
        /// <para>For this scenario, you should use the <see cref="MergeSubsection(MAPData)"/> to merge the subsections</para>
        /// </summary>
        public MAPData LevelData { get; internal set; } = new();
        public void MergeSubsection(MAPData Subsection) => LevelData = MAPData.Combine(LevelData, Subsection);
        /// <summary>
        /// A map of all referenced level sub-section names and the <see cref="MAPJSREvent"/>s found in <see cref="LevelData"/>
        /// that spawn the sub-section.
        /// <para>This property is calculated when referenced in the code at runtime -- use with caution for performance.</para>
        /// </summary>
        public Dictionary<MAPJSREvent, string> ReferencedSubSections
        {
            get
            {
                var dic = new Dictionary<MAPJSREvent, string>();
                foreach(var evt in LevelData.Events.OfType<MAPJSREvent>())                
                    dic.Add(evt, evt.SubroutineName);                
                return dic;
            }
        }
        /// <summary>
        /// Gets the <see cref="MAPContextDefinition"/> attached with this level.
        /// <para>Note: Many levels will not have this set as they are in-fact parts of larger levels.</para>
        /// <para>This value is dictated by the level having an <see cref="MAPInitLevelEvent"/>. This value is always the first one.</para>
        /// <para>Does this level have multiple <see cref="MAPInitLevelEvent"/>s? Use the <see cref="ReferencedContexts"/> property to find all contexts referenced.</para>
        /// </summary>
        public MAPContextDefinition? LevelContext => ReferencedContexts?.FirstOrDefault().Key ?? default;
        /// <summary>
        /// A map of all referenced contexts and at what DELAY they appear in-game
        /// </summary>
        private Dictionary<MAPContextDefinition, int> referencedContexts { get; } = new();
        /// <summary>
        /// A collection of all referenced <see cref="MAPContextDefinition"/>s and at what Delay they appear.
        /// <para>To edit this collection, use the <see cref="AttachContext(MAPContextDefinition, int)"/> function</para>
        /// </summary>
        public IEnumerable<KeyValuePair<MAPContextDefinition, int>> ReferencedContexts => referencedContexts;
        /// <summary>
        /// Creates a new MAPFile representing the referenced file
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        internal MAPFile(string OriginalFilePath) : base(OriginalFilePath) {
            
        }
        internal MAPFile(ASMFile From) : base(From)
        { 
            
        }
        /// <summary>
        /// Attaches the specified Context to this <see cref="MAPFile"/>.
        /// <para>Use the <see cref="ReferencedContexts"/> property to find all contexts referenced</para>
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Delay"></param>
        internal void AttachContext(MAPContextDefinition Context, int Delay)
        {
            referencedContexts.Add(Context, Delay);
        }
    }
}
