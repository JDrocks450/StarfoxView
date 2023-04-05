using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP.SHAPE
{
    /// <summary>
    /// A point in 3D space found in an ASM BSP file.
    /// <para><code>pb X,Y,Z</code></para>
    /// </summary>
    public class BSPPoint
    {
        public int Index { get; set; }
        public int Width { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public BSPPoint() : this(0,1,0,0,0)
        {

        }

        public BSPPoint(int index, int width, int x, int y, int z)
        {
            Index = index;
            Width = width;
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
        /// <summary>
        /// X component
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// Y component
        /// </summary>
        public int Y { get; set; }
        /// <summary>
        /// Z Component
        /// </summary>
        public int Z { get; set; }
        public override string ToString()
        {
            return $"BSPVec3 - X: {X}, Y: {Y}, Z: {Z}";
        }
    }
    /// <summary>
    /// Represents a reference to a <see cref="BSPPoint"/> in the same shape
    /// </summary>
    [Serializable]
    public class BSPPointRef
    {
        /// <summary>
        /// The position this reference falls in the Face macro invokation callsite.
        /// <para>Effectively, which vert this one is, in the order they were called in the source code.</para>
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// The index of the referenced point. Must be defined in the same shape.
        /// </summary>
        public int PointIndex { get; set; }

        public override string ToString()
        {
            return $"BSPPointReference - LinePosition: {Position}, Point: {PointIndex}";
        }
    }
    /// <summary>
    /// A face on a 3D shape. These can be any amount of verts, dependent on the implementation in the source code.
    /// </summary>
    [Serializable]
    public class BSPFace
    {
        public int Color { get; set; }
        public int Index { get; set; }
        public BSPVec3 Normal { get; set; }
        /// <summary>
        /// The indices of the <see cref="BSPPoint"/> used in this face, in order.
        /// </summary>
        public BSPPointRef[] PointIndices { get; set; }

        public BSPFace()
        {

        }
        public BSPFace(int color, int index, BSPVec3 normal, BSPPointRef[] pointIndices) : this()
        {
            Color = color;
            Index = index;
            Normal = normal;
            PointIndices = pointIndices;
        }

        public override string ToString()
        {
            return $"BSPFace - Color: {Color}, Index: {Index}, Normal: {Normal}, Indices: {string.Join<BSPPointRef>(",", PointIndices)}";
        }
    }
    
    /// <summary>
    /// Represents a frame of animation in a Starfox Shape.
    /// </summary>
    [Serializable]
    public class BSPFrame
    {
        /// <summary>
        /// The name of this frame, typically this is parsed as the JumpTab address marker
        /// <para>e.g. A0A, A9A</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The set of points added to this frame. 
        /// <para>Pay close attention to the Index field of the BSPPoint to find where it belongs.</para>
        /// </summary>
        public BSPPoint[] Points { get; set; } = { };
        /// <summary>
        /// Adds a point to this object and rectifies the Index property to be the next in sequence
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="index"></param>
        public void AddPointSequential(BSPPoint Point, out int index)
        {
            index = 0;
            if (Points.Any())
            {
                var lastPoint = Points.Last();
                index = lastPoint.Index + 1;
            }
            Point.Index = index;
            AddPoint(Point);
            index += Point.Width;
        }
        /// <summary>
        /// Adds a point to this frame without changing the Index parameter (be careful)
        /// </summary>
        /// <param name="Point"></param>
        public void AddPoint(BSPPoint Point)
        {
            var fooPoints = Points;
            Array.Resize(ref fooPoints, Points.Length + 1);
            Points = fooPoints;
            Points[Points.Length - 1] = Point;
        }
    }
    
    /// <summary>
    /// Represents a 3D Shape in Starfox
    /// </summary>
    [Serializable] 
    public class BSPShape
    {
        /// <summary>
        /// The header data attached to this Shape
        /// </summary>
        public BSPShapeHeader Header { get; set; }
        public BSPFrame GlobalFrame { get; set; } = new()
        {
            Name = "default_0"
        };
        /// <summary>
        /// These are points that are available no matter what frame of animation you're viewing.
        /// </summary>
        public BSPPoint[] Points => GlobalFrame.Points;
        public void AddPoint(BSPPoint Point) => GlobalFrame.AddPoint(Point);
        /// <summary>
        /// True if this model has BSP Entries added to it's <see cref="BSPEntries"/> collection.
        /// </summary>
        public bool HasBSPProperties => BSPEntries.Count > 0;
        /// <summary>
        /// In BSP-Enabled geometry, this is the BSP table.
        /// </summary>
        public Dictionary<int, BSPEntry> BSPEntries { get; set; } = new();
        /// <summary>
        /// The set of points associated with this shape
        /// <para>Points are handled based on FRAME, this collection should be accessed using: <see cref=""/></para>
        /// </summary>
        public Dictionary<string, BSPFrame> FrameData { get; set; } = new();
        /// <summary>
        /// The keyframes associated with this model, in chronological order
        /// </summary>
        public Dictionary<int, string> Frames { get; set; } = new();
        /// <summary>
        /// A set of faces that reference points with this shape.
        /// </summary>
        public HashSet<BSPFace> Faces { get; set; } = new();       

        /// <summary>
        /// Creates a blank shape
        /// </summary>
        public BSPShape() { 
            
        }

        /// <summary>
        /// Creates a new Shape with the given header
        /// </summary>
        /// <param name="Header"></param>
        public BSPShape(BSPShapeHeader Header) : this() {
            this.Header = Header;
        }
        /// <summary>
        /// Get points for a given keyframe
        /// </summary>
        /// <param name="Frame">The index of the frame</param>
        /// <returns></returns>
        public BSPFrame GetFrame(int Frame = 0) => FrameData[Frames[Frame]];
        public IEnumerable<BSPPoint> GetPoints(int Frame = -1)
        {
            var returnList = new List<BSPPoint>(Points);
            if (Frame == -1 && Frames.Count > 0) Frame++;
            if (Frame >= 0)
                returnList.AddRange(GetFrame(Frame).Points);
            return returnList;
        }
        /// <summary>
        /// Gets the faces from the specified BSPEntry by ID, if this model does not <see cref="HasBSPProperties"/>,
        /// then BSPIndex is assumed as -1.
        /// </summary>
        /// <param name="BSPIndex">The index of the BSP entry.</param>
        /// <returns></returns>
        public IEnumerable<BSPFace> GetFaces(int BSPIndex = -1)
        {
            if (!HasBSPProperties) BSPIndex = -1;
            if (BSPIndex == -1) return Faces;
            return default; // BSPEntries[BSPIndex].Faces;
        }
        /// <summary>
        /// Gets the point at the specified index, optionally you can supply a frame number
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public BSPPoint? GetPointOrDefault(int Index, int Frame = 0, bool nested = false)
        {
            if (Frame == -1 && Frames.Count > 0) Frame++;
            if (Index < 0)
                return default;
            if (Points.Any(x => x.Index == Index))
                return Points.First(x => x.Index == Index);
            if (Frame < Frames.Count && Frame > -1)
            {
                if (GetFrame(Frame).Points.Any(x => x.Index == Index))
                    return GetFrame(Frame).Points.First(x => x.Index == Index);             
            }                              
            return default;
        }
        /// <summary>
        /// Gets the point at the specified index, optionally you can supply a frame number
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public BSPPoint GetPoint(int Index, int Frame = 0, bool nested = false)
        {
            if (Index < 0) 
                throw new Exception("Index is less than one.");
            var foundPoint = GetPointOrDefault(Index, Frame);
            if (foundPoint == default)
                throw new Exception($"The PointIndex: {Index} was not found on this model.");
            return foundPoint;
        }
        public BSPPoint FindPoint(int PointIndex)
        {
            BSPPoint? Search(int Frame)
            {
                return GetPointOrDefault(PointIndex, Frame);
            }
            var foundPoint = GetPointOrDefault(PointIndex);
            if (foundPoint != default) return foundPoint;
            foreach (var frame in Frames.Keys)
            {
                foundPoint = Search(frame);
                if (foundPoint != default) return foundPoint;
            }
            throw new Exception($"The PointIndex {PointIndex} could not be found on the entire model.");
        }
        /// <summary>
        /// Pushes a new frame to this shape object
        /// </summary>
        /// <param name="Frame">The index of the new frame to add</param>
        /// <param name="Points">The points on the given frame</param>
        /// <returns></returns>
        public bool PushFrame(string KeyframeName, params BSPPoint[] Data)
        {
            int currentIndex = Frames.Count;
            if (!FrameData.ContainsKey(KeyframeName)) // not yet added
                FrameData.Add(KeyframeName, new BSPFrame()
                {
                    Name = KeyframeName,
                    Points = Data
                });
            Frames.Add(currentIndex, KeyframeName);
            return true;
        }

        /// <summary>
        /// Serializes this object to the given stream
        /// </summary>
        /// <param name="Destination"></param>
        public void Serialize(Utf8JsonWriter Destination)
        {
            using (var doc = JsonSerializer.SerializeToDocument(this, new JsonSerializerOptions()
            {
                WriteIndented = true,
            }))
                doc.WriteTo(Destination);
        }

        public override string ToString()
        {
            return $"SHAPE - {Header?.Name ?? ""} Faces: {Faces.Count}, Frames: {Frames.Count}";
        }
    }
}
