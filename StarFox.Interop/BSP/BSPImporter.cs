//#define SPECIFIC

using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.MAP;
using StarFox.Interop.MISC;
using StarFox.Interop.MSG;
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
        internal ASMFile[]? Includes;
        internal BSPFile? CurrentFile;
        internal BSPShape? CurrentShape = default; // the currently parsing shape
        internal int currentFrame = -1;
        internal string? currentFrameDefinition = default, currentFrameEndLabel = default;
        internal int pointIndex = 0, framePointIndexStart = 0;
        internal int frameDefinitionAmount = 0;
        internal bool BSPMode = false;
        internal string? BSPEndLabel;
        //----

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
        public StringBuilder? ErrorList => CurrentFile?.ImportErrors;

        /// <summary>
        /// Resets variables to default values
        /// </summary>
        public void ResetVars()
        {
            currentFrame = -1;
            currentFrameDefinition = null;
            PointsMode = PointsModes.None;
            facesLocked = false;
            pointIndex = 0;
            framePointIndexStart = 0;
            frames = 0;
            frameDefinitionAmount = 0;
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
            bool firstDefine = frames == 0;
            frames += NumberOfFrames;
            currentFrame = 0;
            frameDefinitionAmount = NumberOfFrames;
            framePointIndexStart = pointIndex;
        }
        /// <summary>
        /// This function will push a new frame onto the object's <see cref="BSPShape.Frames"/> stack
        /// <para>Generally call this when you see:</para>
        /// <code>jumptab</code>
        /// </summary>
        /// <param name="KeyframeName"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void BeginFrameDataDefine(string KeyframeName)
        {
            KeyframeName = KeyframeName.TrimStart('.'); // trim the period we don't need it
            if (CurrentShape == null) throw new InvalidOperationException("CurrentShape is null");
            if (KeyframeName == null) throw new InvalidDataException("The keyframe has to have a name.");
            CurrentShape.PushFrame(KeyframeName);
        }
        /// <summary>
        /// Begins BSP Mode
        /// <code>BSPInit end_label</code>
        /// </summary>
        /// <param name="EndLabel"></param>
        public void BeginBSPRegion(string EndLabel)
        {
            BSPMode = true;
            BSPEndLabel = EndLabel;
        }
        /// <summary>
        /// When the frame data is read, this will reset values in this context to values before the frames were read.
        /// </summary>
        public void ReturnFromFrameDataRegion()
        {
            currentFrame++;
            pointIndex = framePointIndexStart;
            currentFrameDefinition = default;
        }
        /// <summary>
        /// Will create a point.
        /// PointType is important, as the width of the point is determined by its type.
        /// <code>pb, pw [etc.] x,y,z</code>
        /// Note that some macro functions will apply math to the result, like the following:
        /// <code>pbd2 x,y,z</code>
        /// Will divide each component by two and store that.
        /// </summary>
        /// <param name="PointType">The width of the point being added, determined by the Points line.</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="divisor">Divide components by this divisor</param>
        public BSPPoint MakePoint(int x, int y, int z, float divisor = 1)
        {
            var point = new BSPPoint(pointIndex, -(int)(x / divisor), -(int)(y / divisor), (int)(z / divisor)); // make point
            //pointIndex += PointsDataWidth;
            pointIndex++;
            return point;
        }
        /// <summary>
        /// Tries to find a Shape Header, if <see langword="true"/> then the new shape definition is automatically
        /// set to be the <see cref="CurrentShape"/>
        /// </summary>
        /// <param name="line"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        internal bool LookForShapeHeader(ASMLine line)
        {
            //looks promising, is this line a shape header?
            if (!BSPShapeHeader.TryParse(line, out var header, Includes)) return false; // nope, it's not.
#if SPECIFIC
            if (header.Name != "training") return false;
#endif
            if (CurrentShape != default) // yikes, we found a stray definition, or perhaps one that references another.
            {
                // stray shape, push this header to the strays list.          
                CurrentFile.BlankShapes.Add(CurrentShape);
                CurrentShape = null;
            }
            //found header !!!
            CurrentShape = new BSPShape(header); // created a new shape to dump info into
            //Reset vars to default values
            ResetVars();
            return true;
        }
        /// <summary>
        /// Will push a point to the base shape, not a keyframe.
        /// PointType is important, as the width of the point is determined by its type.
        /// <code>pb, pw [etc.] x,y,z</code>
        /// Note that some macro functions will apply math to the result, like the following:
        /// <code>pbd2 x,y,z</code>
        /// Will divide each component by two and store that.
        /// </summary>
        /// <param name="PointType">The width of the point being added, determined by the Points line.</param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="divisor">Divide components by this divisor</param>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void PushPoint(PointsModes PointType, int x, int y, int z, float divisor = 1)
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
            bool XMode = PointsMode is PointsModes.PointsXb or PointsModes.PointsXw;
        XMode:
            int shift = CurrentShape?.Header?.Shift ?? 0;
            shift = 0;
            var point = MakePoint(x << shift, y << shift, z << shift, divisor);
            CurrentShape.LargestXPoint = Math.Max(CurrentShape.LargestXPoint, Math.Abs(x));
            CurrentShape.LargestYPoint = Math.Max(CurrentShape.LargestYPoint, Math.Abs(y));
            CurrentShape.LargestZPoint = Math.Max(CurrentShape.LargestZPoint, Math.Abs(z));
            if (currentFrameDefinition != null) // are we in a frame?
            {
                CurrentShape.FrameData[currentFrameDefinition].AddPoint(point);
#if SPECIFIC
                ErrorList.AppendLine($"PUSHPOINT: to FRAME: {currentFrameDefinition} INDEX: {point.Index} ACTUAL: {point.ActualIndex} | {point}");
#endif
            }
            else if (CurrentShape.Frames.Count > 0) // do any frames exist before this one?
            { // add this point found to all previous frames in order
                for (int i = 0; i < frameDefinitionAmount; i++)
                {
                    var frame = CurrentShape.GetFrame((frames - i) - 1);
                    frame?.AddPointSequential(point,out pointIndex);
#if SPECIFIC
                    ErrorList.AppendLine($"PUSHPOINT: to FRAME: {currentFrameDefinition} INDEX: {point.Index} ACTUAL: {point.ActualIndex} | {point}");
#endif
                }
            }
            else
            {
                CurrentShape.AddPoint(point);
#if SPECIFIC
                ErrorList.AppendLine($"PUSHPOINT: to FRAME: default_0 INDEX: {point.Index} ACTUAL: {point.ActualIndex} | {point}");
#endif
            }
            if (XMode)
            {
                x *= -1;
                XMode = false;
                goto XMode;
            }
        }
        /// <summary>
        /// Pushes a BSP definition to the BSP jump table.
        /// <code>BSP id,faces_ptr,.bsp-</code>
        /// </summary>
        /// <param name="ID">The id of this BSP.</param>
        /// <param name="FacesPtr">The faces table indicating where to find the data in this BSP.</param>
        /// <param name="JumpPtr">The ptr to the next BSP.</param>
        internal void PushBSP(int ID, string FacesPtr, string JumpPtr)
        {
            if (CurrentShape == null) throw new NullReferenceException("There is no current shape to add this to.");
            CurrentShape.BSPEntries.Add(ID, new BSPEntry(ID, FacesPtr, JumpPtr));
        }
    }
    /// <summary>
    /// Parses and imports the given ASP BSP Tree file into model definitions.
    /// <para>More than one shape per file is completely supported.</para>
    /// </summary>
    public class BSPImporter : BasicCodeImporter<BSPFile>
    {
        public override string[] ExpectedIncludes => new string[] {
            "SHMACS.INC", //Shape Macros required
            "STRATEQU.INC" // Used for constants that describe sizing constraints, etc.
        };
        BSPImporterContext? asmContext;        

        /// <summary>
        /// Initializes a new <see cref="BSPImporter"/>
        /// </summary>
        public BSPImporter()
        {

        }                             

        /// <summary>
        /// After all shapes have been defined and parsed, this function can be used to attempt to turn Blank Shapes
        /// into their original forms through finding where their point data actually lives.
        /// </summary>
        private void DereferenceBlankShapes(BSPFile File)
        {
            List<BSPShape> completes = new(); // completed shapes
            foreach(var blankShape in File.BlankShapes) // blank shapes iteration
            {
                foreach(var shape in File.Shapes) // iterate over all shapes
                {
                    if (shape.Equals(blankShape)) 
                        continue; // ignore the current blank shape, please.
                    if (shape.Header.PointPtr == blankShape.Header.PointPtr &&
                        shape.Header.FacePtr == blankShape.Header.FacePtr)
                    { // we FOUND the data!
                        blankShape.CopyData(shape);
                        if (blankShape.Header.Name == shape.Header.Name)
                        {
                            var tempName = shape.Header.Name;
                            if (!string.IsNullOrWhiteSpace(blankShape.Header.InlineLabelName))
                                blankShape.Header.DataPointer = shape.Header.Name;                    
                        }
                        completes.Add(blankShape);
                    }    
                }
            }
            foreach (var shape in completes)
            {
                File.Shapes.Add(shape);
                File.BlankShapes.Remove(shape);
            }
            completes.Clear();
        }
        private void FixDuplicateNames(BSPFile File)
        {
            foreach (var shape in File.Shapes) // iterate over all shapes
            {
                var matches = File.Shapes.Where(x => shape != x && x.Header.UniqueName == shape.Header.UniqueName);
                int index = 0;
                foreach (var match in matches)
                {
                    index++;
                    match.Header.UniqueName = match.Header.Name + $"({index})";
                }
            }
        }
        /// <summary>
        /// Attempts to turn this line into a <see cref="BSPFace"/> and returns whether that's feasible or not.
        /// If true, then the face is automatically processed to the <see cref="BSPImporterContext.CurrentShape"/>
        /// </summary>
        /// <param name="line"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        private bool LookForFaceDefinition(ASMLine line, BSPImporterContext Context)
        {
            if (BSPFaceStructureConverter.TryParse(in line, out var Face))
            { // this line is a face call
                if (Face.PointIndices.Length > 3)
                { // make into a TRI instead of an any sided shape
                    var points = Face.PointIndices.Select(x => Context.CurrentShape.FindPoint(x.PointIndex)).ToArray();
                    bool result = BSPTriangulate.EarClipTriangulationAlgorithm(points, Face.Normal, out var newVerts);
                    if (!result)
                    {
                        result = BSPTriangulate.EarClipTriangulationAlgorithm(points.Reverse(), Face.Normal, out newVerts);
                        if (!result)
                        {
                            Context.ErrorList.AppendLine($"*****TRIANGULATION FAILURE! {Context.CurrentShape.Header.Name}*****");
                        }
                    }
                    BSPPointRef[] refs = new BSPPointRef[newVerts.Count];
                    for (int i = 0; i < newVerts.Count; i++)
                    {
                        var point = newVerts[i];
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
            asmContext = new BSPImporterContext() // create a new context
            {
                CurrentFile = file,
                Includes = baseImporter.CurrentIncludes
            };
            asmContext.ResetVars();
            foreach(var chunk in file.Chunks)
            {
                try
                {
                    currentLineIndex++;
                    if (chunk == null) continue; // ???
                    if (chunk is ASMLine line)//Let's always only look for lines.
                    { // found a line
                        if (asmContext.currentFrameDefinition != null && asmContext.currentFrameEndLabel != null && 
                            line.Text.NormalizeFormatting().ToUpper().StartsWith(asmContext.currentFrameEndLabel.ToUpper()))
                        { //we are leaving a keyframe                            
                            asmContext.ReturnFromFrameDataRegion();
                        }
                        if (!line.HasStructureApplied) continue; // hmm but this line doesn't have a recognized structure.
                        if (line.Structure is ASMMacroInvokeLineStructure macroInvoke)
                        {
                            //ALWAYS LOOK FOR HEADERS
                            if (asmContext.LookForShapeHeader(line))
                                continue; // Started a new shape  
                            if (asmContext.CurrentShape == default) continue;
                            //NOT A HEADER, CHECK IF IT HAS A LABEL (JumpTab)
                            if (line.HasInlineLabel)
                            {
                                if (asmContext.CurrentShape.FrameData.ContainsKey(line.InlineLabel))
                                    asmContext.currentFrameDefinition = line.InlineLabel; // we're now defining a keyframe
                            }
                            //looking for model stuff
                            //** advanced function calls first -- Recognize Faces
                            if (LookForFaceDefinition(line, asmContext)) continue;
                            BSPImporterContext.PointsModes mode = BSPImporterContext.PointsModes.Pointsb;
                            //** then basic function calls (recognize everything else)
                            float pbDivisor = 1, yFactor = 1; // point-specific macro math operations
                            switch (macroInvoke.MacroReference.Name.ToLower())
                            {
                                //***BSP
                                case "bspinit": // indicates we're defining a BSP Region
                                    asmContext.BeginBSPRegion(macroInvoke.TryGetParameter(0).ParameterContent);
                                    continue;
                                case "bsp":
                                    continue;
                                    asmContext.PushBSP(
                                        macroInvoke.TryGetParameter(0).TryParseOrDefault(), 
                                        macroInvoke.TryGetParameter(1).ParameterContent,
                                        macroInvoke.TryGetParameter(2).ParameterContent
                                    );
                                    continue;
                                //**END BSP
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
                                    asmContext.currentFrameEndLabel = macroInvoke.TryGetParameter(0)?.ParameterContent;
                                    asmContext.ReturnFromFrameDataRegion();
                                    continue;
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
                                case "pbd2": // point definition, divide by 2
                                    pbDivisor = 2;
                                    goto case "pb";
                                case "pby2": // point definition, multiply Y component by two
                                    yFactor = 2;
                                    goto case "pb";
                                case "pb": // point definition
                                    var x = macroInvoke.TryGetParameter(0)?.TryParseOrDefault() ?? 0; //x
                                    var y = macroInvoke.TryGetParameter(1)?.TryParseOrDefault() ?? 0; //y
                                    var z = macroInvoke.TryGetParameter(2)?.TryParseOrDefault() ?? 0; //z
                                    asmContext.PushPoint(mode, x, (int)(y*yFactor), z, pbDivisor);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    asmContext?.ErrorList?.AppendLine("****");
                    asmContext?.ErrorList?.AppendLine($"SHAPE: {asmContext.CurrentShape}");
                    asmContext?.ErrorList?.AppendLine($"CHUNK: {chunk}");
                    asmContext?.ErrorList?.AppendLine($"FRAME: {asmContext.currentFrame}");
                    asmContext?.ErrorList?.AppendLine($"ERROR: ");
                    asmContext?.ErrorList?.AppendLine(ex.ToString());
                    asmContext?.ErrorList?.AppendLine("****");
                    asmContext.ResetVars();
                }
            }
            DereferenceBlankShapes(file);
            FixDuplicateNames(file);
            LookupAllShapeHeaders(baseImport, file);
        end:
            ErrorOut = file.ImportErrors;
            return file;
        }

        private void LookupAllShapeHeaders(ASMFile BaseFile, BSPFile Target)
        {
            foreach (var chunk in BaseFile.Chunks.OfType<ASMLine>())
            {
                if (!chunk.HasStructureApplied) continue;
                if (chunk.StructureAsMacroInvokeStructure == default) continue;
                if (chunk.StructureAsMacroInvokeStructure.MacroReference.Name.ToLower() !=
                    "shapehdr") continue;
                var sName = chunk.StructureAsMacroInvokeStructure.Parameters.Last().ParameterContent;
                var fooSName = sName;
                int tries = 1;
                while (!Target.ShapeHeaderEntries.Add(fooSName))
                {
                    fooSName = sName + "_" + tries;
                    tries++;
                }
            }
        }
    }
}
