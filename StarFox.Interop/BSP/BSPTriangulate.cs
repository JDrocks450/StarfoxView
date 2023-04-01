using EarClipperLib;
using StarFox.Interop.BSP.SHAPE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP
{
    public static class BSPTriangulate
    {
        public static bool EarClipTriangulationAlgorithm(IEnumerable<BSPPoint> FacePoints, BSPVec3 Normal, out List<BSPPoint> NewVerts)
        {
            Vector3m Normalize(BSPVec3 vector)
            {
                double length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
                return new Vector3m(vector.X / length, vector.Y / length, vector.Z / length);
            }
            var verticies = FacePoints.Select(x => new Vector3m(x.X, x.Y, x.Z)).ToList();
            EarClipping converter = new();
            converter.SetPoints(verticies,null,Normalize(Normal));
            NewVerts = new();
            try
            {
                converter.Triangulate();
                var result = converter.Result;                
                foreach (var point in result)
                {
                    foreach (var bspPoint in FacePoints)
                    {
                        if (point.X == bspPoint.X &&
                            point.Y == bspPoint.Y &&
                            point.Z == bspPoint.Z)
                        {
                            NewVerts.Add(bspPoint);
                            continue;
                        }
                    }
                }
                return NewVerts.Count % 3 == 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static List<int> TriangulateVertices(IEnumerable<BSPPoint> FacePoints)
        {
            var vertices = FacePoints.Select(x => new Vector3()
            {
                X = x.X,
                Y = x.Y,
                Z = x.Z
            }).ToList();
            List<int> indices = new List<int>();
            int n = vertices.Count;
            if (n < 3) return indices;

            int[] V = new int[n];
            if (Area(vertices) > 0)
            {
                for (int v = 0; v < n; v++) V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++) V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                {
                    // INFINITE LOOP!
                    break;
                }

                int u = v;
                if (nv <= u) u = 0;
                v = u + 1;
                if (nv <= v) v = 0;
                int w = v + 1;
                if (nv <= w) w = 0;

                if (Snip(vertices, u, v, w, nv, V))
                {
                    int a, b, c, s, t;

                    a = V[u]; b = V[v]; c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    m++;

                    for (s = v, t = v + 1; t < nv; s++, t++)
                    {
                        V[s] = V[t];
                    }
                    nv--;

                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices;
        }

        private static float Area(List<Vector3> vertices)
        {
            int n = vertices.Count;
            float A = 0.0f;

            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector3 pval = vertices[p];
                Vector3 qval = vertices[q];
                A += pval.X * qval.Y - qval.X * pval.Y;
            }

            return A * 0.5f;
        }

        private static bool Snip(List<Vector3> vertices, int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector3 A = vertices[V[u]];
            Vector3 B = vertices[V[v]];
            Vector3 C = vertices[V[w]];

            if (float.Epsilon > (((B.X - A.X) * (C.Y - A.Y)) - ((B.Y - A.Y) * (C.X - A.Y)))) return false;

            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w)) continue;
                Vector3 P = vertices[V[p]];
                if (PointInTriangle(A, B, C, P)) return false;
            }

            return true;

        }

        private static bool PointInTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P)
        {
            float ax = cross(B - C, P - C);
            float ay = cross(C - A, P - A);
            float az = cross(A - B, P - B);

            return sign(ax) == sign(ay) && sign(ay) == sign(az);
        }

        private static float cross(Vector3 a, Vector3 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private static bool sign(float f)
        {
            return f >= 0;
        }
    }
}
