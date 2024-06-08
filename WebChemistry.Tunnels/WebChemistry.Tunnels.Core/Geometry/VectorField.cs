/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */


namespace WebChemistry.Tunnels.Core.Geometry
{
    using System;
    using System.Linq;
    using WebChemistry.Framework.Math;

    public class VectorFieldGrid
    {
        public string Name { get; private set; }

        public double[][][] Values { get; private set; }
        public Vector3D[][][] Field { get; private set; }

        public Vector3D Origin { get; private set; }
        public Vector3D CellSize { get; private set; }
        public int SizeX { get; private set; }
        public int SizeY { get; private set; }
        public int SizeZ { get; private set; }
        
        static double Dist(double x, double y, double z, Vector3D p)
        {
            double dx = p.X - x, dy = p.Y - y, dz = p.Z - z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static VectorFieldGrid MakeRandom(string name, WebChemistry.Framework.Core.IStructure s, double min = -1, double max = 1)
        {
            double maxX, maxY, maxZ, minX, minY, minZ;
            minX = minY = minZ = double.MaxValue;
            maxX = maxY = maxZ = double.MinValue;

            foreach (var a in s.Atoms)
            {
                minX = Math.Min(minX, a.Position.X);
                minY = Math.Min(minY, a.Position.Y);
                minZ = Math.Min(minZ, a.Position.Z);

                maxX = Math.Max(maxX, a.Position.X);
                maxY = Math.Max(maxY, a.Position.Y);
                maxZ = Math.Max(maxZ, a.Position.Z);
            }

            minX -= 1.5; minY -= 1.5; minZ -= 1.5;
            maxX += 1.5; maxY += 1.5; maxZ += 1.5;

            return MakeRandom(name, new Vector3D(minX, minY, minZ), new Vector3D(maxX, maxY, maxZ), new Vector3D(1, 1, 1), min, max);
        }

        public static VectorFieldGrid MakeRandom(string name, Vector3D bottomLeft, Vector3D topRight, Vector3D size, double min = -1, double max = 1)
        {
            int sX = (int)Math.Ceiling((topRight.X - bottomLeft.X) / size.X);
            int sY = (int)Math.Ceiling((topRight.Y - bottomLeft.Y) / size.Y);
            int sZ = (int)Math.Ceiling((topRight.Z - bottomLeft.Z) / size.Z);

            var values = new double[sX][][];
            var field = new Vector3D[sX][][];

            var rnd = new Random();
            Func<double> next = () => min + (max - min) * rnd.NextDouble();

            for (int i = 0; i < sX; i++)
            {
                var rowV = new double[sY][];
                var rowF = new Vector3D[sY][];
                values[i] = rowV;
                field[i] = rowF;
                for (int j = 0; j < sY; j++)
                {
                    var colV = new double[sZ];
                    var colF = new Vector3D[sZ];
                    rowV[j] = colV;
                    rowF[j] = colF;
                    for (int k = 0; k < sZ; k++)
                    {
                        colV[k] = next();
                        colF[k] = new Vector3D(next(), next(), next()).Normalize();
                    }
                }
            }

            return new VectorFieldGrid
            {
                Name = name,
                Values = values,
                Field = field,
                Origin = bottomLeft,
                CellSize = size,
                SizeX = sX,
                SizeY = sY,
                SizeZ = sZ
            };
        }

        public void Interpolate(Vector3D position, out double? value, out Vector3D field)
        {
            position = position - Origin;
            int iX = (int)Math.Floor(position.X / CellSize.X),
                iY = (int)Math.Floor(position.Y / CellSize.Y),
                iZ = (int)Math.Floor(position.Z / CellSize.Z);

            if (iX >= SizeX || iY >= SizeY || iZ >= SizeZ)
            {
                value = null;
                field = new Vector3D();
                return;
            }

            Vector3D f = new Vector3D();
            double v = 0.0, total = 0.0, w;

            for (int l = 0; l < 8; l++)
            {
                int i = iX + (l & 1), j = iY + ((l >> 1) & 1), k = iZ + ((l >> 2) & 1);
                w = 1.0 / Dist(i * CellSize.X, j * CellSize.Y, k * CellSize.Z, position);
                v += w * Values[i][j][k];
                f += w * Field[i][j][k];
                total += w;
            }

            value = v / total;
            field = f / total;
        }
    }

    public class SurfaceVectorField
    {
        public TriangulatedSurface Surface { get; private set; }

        /// <summary>
        /// A normalized vector for each vertex in Surface.Vertices
        /// </summary>
        public Vector3D[] Field { get; private set; }

        /// <summary>
        /// A scalar for each vertex in Surface.Vertices
        /// </summary>
        public double?[] Values { get; set; }

        public double MinMagnitude { get; private set; }
        public double MaxMagnitude { get; private set; }

        public static SurfaceVectorField MakeRandom(TriangulatedSurface surface, double min = -1, double max = 1)
        {
            var len = surface.Vertices.Length;
            var field = new Vector3D[len];
            var values = new double?[len];
            var rnd = new Random();
            Func<double> next = () => min + (max - min) * rnd.NextDouble();
            for (int i = 0; i < len; i++)
            {
                values[i] = next();
                field[i] = new Vector3D(next(), next(), next()).Normalize();
            }
            return new SurfaceVectorField(surface, field, values);
        }

        public static SurfaceVectorField FromField(TriangulatedSurface surface, VectorFieldGrid grid)
        {
            var len = surface.Vertices.Length;
            var field = new Vector3D[len];
            var values = new double?[len];
            for (int i = 0; i < len; i++)
            {
                double? val;
                Vector3D f;
                grid.Interpolate(surface.Vertices[i].Position, out val, out f);
                field[i] = f;
                values[i] = val;
            }

            return new SurfaceVectorField(surface, field, values);
        }

        public SurfaceVectorField(TriangulatedSurface surface, Vector3D[] field, double?[] values)
        {
            this.Surface = surface;
            this.Field = field;
            this.Values = values;

            double min = double.MaxValue, max = double.MinValue;
            values.ForEach(v =>
            {
                if (v.HasValue) min = Math.Min(v.Value, min);
                if (v.HasValue) max = Math.Max(v.Value, max);
            });
            MinMagnitude = min;
            MaxMagnitude = max;
        }
    }
}
