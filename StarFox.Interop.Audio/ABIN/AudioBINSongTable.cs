using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace StarFox.Interop.Audio.ABIN
{
    public interface ISongTableEntry
    {
        /// <summary>
        /// The address where this table entry is found in SPC Audio Memory
        /// </summary>
        public ushort SPCAddress { get; }
    }
    /// <summary>
    /// An <see cref="ISongTableEntry"/> that has an address in SPC memory
    /// </summary>
    public struct AudioBINSongTableEntry : ISongTableEntry
    {
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is ISongTableEntry e)
                return SPCAddress == e.SPCAddress;
            return base.Equals(obj);
        }
        public AudioBINSongTableEntry(ushort sPCAddress)
        {
            SPCAddress = sPCAddress;
        }
        public ushort SPCAddress { get; }
    }
    /// <summary>
    /// A <see cref="ISongTableEntry"/> that has a start address and an end address in SPC memory, representative of an array
    /// </summary>
    public struct AudioBINSongTableRangeEntry : ISongTableEntry
    {
        private ushort _end;
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is AudioBINSongTableRangeEntry e)
                return SPCAddress == e.SPCAddress && e.Length == Length;
            return base.Equals(obj);
        }
        /// <summary>
        /// Creates a new Range from a Start and End address
        /// </summary>
        /// <param name="sPCAddress"></param>
        /// <param name="sPCAddressEnd"></param>
        public AudioBINSongTableRangeEntry(ushort sPCAddress, ushort sPCAddressEnd) : this()
        {
            SPCAddress = sPCAddress;
            SPCAddressEnd = sPCAddressEnd;
        }
        /// <summary>
        /// Creates a new Range from a Start address and the length of the item
        /// </summary>
        /// <param name="sPCAddress"></param>
        /// <param name="Length"></param>
        public AudioBINSongTableRangeEntry(ushort sPCAddress, int Length) : this(sPCAddress, (ushort)(sPCAddress + Length)) { }
        public ushort SPCAddress { get; set; }
        /// <summary>
        /// The address where this song table entry ends
        /// </summary>
        public ushort SPCAddressEnd
        {
            get => _end;
            set
            {
                if (value < SPCAddress)
                    throw new InvalidDataException($"{nameof(SPCAddressEnd)} ({value}) is less than {nameof(SPCAddress)} ({SPCAddress})");
                _end = value;
            }
        }
        /// <summary>
        /// The length as calculated by <c>SPCAddressEnd - SPCAddress</c>
        /// </summary>
        public int Length => SPCAddressEnd - SPCAddress;
    }
    /// <summary>
    /// Contains a table of song addresses
    /// </summary>
    public class AudioBINTable : IList<ISongTableEntry>
    {
        /// <summary>
        /// Set of known Song Table types
        /// </summary>
        public enum SongTableEntryTypes
        {
            /// <summary>
            /// A set of SPC Addresses to Subs
            /// </summary>
            Normal = 0,
            /// <summary>
            /// A set of ranges making up sample data
            /// </summary>
            Ranges = 1,
        }
        /// <summary>
        /// Denotes whether this is a <see cref="AudioBINChunk.ChunkTypes.SampleTable"/> or <see cref="AudioBINChunk.ChunkTypes.SongTable"/>
        /// <para>Any other value is an error.</para>
        /// </summary>
        public AudioBINChunk.ChunkTypes TableType
        {
            get => _type;
            set
            {
                if (value is AudioBINChunk.ChunkTypes.SampleTable or AudioBINChunk.ChunkTypes.SongTable)
                    _type = value;
                else throw new InvalidDataException($"Invalid type: {value}");
            }
        }
        /// <summary>
        /// The address in SPC Audio Memory to write this Song Table
        /// </summary>
        public ushort SPCAddress { get; set; }
        public string SPCAddressHexString => SPCAddress.ToString("X4");
        /// <summary>
        /// The length of this song table
        /// </summary>
        public int Length => Count * sizeof(ushort);

        public int Count => ((ICollection<ISongTableEntry>)baseList).Count;

        public bool IsReadOnly => ((ICollection<ISongTableEntry>)baseList).IsReadOnly;

        public ISongTableEntry this[int index] { get => ((IList<ISongTableEntry>)baseList)[index]; set => ((IList<ISongTableEntry>)baseList)[index] = value; }

        private List<ISongTableEntry> baseList = new List<ISongTableEntry>();
        private AudioBINChunk.ChunkTypes _type;

        public int IndexOf(ISongTableEntry item)
        {
            return ((IList<ISongTableEntry>)baseList).IndexOf(item);
        }

        public void Insert(int index, ISongTableEntry item)
        {
            ((IList<ISongTableEntry>)baseList).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<ISongTableEntry>)baseList).RemoveAt(index);
        }

        public void Add(ISongTableEntry item) => ((ICollection<ISongTableEntry>)baseList).Add(item);
        /// <summary>
        /// Macro function to add a new <see cref="AudioBINSongTableEntry"/> to this collection
        /// </summary>
        /// <param name="SPCAddress"></param>
        public void Add(ushort SPCAddress) => Add(new AudioBINSongTableEntry(SPCAddress));
        /// <summary>
        /// Macro function to add a new <see cref="AudioBINSongTableRangeEntry"/> to this collection
        /// </summary>
        public void Add(ushort SPCAddress, ushort SPCEndAddress) => Add(new AudioBINSongTableRangeEntry(SPCAddress, SPCEndAddress));
        /// <summary>
        /// Macro function to add a new <see cref="AudioBINSongTableRangeEntry"/> to this collection
        /// </summary>
        public void Add(ushort SPCAddress, int Length) => Add(new AudioBINSongTableRangeEntry(SPCAddress, Length));
        public void Clear()
        {
            ((ICollection<ISongTableEntry>)baseList).Clear();
        }

        public bool Contains(ISongTableEntry item)
        {
            return ((ICollection<ISongTableEntry>)baseList).Contains(item);
        }

        public void CopyTo(ISongTableEntry[] array, int arrayIndex)
        {
            ((ICollection<ISongTableEntry>)baseList).CopyTo(array, arrayIndex);
        }

        public bool Remove(ISongTableEntry item)
        {
            return ((ICollection<ISongTableEntry>)baseList).Remove(item);
        }

        public IEnumerator<ISongTableEntry> GetEnumerator()
        {
            return ((IEnumerable<ISongTableEntry>)baseList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)baseList).GetEnumerator();
        }
    }
}
