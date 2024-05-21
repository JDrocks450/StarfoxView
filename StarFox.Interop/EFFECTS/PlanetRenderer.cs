using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.EFFECTS
{
    /// <summary>
    /// Outlines all of the individual options found in an <see cref="PlanetRenderer.PlanetRendererOptions"/> class
    /// </summary>
    public interface IPlanetRenderOptions
    {
        /// <summary>
        /// The original render size of the Planet's texture.
        /// <para/>Default is 32x32 pixels
        /// </summary>
        int TextureUniformSize { get; set; }
        /// <summary>
        /// How fast the planet rotates on the X Axis
        /// </summary>
        double RotationalSpeedX { get; set; }
        /// <summary>
        /// How fast the planet rotates on the Y Axis
        /// </summary>
        double RotationalSpeedY { get; set; }
        /// <summary>
        /// This multiplies the base resolution: 32x32 pixels by the set factor to increase the rendered
        /// resolution for the edge of the planet as well as the Spotlight while preserving the original texture's
        /// appearance
        /// </summary>
        double RenderScaleFactor { get; set; }
        /// <summary>
        /// The X position of the Spotlight on the planet
        /// <para/>This is measured in relative units -- 0 to 1 where 0 is far left and 1 is far right.
        /// <para/>Note: The spotlight is rendered in full resolution, as in the render width and height of the 
        /// <see cref="PlanetRenderer"/> is used -- not the actual Planet's rendered resolution.
        /// </summary>
        double SpotlightPositionX { get; set; }
        /// <summary>
        /// The Y position of the Spotlight on the planet
        /// <para/>This is measured in relative units -- 0 to 1 where 0 is north and 1 is south.
        /// <para/>Note: The spotlight is rendered in full resolution, as in the render width and height of the 
        /// <see cref="PlanetRenderer"/> is used -- not the actual Planet's rendered resolution.
        /// </summary>
        double SpotlightPositionY { get; set; }
        /// <summary>
        /// The maximum brightness of the Spotlight
        /// </summary>
        double SpotlightMaxLumination { get; set; }
        /// <summary>
        /// The minimum brightness of the whole planet, this can also be interpreted as the 
        /// Ambient light intensity
        /// </summary>
        double SpotlightMinLumination { get; set; }
        /// <summary>
        /// How sharply the spotlight cuts off past the <see cref="SpotlightDistance"/>
        /// </summary>
        double SpotlightIntensity { get; set; }
        /// <summary>
        /// How far across the planet the Spotlight will spread
        /// <para/>This is correlated with the <see cref="SpotlightMaxLumination"/>
        /// so it is important to set that first before adjusting this.
        /// </summary>
        double SpotlightDistance { get; set; }
    }

    /// <summary>
    /// Renders a Starfox-like planet image that rotates over a given timestep.
    /// <para/>Please see: <see cref="PlanetRendererOptions"/> to customize this renderer
    /// </summary>
    public sealed class PlanetRenderer : AnimatorEffect<Bitmap>
    {
        /// <summary>
        /// Use these options to customize this <see cref="PlanetRenderer"/>
        /// </summary>
        public class PlanetRendererOptions : IPlanetRenderOptions
        {
            public int TextureUniformSize { get; set; } = 32;
            public double RotationalSpeedX { get; set; } = 15;
            public double RotationalSpeedY { get; set; } = 0;
            public double RenderScaleFactor { get; set; } = 4;
            public double SpotlightPositionX { get; set; } = .75;
            public double SpotlightPositionY { get; set; } = .15;
            public double SpotlightMaxLumination { get; set; } = 1.25;
            public double SpotlightMinLumination { get; set; } = .15;
            public double SpotlightIntensity { get; set; } = .5;
            public double SpotlightDistance { get; set; } = 1;
        }

        private Color[,]? cacheImage;
        private Bitmap planetTexture;
        private double planetRotationalSpeedX => Options.RotationalSpeedX;
        private double planetRotationalSpeedY => Options.RotationalSpeedY;
        private double XScroll, YScroll;
        private int PlanetTextureW; 
        private int PlanetTextureH;

        /// <summary>
        /// The custom settings applied to this object
        /// <para/>It is safe to change these options while animating
        /// </summary>
        public PlanetRendererOptions Options { get; set; } = new();

        /// <summary>
        /// Creates a new <see cref="PlanetRenderer"/> with no texture set.
        /// You need to then call <see cref="LoadTexture(Bitmap, PlanetRendererOptions?)"/>
        /// to bring this <see cref="AnimatorEffect{T}"/> into READY status
        /// </summary>
        public PlanetRenderer() : base(false)
        {
            AnimatorStatus = AnimatorStatus.NOT_INIT;
        }

        public PlanetRenderer(Bitmap PlanetTexture, PlanetRendererOptions? Options = null) : this()
        {
            LoadTexture(PlanetTexture, Options);
        }

        public void LoadTexture(Bitmap PlanetTexture, PlanetRendererOptions? Options = null)
        {
            if (Options != null)
                this.Options = Options;
            planetTexture = PlanetTexture;
            PlanetTextureH = PlanetTexture.Height;
            PlanetTextureW = PlanetTexture.Width;
            AnimatorStatus = AnimatorStatus.READY;
        }

        private double Distance(Point A, Point B) => Math.Sqrt(Math.Pow(B.X - A.X, 2) + Math.Pow(B.Y - A.Y, 2));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public override Bitmap RenderOnce(TimeSpan DeltaTime)
        {
            if (IsDisposed) throw new ObjectDisposedException($"{GetType().Name} instance has been disposed, yet is now trying to be used.");
            if (planetTexture == null)
                throw new ArgumentNullException(nameof(planetTexture));
            if (cacheImage == null)
                cacheImage = CopyPixels(planetTexture);

            //**Render resolution and original resolution here
            double O_SCALE_FACTOR = Options.RenderScaleFactor;
            //Scale factor can be changed mid-function -- O_SCALE_FACTOR is the defacto
            double SCALE_FACTOR = O_SCALE_FACTOR;
            int RENDER_W = (int)(Options.TextureUniformSize * O_SCALE_FACTOR), RENDER_H = RENDER_W;

            double SCALED_REN_W() => RENDER_W / SCALE_FACTOR;
            double SCALED_REN_H() => RENDER_H / SCALE_FACTOR;

            Point LightSource = new Point((int)(SCALED_REN_W() * Options.SpotlightPositionX), (int)(SCALED_REN_H() * Options.SpotlightPositionY));

            long ticket = CreateBuffer(RENDER_W, RENDER_H);
            for (int fX = 0; fX < RENDER_W; fX++)
            {
                int destX = (int)XScroll + fX;
                double X = fX / (RENDER_W / 2.0);

                //circular function is below, flipped on the x axis
                //(x-1)^2+(y^2)=1 which is also (x-a)^2+(y-b)^2=r^2
                double columnHeight = Math.Sqrt(1 - Math.Pow(X - 1, 2)) * (RENDER_H / 2) * 2;
                int yShift = (int)((RENDER_H / 2) - (columnHeight / 2));
                for (int Y = 0; Y < columnHeight; Y++)
                {
                    #region BULGE_DISABLED
                    var destY = yShift + Y;
                    Point center = new Point(RENDER_W / 2, RENDER_H / 2);
                    Point actualPosition = new Point(fX, destY);
                    double distance = Distance(actualPosition, center) / (RENDER_W * 4);
                    double YDistance = Distance(new Point(center.X, actualPosition.Y), center) / (RENDER_H / 2);
                    YDistance = 1 - YDistance;
                    //SCALE_FACTOR = O_SCALE_FACTOR - (YDistance * .01);
                    #endregion

                    int sourceY = (int)((Y+yShift+YScroll) / SCALE_FACTOR) % PlanetTextureH;
                    if (sourceY < 0)
                        sourceY = PlanetTextureH + sourceY;
                    int sourceX = (int)(destX / SCALE_FACTOR) % PlanetTextureW;
                    if (sourceX < 0)
                        sourceX = PlanetTextureW + sourceX;

                    //calculate spotlight lighting
                    Point textureSourcePos = new Point((int)(fX / SCALE_FACTOR), sourceY);
                    double lightStrength = Math.Max(Options.SpotlightMinLumination, Math.Min(Options.SpotlightMaxLumination, Options.SpotlightMaxLumination - Distance(textureSourcePos, LightSource) / 
                        (Math.Abs((LightSource.Y < SCALED_REN_H()/2 ? SCALED_REN_H() : 0) - LightSource.Y)*Options.SpotlightDistance + Options.SpotlightIntensity)));

                    Color color = cacheImage[sourceX, sourceY];
                    color = Color.FromArgb(255, (int)Math.Min(255,color.R * lightStrength), 
                        (int)Math.Min(255,color.G * lightStrength), (int)Math.Min(255, color.B * lightStrength));
                    SetPixel(ticket, fX, destY, color);
                }
            }
            XScroll += DeltaTime.TotalSeconds * planetRotationalSpeedX;
            YScroll += DeltaTime.TotalSeconds * planetRotationalSpeedY;
            return CompleteBuffer(ticket);
        }

        protected override bool OnDispose()
        {
            if (planetTexture != null)
                lock (planetTexture)
                {
                    planetTexture.Dispose();
                    planetTexture = null;
                }
            return true;
        }
    }
}
