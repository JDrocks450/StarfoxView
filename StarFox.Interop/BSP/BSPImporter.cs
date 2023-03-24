using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.MAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP
{
    internal class BSPImporterContext
    {
        internal enum PointsModes
        {
            None, 
            /// <summary>
            /// Counts by 2s
            /// </summary>
            PointsXb,
            /// <summary>
            /// Counts by 1s
            /// </summary>
            Pointsb, 
            /// <summary>
            /// Counts by 1s
            /// </summary>
            Pointsw,
            /// <summary>
            /// Counts by 2s
            /// </summary>
            PointsXw
        }

        //*** BSP VARS                        
        internal BSPShape? CurrentShape = default; // the currently parsing shape
        internal int currentFrame = -1, currentFrameDefinition = 0;
        internal int pointIndex = 0, pointActualIndex = 0, framePointIndexStart = 0, framePointActualIndexStart = 0;
        /// <summary>
        /// Describes the current point parsing mode
        /// </summary>
        internal PointsModes PointsMode { get; set; }
        /// <summary>
        /// The data width of the point definitions
        /// </summary>
        internal int PointsDataWidth => PointsMode switch
        {
            PointsModes.PointsXb => 2,
            PointsModes.Pointsb => 1,
            PointsModes.Pointsw => 1,
            PointsModes.PointsXw => 2,
            _ => 0,
        };
        /// <summary>
        /// If we're not parsing points, this is locked.
        /// </summary>
        internal bool pointsLocked => PointsMode == PointsModes.None;
        internal bool facesLocked = false;
        internal int frames = 0;
        //***

        /// <summary>
        /// Resets variables to default values
        /// </summary>
        public void ResetVars()
        {
            currentFrame = -1;
            PointsMode = PointsModes.None;
            facesLocked = false;
            pointIndex = 0;
            pointActualIndex = 0;
            currentFrameDefinition = 0;
            framePointIndexStart = framePointActualIndexStart = 0;
            frames = 0;
        }
        public void BeginPointsRegion(PointsModes Mode) => PointsMode = Mode;
        /// <summary>
        /// This function will set the context to be in FRAMES mode
        /// <para>In the source code this looks like:</para>
        /// <code>Frames [NumberOfFrames]</code>
        /// </summary>
        /// <param name="NumberOfFrames"></param>
        public void BeginFramesRegion(int NumberOfFrames)
        {
            frames = NumberOfFrames;
            currentFrame = 0;
            framePointIndexStart = pointIndex;
            framePointActualIndexStart = pointActualIndex;
        }
        /// <summary>
        /// This function will push a new frame onto the object's <see cref="BSPShape.Frames"/> stack
        /// <para>Generally call this when you see:</para>
        /// <code>jumptab</code>
        /// </summary>
        /// <param name="Name"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void BeginFrameDataDefine(string? Name = default)
        {
            if (CurrentShape == null) throw new InvalidOperationException("CurrentShape is null");
            if (currentFrameDefinition + 1 > frames) throw new InvalidOperationException("You're above the amount of frames defined.");
            CurrentShape.PushFrame(currentFrameDefinition, new()
            {
                Name = Name ?? $"FRAME_{currentFrameDefinition}",
                Points = new()
            });
            currentFrameDefinition++;
        }
        /// <summary>
        /// When the frame data is read, this will reset values in this context to values before the frames were read.
        /// </summary>
        public void ReturnFromFrameDataRegion()
        {
            currentFrame++;
            pointIndex = framePointIndexStart;
            pointActualIndex = framePointActualIndexStart;
        }
        public void PushPoint(PointsModes PointType, int x, int y, int z)
        {
            if (pointsLocked) // has endpoints been called?
                throw new InvalidDataException($"You're not in POINTS mode."); // yikes!
            bool compatible = PointsMode switch
            {
                PointsModes.Pointsb => PointType is PointsModes.PointsXb or PointsModes.Pointsb,
                PointsModes.PointsXb => PointType is PointsModes.PointsXb or PointsModes.Pointsb,
                PointsModes.PointsXw => PointType is PointsModes.Pointsw or PointsModes.PointsXw,
                PointsModes.Pointsw => PointType is PointsModes.Pointsw or PointsModes.PointsXw,
                _ => false
            };
            if (!compatible)
                throw new InvalidOperationException($"You're not in the correct mode to define a point like that. M: {PointsMode} T: {PointType}");
            var point = new BSPPoint(pointIndex, pointActualIndex, x, y, z); // make point
            pointIndex += PointsDataWidth;
            pointActualIndex++;
            if (currentFrame >= 0)
                CurrentShape.GetFrame(currentFrame).Points.Add(point); // push to current frame
            else CurrentShape.Points.Add(point);
        }
    }
    /// <summary>
    /// Parses and imports the given ASP BSP Tree file into model definitions.
    /// <para>More than one shape per file is completely supported.</para>
    /// </summary>
    public class BSPImporter : CodeImporter<BSPFile>
    {
        private ASMImporter baseImporter = new();
        public override string[] ExpectedIncludes => new string[] {
            "SHMACS.INC"
        };

        /// <summary>
        /// Initializes a new <see cref="BSPImporter"/>
        /// </summary>
        public BSPImporter()
        {

        }        

        public override void SetImports(params ASMFile[] Includes) => baseImporter.SetImports(Includes);

        private bool LookForShapeHeader(ASMLine line, BSPImporterContext Context)
        {
            //looks promising, is this line a shape header?
            if (!BSPShapeHeader.TryParse(line, out var header)) return false; // nope, it's not.
            //found header !!!
            Context.CurrentShape = new BSPShape(header); // created a new shape to dump info into
            //Reset vars to default values
            Context.ResetVars();
            return true;
        }

        private bool LookForFaceDefinition(ASMLine line, BSPImporterContext Context)
        {
            if (BSPFaceStructureConverter.TryParse(in line, out var Face))
            { // this line is a face call
                if (Face.PointIndices.Length > 3)
                {
                    var points = Face.PointIndices.Select(x => Context.CurrentShape.GetPoint(x.PointIndex)).ToArray();
                    var newVerts = BSPTriangulate.TriangulateVertices(points);
                    BSPPointRef[] refs = new BSPPointRef[newVerts.Count];
                    for(int i = 0; i < newVerts.Count; i++)
                    {
                        var pointref = newVerts[i];
                        var point = points.ElementAt(pointref);
                        refs[i] = new BSPPointRef()
                        {
                            PointIndex = point.Index,
                            Position = i
                        };
                    }
                    Face.PointIndices = refs;
                }
                Context.CurrentShape.Faces.Add(Face);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Imports the selected file to find shapes defined in the file
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public override async Task<BSPFile> ImportAsync(string FilePath)
        {
            //Import the shape file as assembly first
            var baseImport = await baseImporter.ImportAsync(FilePath);
            if (baseImport == default) throw new InvalidOperationException("That file could not be parsed.");            
            var file = ImportedObject = new BSPFile(baseImport); // from ASM file
            int currentLineIndex = -1; // the current line of assembly we're reading
            var asmContext = new BSPImporterContext(); // create a new context
            asmContext.ResetVars();
            foreach(var chunk in file.Chunks)
            {
                try
                {
                    currentLineIndex++;
                    if (chunk == null) continue; // ???
                    if (chunk is ASMLine line)//Let's always only look for lines.
                    { // found a line
                        if (!line.HasStructureApplied) continue; // hmm but this line doesn't have a recognized structure.
                        if (line.Structure is ASMMacroInvokeLineStructure macroInvoke)
                        {
                            if (asmContext.CurrentShape == default) // looking for a shape header...
                            {
                                _ = LookForShapeHeader(line, asmContext);
                                continue;
                            }
                            //looking for model stuff
                            //** advanced function calls first
                            if (LookForFaceDefinition(line, asmContext)) continue;
                            BSPImporterContext.PointsModes mode = BSPImporterContext.PointsModes.Pointsb;
                            //** then basic function calls
                            switch (macroInvoke.MacroReference.Name.ToLower())
                            {
                                case "frames": // indicates we're changing to frames
                                    asmContext.BeginFramesRegion(macroInvoke.TryGetParameter(0)?.TryParseOrDefault() ?? 0);
                                    continue;
                                case "pointsxw": // points width 2
                                    asmContext.BeginPointsRegion(BSPImporterContext.PointsModes.PointsXw);
                                    continue;
                                case "pointsw": // points width 1
                                    asmContext.BeginPointsRegion(BSPImporterContext.PointsModes.Pointsw);
                                    continue;
                                case "pointsxb": // points width 2
                                    asmContext.BeginPointsRegion(BSPImporterContext.PointsModes.PointsXb);
                                    continue;
                                case "pointsb": // points width 1
                                    asmContext.BeginPointsRegion(BSPImporterContext.PointsModes.Pointsb);
                                    continue;
                                case "jump": // move from this frame to the next
                                    asmContext.ReturnFromFrameDataRegion();
                                    break;
                                case "jumptab": // define new frame at the specifed inline label
                                    var name = macroInvoke.TryGetParameter(0)?.ParameterContent;
                                    asmContext.BeginFrameDataDefine(name);
                                    break;
                                case "fend": // lock faces
                                    asmContext.facesLocked = true;
                                    break;
                                case "endpoints": // points region closed
                                    asmContext.BeginPointsRegion(BSPImporterContext.PointsModes.None); // set points mode to none
                                    continue;
                                case "endshape":
                                    // shape complete
                                    // push shape to file and close shape
                                    file.Shapes.Add(asmContext.CurrentShape);
                                    asmContext.CurrentShape = null;
                                    continue;
                                case "pw":
                                    mode = BSPImporterContext.PointsModes.Pointsw;
                                    goto case "pb";
                                case "pb": // point definition
                                    var x = macroInvoke.TryGetParameter(0)?.TryParseOrDefault() ?? 0; //x
                                    var y = macroInvoke.TryGetParameter(1)?.TryParseOrDefault() ?? 0;//y
                                    var z = macroInvoke.TryGetParameter(2)?.TryParseOrDefault() ?? 0;//z
                                    asmContext.PushPoint(mode, x, y, z);
                                    break;
                            }
                        }
                    }
                }
                catch
                {
                    asmContext.ResetVars();
                }
            }
            end:
            return file;
        }

        internal override ImporterContext<IncludeType>? GetCurrentContext<IncludeType>()
        {
            return baseImporter.Context as ImporterContext<IncludeType>;
        }
    }
}
