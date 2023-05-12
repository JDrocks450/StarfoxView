using System.Collections;

namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Contains a table of song addresses
    /// </summary>
    public class AudioBINSongTable : IList<ushort>
    {
        public ushort this[int index] { get => ((IList<ushort>)Addresses)[index]; set => ((IList<ushort>)Addresses)[index] = value; }
        private List<ushort> Addresses { get; } = new List<ushort>();
        public int Count => ((ICollection<ushort>)Addresses).Count;
        public bool IsReadOnly => ((ICollection<ushort>)Addresses).IsReadOnly;
        public void Add(ushort item)
        {
            ((ICollection<ushort>)Addresses).Add(item);
        }

        public void Clear()
        {
            ((ICollection<ushort>)Addresses).Clear();
        }

        public bool Contains(ushort item)
        {
            return ((ICollection<ushort>)Addresses).Contains(item);
        }

        public void CopyTo(ushort[] array, int arrayIndex)
        {
            ((ICollection<ushort>)Addresses).CopyTo(array, arrayIndex);
        }

        public IEnumerator<ushort> GetEnumerator()
        {
            return ((IEnumerable<ushort>)Addresses).GetEnumerator();
        }

        public int IndexOf(ushort item)
        {
            return ((IList<ushort>)Addresses).IndexOf(item);
        }

        public void Insert(int index, ushort item)
        {
            ((IList<ushort>)Addresses).Insert(index, item);
        }

        public bool Remove(ushort item)
        {
            return ((ICollection<ushort>)Addresses).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<ushort>)Addresses).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Addresses).GetEnumerator();
        }
    }
}
