namespace StarFox.Interop.BSP.SHAPE
{
    /// <summary>
    /// Represents an entry in the BSP Collection of a BSP-Activated geometry.
    /// </summary>
    public class BSPEntry
    {
        public BSPEntry(int iD, string facesPtr, string nextPtr)
        {
            ID = iD;
            FacesPtr = facesPtr;
            NextPtr = nextPtr;
        }

        public int ID { get; }
        public string FacesPtr { get; }
        public string NextPtr { get; }
    }
}