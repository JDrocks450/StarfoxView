using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF.UI.Extensions.Backgrounds
{
    /// <summary>
    /// Interaction logic for SpacePanel.xaml
    /// </summary>
    public partial class SpacePanel : UserControl
    {
        const double DEPTH = 200, LINEAR_RANGE = 20, STARS = DEPTH * 2;

        static readonly Random Rand = new Random();

        static readonly Vector3DCollection Normals = new()
        {
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1),
            new Vector3D(0, 0, 1)
        };
        const float POS = .02f;
        static readonly Point3DCollection Positions = new()
        {
            new Point3D((float)-POS, (float)-POS, (float)POS),
            new Point3D((float)POS, (float)-POS, (float)POS),
            new Point3D((float)POS, (float)POS, (float)POS),
            new Point3D((float)POS, (float)POS, (float)POS),
            new Point3D((float)-POS, (float)POS, (float)POS),
            new Point3D((float)-POS, (float)-POS, (float)POS)
        };
        static readonly Int32Collection Triangles = new() { 0, 1, 2, 3, 4, 5 };

        public SpacePanel()
        {
            InitializeComponent();

            LoadScene(Colors.White, Colors.White, Colors.White, Colors.DarkCyan, Colors.DeepSkyBlue);
        }

        private void LoadScene(params Color[] Palette)
        {
            List<Point3D> positions = new();
            int palette = 0;
            for (int star = 0; star < STARS; star++)
            {
                if (palette == Palette.Length)
                    palette = 0;
                Color color = Palette[palette];
                Point3D Position;
                if (star < (STARS / 2) + 1)
                {
                    Position.X = (Rand.NextDouble() * LINEAR_RANGE) - (LINEAR_RANGE / 2);
                    Position.Y = (Rand.NextDouble() * LINEAR_RANGE) - (LINEAR_RANGE / 2);
                    Position.Z = Rand.NextDouble() * -DEPTH;
                    positions.Add(Position);
                }
                else
                {
                    Position = positions[(star - 1) - (int)(STARS / 2)];
                    Position.Z += DEPTH / 2;
                }
                SpaceScene.Children.Add(GenerateBillboard(Position, color));
                palette++;
            }
        }

        private ModelVisual3D GenerateBillboard(Point3D Position, Color color)
        {
            return new ModelVisual3D()
            {
                Content = new GeometryModel3D()
                {
                    Geometry = new MeshGeometry3D()
                    {
                        Normals = Normals,
                        Positions = Positions,
                        TextureCoordinates = new() { new Point(0, 0), new Point(1, 1) },
                        TriangleIndices = Triangles
                    },
                    Material = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(color)
                    }
                },        
                Transform = new Transform3DGroup()
                {
                    Children = new()
                    {
                        new TranslateTransform3D()
                        {
                            OffsetX = Position.X,
                            OffsetY = Position.Y,
                            OffsetZ = Position.Z
                        },
                    }
                }
            };
        }
    }
}
