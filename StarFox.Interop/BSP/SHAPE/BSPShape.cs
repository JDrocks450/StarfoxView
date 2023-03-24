using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP.SHAPE
{
    /// <summary>
    /// A point in 3D space found in an ASM BSP file.
    /// <para><code>pb X,Y,Z</code></para>
    /// </summary>
    public struct BSPPoint
    {
        /// <summary>
        /// Starfox uses BOTH byte and word-based points. When using word, the index is always incremented by two.
        /// <para>If you're just looking for the index of this point regardless of it's size, use <see cref="ActualIndex"/></para>
        /// </summary>
        public int Index;
        public int ActualIndex;
        public int X, Y, Z;

        public BSPPoint() : this(0,0,0,0,0)
        {

        }

        public BSPPoint(int index, int actual, int x, int y, int z)
        {
            Index = index;
            ActualIndex = actual;
            X = x;
            Y = y;
            Z = z;
        }
        public override string ToString()
        {
            return $"BSPPoint - X: {X}, Y: {Y}, Z: {Z}";
        }
    }
    /// <summary>
    /// A rudementary Vector 3 class that only deals in integers (SF Source Code Compatibility)
    /// </summary>
    public struct BSPVec3
    {
        public int X, Y, Z;
        public override string ToString()
        {
            return $"BSPVec3 - X: {X}, Y: {Y}, Z: {Z}";
        }
    }
    /// <summary>
    /// Represents a reference to a <see cref="BSPPoint"/> in the same shape
    /// </summary>
    public struct BSPPointRef
    {
        /// <summary>
        /// The position this reference falls in the Face macro invokation callsite.
        /// <para>Effectively, which vert this one is, in the order they were called in the source code.</para>
        /// </summary>
        public int Position;
        /// <summary>
        /// The index of the referenced point. Must be defined in the same shape.
        /// </summary>
        public int PointIndex;

        public override string ToString()
        {
            return $"BSPPointReference - LinePosition: {Position}, Point: {PointIndex}";
        }
    }
    /// <summary>
    /// A face on a 3D shape. These can be any amount of verts, dependent on the implementation in the source code.
    /// </summary>
    public struct BSPFace
    {
        public int Color;
        public int Index;
        public BSPVec3 Normal;
        /// <summary>
        /// The indices of the <see cref="BSPPoint"/> used in this face, in order.
        /// </summary>
        public BSPPointRef[] PointIndices;

        public BSPFace(int color, int index, BSPVec3 normal, BSPPointRef[] pointIndices)
        {
            Color = color;
            Index = index;
            Normal = normal;
            PointIndices = pointIndices;
        }

        public override string ToString()
        {
            return $"BSPFace - Color: {Color}, Index: {Index}, Normal: {Normal}, Indices: {string.Join(",", PointIndices)}";
        }
    }
    public class BSPFrame
    {
        public string Name { get; set; }
        public HashSet<BSPPoint> Points { get; set; }
    }
    /// <summary>
    /// Represents a 3D Shape in Starfox
    /// </summary>
    public class BSPShape
    {
        /// <summary>
        /// The header data attached to this Shape
        /// </summary>
        public BSPShapeHeader Header { get; internal set; }
        /// <summary>
        /// These are points that are available no matter what frame of animation you're viewing.
        /// </summary>
        public HashSet<BSPPoint> Points { get; } = new();
        /// <summary>
        /// The set of points associated with this shape
        /// <para>Points are handled based on FRAME, this collection should be accessed using: <see cref=""/></para>
        /// </summary>
        public Dictionary<int, BSPFrame> Frames { get; } = new();
        /// <summary>
        /// A set of faces that reference points with this shape.
        /// </summary>
        public HashSet<BSPFace> Faces { get; } = new();

        internal BSPShape() { 
            
        }

        /// <summary>
        /// Creates a new Shape with the given header
        /// </summary>
        /// <param name="Header"></param>
        public BSPShape(BSPShapeHeader Header) : this() {
            this.Header = Header;
        }
        /// <summary>
        /// Get points for a given frame
        /// </summary>
        /// <param name="Frame">The index of the frame</param>
        /// <returns></returns>
        public BSPFrame GetFrame(int Frame = 0) => Frames[Frame];
        public IEnumerable<BSPPoint> GetPoints(int Frame = -1)
        {
            var returnList = new List<BSPPoint>(Points);
            if (Frame == -1 && Frames.Count > 0) Frame++;
            if (Frame >= 0)
                returnList.AddRange(GetFrame(Frame).Points);
            return returnList;
        }
        /// <summary>
        /// Gets the point at the specified index, optionally you can supply a frame number
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public BSPPoint GetPoint(int Index, int Frame = 0)
        {
            if (Index < 0) throw new Exception("That point wasn't found.");
            if (Points.Any(x => x.Index == Index))
                return Points.First(x => x.Index == Index);
            if (Frame < Frames.Count && Frame > -1)
            {
                if (Frames[Frame].Points.Any(x => x.Index == Index))
                    return Frames[Frame].Points.First(x => x.Index == Index);
                else return GetPoint(Index-1, Frame);
            }
            else if (Index > 0) return GetPoint(Index - 1, Frame);
            else throw new Exception("That point wasn't found.");
        }
        /// <summary>
        /// Pushes a new frame to this shape object
        /// </summary>
        /// <param name="Frame">The index of the new frame to add</param>
        /// <param name="Points">The points on the given frame</param>
        /// <returns></returns>
        public bool PushFrame(int Frame, BSPFrame FrameData)
        {
            if (Frames.ContainsKey(Frame)) return false;
            Frames.Add(Frame, FrameData);
            return true;
        }

        public override string ToString()
        {
            return $"SHAPE - {Header?.Name ?? ""} Faces: {Faces.Count}, Frames: {Frames.Count}";
        }
    }
}
