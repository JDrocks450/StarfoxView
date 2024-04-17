using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.EFFECTS
{
    public sealed class PlanetRenderer : AnimatorEffect<Bitmap>
    {
        private Color[,]? cacheImage;
        private Bitmap planetTexture;
        private readonly double planetRotationalSpeed;
        private double XScroll;
        private int PlanetTextureW; 
        private int PlanetTextureH;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PlanetTexture"></param>
        /// <param name="PlanetRotationalSpeed">Measured in Pixels per Second</param>
        public PlanetRenderer(Bitmap PlanetTexture, double PlanetRotationalSpeed = 30) : base(false)
        {
            planetTexture = PlanetTexture;
            planetRotationalSpeed = PlanetRotationalSpeed;
            PlanetTextureH = PlanetTexture.Height;
            PlanetTextureW = PlanetTexture.Width;
        }

        private double Distance(Point A, Point B) => Math.Sqrt(Math.Pow(B.X - A.X, 2) + Math.Pow(B.Y - A.Y, 2));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public override Bitmap RenderOnce(TimeSpan DeltaTime)
        {
            if (planetTexture == null)
                throw new ArgumentNullException(nameof(planetTexture));
            if (cacheImage == null)
                cacheImage = CopyPixels(planetTexture);

            const double O_SCALE_FACTOR = 4;
            double SCALE_FACTOR = O_SCALE_FACTOR;
            const int RENDER_W = 32 * 4, RENDER_H = RENDER_W;
            double SCALED_REN_W() => RENDER_W / SCALE_FACTOR;
            double SCALED_REN_H() => RENDER_H / SCALE_FACTOR;

            Point LightSource = new Point((int)(SCALED_REN_W() / 2), 5);

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
                    var destY = yShift + Y;
                    Point center = new Point(RENDER_W/2, RENDER_H/2);
                    Point actualPosition = new Point(fX, destY);
                    double distance = Distance(actualPosition, center) / (RENDER_W * 4);
                    double YDistance = Distance(new Point(center.X, actualPosition.Y), center) / (RENDER_H / 2);
                    YDistance = 1 - YDistance;
                    //SCALE_FACTOR = O_SCALE_FACTOR - (YDistance * .01);
                    
                    int sourceY = (int)((Y + yShift) / SCALE_FACTOR) % PlanetTextureH;
                    if (sourceY < 0)
                        sourceY = PlanetTextureH - sourceY;
                    int sourceX = (int)(destX / SCALE_FACTOR) % PlanetTextureW;
                    if (sourceX < 0)
                        sourceX = PlanetTextureW - sourceX;

                    //calculate lighting
                    Point textureSourcePos = new Point((int)(fX / SCALE_FACTOR), sourceY);
                    double lightStrength = Math.Max(0, Math.Min(1, 1 - Distance(textureSourcePos, LightSource) / SCALED_REN_H() + .05));

                    Color color = cacheImage[sourceX, sourceY];
                    color = Color.FromArgb(255, (int)(color.R * lightStrength), (int)(color.G * lightStrength), (int)(color.B * lightStrength));
                    SetPixel(ticket, fX, destY, color);
                }
            }
            XScroll += DeltaTime.TotalSeconds * planetRotationalSpeed;
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
