using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.UI.Extensions.Shapes
{
    public abstract class RoundMesh3D
    {
        protected int n = 10;
        protected int r = 20;
        protected Point3DCollection points;
        protected Int32Collection triangleIndices;

        public virtual int Radius
        {
            get { return r; }
            set { r = value; CalculateGeometry(); }
        }

        public virtual int Separators
        {
            get { return n; }
            set { n = value; CalculateGeometry(); }
        }

        public Point3DCollection Points
        {
            get { return points; }
        }

        public Int32Collection TriangleIndices
        {
            get { return triangleIndices; }
        }

        protected abstract void CalculateGeometry();
    }
}
