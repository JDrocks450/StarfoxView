using System.Drawing;

namespace StarFox.Interop.EFFECTS
{
    /// <summary>
    /// Renders a <see cref="Bitmap"/> in a similar way to how
    /// the Awesome Blackhole or Final Level backgrounds appear in Starfox (SNES).
    /// </summary>
    public sealed class WavyBackgroundRenderer : AnimatorEffect<Bitmap>
    {
        const int RENDER_W = StarfoxEqu.RENDER_W, RENDER_H = StarfoxEqu.RENDER_H,
            SCR_W = StarfoxEqu.SCR_W, SCR_H = StarfoxEqu.SCR_H;

        private readonly Bitmap _SCRFile;
        
        public WavyEffectStrategies Strategy { get; set; } = WavyEffectStrategies.None;
        
        /// <summary>
        /// Dictates how the waviness is created and how the background appears
        /// </summary>
        public enum WavyEffectStrategies
        {
            None,
            Simple,
            SineFullscreen, 
            SineMirrored
        }

        /// <summary>
        /// Creates a new <see cref="WavyBackgroundRenderer"/> with the given <see cref="Bitmap"/> image
        /// </summary>
        /// <param name="Strategy"></param>
        /// <param name="SCR"></param>
        public WavyBackgroundRenderer(WavyEffectStrategies Strategy, Bitmap SCR, bool DiagnosticsEnabled = false) : base(DiagnosticsEnabled)
        {
            this.Strategy = Strategy;
            _SCRFile = SCR;
        }

        /// <summary>
        /// Renders a new frame of the background animation.
        /// <para/>Advances this object's internal clock by the given <paramref name="DeltaTime"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Target platform is Windows only.")]
        public override Bitmap RenderOnce(TimeSpan DeltaTime)
        {
            if (BackgroundCache == null)
                WavyBackground_CopyData2Cache();
            if (BackgroundCache == null)
                throw new InvalidDataException("No bitmap was provided, or it has been disposed of.");
            if (Strategy == WavyEffectStrategies.None)
                throw new InvalidDataException("You didn't specify a strategy to use, Strategy is None.");

            Func<Bitmap>? BackgroundFunction = Strategy switch
            {
                WavyEffectStrategies.Simple => SetWavyBackground_Strategy1,
                WavyEffectStrategies.SineFullscreen => SetWavyBackground_Strategy2,
                WavyEffectStrategies.SineMirrored => SetWavyBackground_Strategy3,
                _ => null
            };
            if (BackgroundFunction == null)
                throw new NotImplementedException("That waviness function doesn't appear to have been coded yet.");

            animationTime += DeltaTime;
            return BackgroundFunction();
        }        

        #region RENDERCODE
        // ** Dynamic Backgrounds
        /// <summary>
        /// 512^2 * <see langword="sizeof"/>(<see cref="Color"/>) bytes large max
        /// </summary>
        System.Drawing.Color[,] BackgroundCache = null;
        int SinX = 0;        
        // **

        /// <summary>
        /// Caches <see cref="_SCRFile"/> to a color[] because <see cref="Bitmap"/> is unreal how slow it is
        /// and the files we're handling a limited to 512^2
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Target platform is Windows only.")]
        private bool WavyBackground_CopyData2Cache()
        {
            BackgroundCache = null;
            if (_SCRFile == null) return false;
            BackgroundCache = CopyPixels(_SCRFile);
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Target platform is Windows only.")]
        private Bitmap SetWavyBackground_Strategy1()
        {
            int rowOffset = 0, sinX = SinX;

            long Ticket = CreateBuffer(RENDER_W, RENDER_H);
            Point renderOutPixel = new Point();
            int yOffset = (int)(300 * Math.Sin(SinX * .0001));
            for (int vScan = 0; vScan < RENDER_H; vScan++)
            {
                renderOutPixel.X = 0;
                int pY = yOffset + (vScan > RENDER_H / 2 ? RENDER_H / 2 - (vScan % (RENDER_H / 2)) : vScan);
                if (pY >= SCR_H)
                    pY = Math.Abs(SCR_H - ((pY % SCR_H) + 1));
                else
                    pY = Math.Abs(pY);
                //take row 
                for (int rowX = 0; rowX < RENDER_W; rowX++)
                {
                    int pX = rowX + rowOffset;
                    if (pX >= SCR_W)
                        pX = Math.Abs(SCR_W - ((pX % SCR_W) + 1));
                    else
                        pX = Math.Abs(pX);
                    var color = BackgroundCache[pX, pY];
                    SetPixel(Ticket, renderOutPixel.X, renderOutPixel.Y, color);
                    renderOutPixel.X++;
                }
                renderOutPixel.Y++;
                SinX++;
                rowOffset = ((int)(25 * Math.Sin(SinX * .05) * Math.Sin(SinX * .15))) + yOffset;
            }
            return CompleteBuffer(Ticket);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Target platform is Windows only.")]
        private Bitmap SetWavyBackground_Strategy2()
        {
            int rowOffset = 0, sinX = SinX;

            long Ticket = CreateBuffer(RENDER_W, RENDER_H);
            Point renderOutPixel = new Point();
            //SINE WAVE: Go over half the render window around .15 cycles / second (very slowly) 
            int yOffset = 0;
            double amplitude = RENDER_W / 4.0;
            int xOffset = (int)(amplitude * Math.Sin(2 * Math.PI * (.05) * animationTime.TotalSeconds));
            double amplitudeModulator = 0;
            TimeSpan vScanWaveTime = animationTime;
            for (int vScan = 0; vScan < RENDER_H; vScan++)
            {
                renderOutPixel.X = 0;
                int pY = yOffset + (vScan > RENDER_H / 2 ? RENDER_H / 2 - (vScan % (RENDER_H / 2)) : vScan);
                if (pY >= SCR_H)
                    pY = Math.Abs(SCR_H - ((pY % SCR_H) + 1));
                else
                    pY = Math.Abs(pY);
                //take row 
                for (int rowX = 0; rowX < RENDER_W; rowX++)
                {
                    int pX = xOffset + rowX + rowOffset;
                    if (pX >= SCR_W)
                        pX = Math.Abs(SCR_W - ((pX % SCR_W) + 1));
                    else
                        pX = Math.Abs(pX);
                    if (BackgroundCache == null) break;
                    lock (BackgroundCache)
                    {
                        var color = BackgroundCache[pX, pY];
                        SetPixel(Ticket,(int)renderOutPixel.X, (int)renderOutPixel.Y, color);
                    }
                    renderOutPixel.X++;
                    rowOffset = (int)(amplitude * Math.Sin(2 * Math.PI * (.5) * vScanWaveTime.TotalSeconds));
                    vScanWaveTime += TimeSpan.FromMilliseconds(.1);
                    //SinX++;
                }
                renderOutPixel.Y++;
            }
            return CompleteBuffer(Ticket);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Target platform is Windows only.")]
        private Bitmap SetWavyBackground_Strategy3()
        {            
            int rowOffset = 0, sinX = SinX;

            long Ticket = CreateBuffer(RENDER_W, RENDER_H);
            Point renderOutPixel = new Point();
            //SINE WAVE: Go over half the render window around .15 cycles / second (very slowly) 
            int yOffset = 0;
            double amplitude = RENDER_W / 4.0;
            int xOffset = (int)(RENDER_W / 4.0 * Math.Sin(2 * Math.PI * (.05) * animationTime.TotalSeconds));
            double amplitudeModulator = 0;
            TimeSpan vScanWaveTime = animationTime;
            for (int vScan = 0; vScan < RENDER_H / 2; vScan++)
            {
                renderOutPixel.X = 0;
                int pY = yOffset + vScan;
                if (pY >= SCR_H)
                    pY = Math.Abs(SCR_H - ((pY % SCR_H) + 1));
                else
                    pY = Math.Abs(pY);
                //take row 
                for (int rowX = 0; rowX < RENDER_W; rowX++)
                {
                    int pX = xOffset + rowX + rowOffset;
                    if (pX >= SCR_W)
                        pX = Math.Abs(SCR_W - ((pX % SCR_W) + 1));
                    else
                        pX = Math.Abs(pX);
                    var color = BackgroundCache[pX, pY];
                    SetPixel(Ticket, (int)renderOutPixel.X, (int)renderOutPixel.Y, color);
                    renderOutPixel.X++;
                    rowOffset = (int)(amplitude * Math.Sin(2 * Math.PI * (.5) * vScanWaveTime.TotalSeconds));
                    vScanWaveTime += TimeSpan.FromMilliseconds(.1);
                }
                renderOutPixel.Y++;
            }
            int line = (RENDER_H / 2) - 1;
            for (int vScan = (RENDER_H / 2) - 1; vScan > 0; vScan--)
            {
                for (int rowX = 0; rowX < RENDER_W; rowX++)
                {
                    var color = GetPixel(Ticket, rowX, vScan);
                    SetPixel(Ticket, rowX, line, color);
                }
                line++;
            }
            return CompleteBuffer(Ticket);
        }
        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Target platform is Windows only.")]
        protected override bool OnDispose()
        {
            if (Disposing)
                throw new InvalidOperationException("Attempted to call dispose on an already Disposing object.");
            Disposing = true;

            //**Dispose
            BackgroundCache = null;            
            if (_SCRFile != null)
                lock (_SCRFile) 
                    _SCRFile.Dispose();
            //

            Disposing = false;
            return true;
        }        
    }
}
