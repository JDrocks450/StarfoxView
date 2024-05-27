using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Diagnostics;

namespace WPF.UI.Extensions.Shapes
{
    public class DiscGeometry3D : RoundMesh3D
    {
        protected override void CalculateGeometry()
        {
            int numberOfSeparators = 4 * n + 4;

            points = new Point3DCollection(numberOfSeparators + 1);
            triangleIndices = new Int32Collection((numberOfSeparators + 1) * 3);

            points.Add(new Point3D(0, 0, 0));
            for (int divider = 0; divider < numberOfSeparators; divider++)
            {
                double alpha = Math.PI / 2 / (n + 1) * divider;
                points.Add(new Point3D(r * Math.Cos(alpha), 0, -1 * r * Math.Sin(alpha)));

                triangleIndices.Add(0);
                triangleIndices.Add(divider + 1);
                triangleIndices.Add(divider == numberOfSeparators - 1 ? 1 : divider + 2);
            }
        }
    }
}