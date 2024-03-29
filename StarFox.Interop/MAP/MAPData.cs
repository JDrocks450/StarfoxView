using StarFox.Interop.MAP.EVT;
using StarFox.Interop.MISC;
using System.Text.Json;

namespace StarFox.Interop.MAP
{
    /// <summary>
    /// Represents the data used to create a level in Starfox.
    /// </summary>
    public class MAPData
    {
        /// <param name="LabelName"> The name of the inline label for this region </param>
        /// <param name="ASMChunkIndex"> The index of chunk the Inline Label appears at </param>
        /// <param name="EstimatedTimeStart"> The estimated LevelTime (Delay) this might appear at. This is useful
        /// for a visual editor showing where this loop might be.</param>
        [Serializable] public record MAPRegionContext(string LabelName, uint ASMChunkIndex, int EstimatedTimeStart)
        {
            /// <summary>
            /// The estimated LevelTime (Delay) this might end at. This is useful
            /// for a visual editor showing where this loop might be.</param>
            /// </summary>
            public int EstimatedTimeEnd { get; set; }
            /// <summary>
            /// If there are any maploops that use this label
            /// </summary>
            public bool IsLooped => ReferencedLoops.Any();
            /// <summary>
            /// Any <see cref="MAPLoopEvent"/> that references this section/region
            /// <para/> For all intents and purposes this should only ever have one item or none at all. 
            /// Not sure if multiple <c>maploop</c>s using the same spot is even feasible
            /// </summary>
            public HashSet<MAPLoopEvent> ReferencedLoops { get; set; } = new();
        }

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
        /// Maps have labels that can be used in loops to create repeated sections of levels without
        /// copy/pasting, for example.
        /// <para/>This details where those occur in the base <see cref="ASMFile"/> this map was imported from
        /// </summary>
        public Dictionary<string, MAPRegionContext> SectionMarkers { get; } = new();

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
}
