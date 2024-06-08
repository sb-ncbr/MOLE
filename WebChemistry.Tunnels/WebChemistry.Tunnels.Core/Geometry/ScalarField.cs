/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Geometry
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Math;
    
    public class ScalarFieldGrid : FieldBase
    {
        public double[][][] Values { get; private set; }

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

        public static ScalarFieldGrid MakeRandom(string name, WebChemistry.Framework.Core.IStructure s, double min = -1, double max = 1)
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

        public static ScalarFieldGrid MakeRandom(string name, Vector3D bottomLeft, Vector3D topRight, Vector3D size, double min = -1, double max = 1)
        {
            int sX = (int)Math.Ceiling((topRight.X - bottomLeft.X) / size.X);
            int sY = (int)Math.Ceiling((topRight.Y - bottomLeft.Y) / size.Y);
            int sZ = (int)Math.Ceiling((topRight.Z - bottomLeft.Z) / size.Z);

            var values = new double[sX][][];

            var rnd = new Random();
            Func<double> next = () => min + (max - min) * rnd.NextDouble();

            for (int i = 0; i < sX; i++)
            {
                var rowV = new double[sY][];
                values[i] = rowV;
                for (int j = 0; j < sY; j++)
                {
                    var colV = new double[sZ];
                    rowV[j] = colV;
                    for (int k = 0; k < sZ; k++)
                    {
                        colV[k] = next();
                    }
                }
            }

            return new ScalarFieldGrid(name)
            {
                Values = values,
                Origin = bottomLeft,
                CellSize = size,
                SizeX = sX,
                SizeY = sY,
                SizeZ = sZ
            };
        }

        enum DxParserState
        {
            Init,
            Values
        }

        static CultureInfo Culture = CultureInfo.InvariantCulture;

        static double ParseNumber(string value)
        {
            return Convert.ToDouble(value, Culture);
        }

        static ScalarFieldGrid FromOpenDXInternal(string name, string filename)
        {
            var split = new char[] { ' ' };

            int nX = 0, nY = 0, nZ = 0;
            Vector3D origin = new Vector3D();
            double dX = 0, dY = 0, dZ = 0;            
            double[][][] values = null;
            int numRead = 0;
            int toRead = 0;
            int i = 0, j = 0, k = 0;


            Action<string> onNext = s =>
            {
                numRead++;
                values[i][j][k] = ParseNumber(s);

                k++;
                if (k == nZ)
                {
                    k = 0;
                    j++;
                    if (j == nY)
                    {
                        j = 0;
                        i++;
                    }
                }
            };

            using (var reader = new StreamReader(filename))
            {
                var state = DxParserState.Init;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // comment.
                    if (line.Length > 0 && line[0] == '#') continue;
                    if (state == DxParserState.Init)
                    {
                        if (line.StartsWith("object 1 class gridpositions counts ", StringComparison.Ordinal))
                        {
                            var reg = new Regex("object 1 class gridpositions counts (?<nX>[0-9]*) (?<nY>[0-9]*) (?<nZ>[0-9]*)");
                            var match = reg.Match(line);
                            nX = Convert.ToInt32(match.Groups["nX"].Value);
                            nY = Convert.ToInt32(match.Groups["nY"].Value);
                            nZ = Convert.ToInt32(match.Groups["nZ"].Value);
                        }
                        else if (line.StartsWith("origin ", StringComparison.Ordinal))
                        {
                            var reg = new Regex("origin (?<oX>[^ ]*) (?<oY>[^ ]*) (?<oZ>[^ ]*)");
                            var match = reg.Match(line);
                            origin = new Vector3D(ParseNumber(match.Groups["oX"].Value), ParseNumber(match.Groups["oY"].Value), ParseNumber(match.Groups["oZ"].Value));
                        }
                        else if (line.StartsWith("delta ", StringComparison.Ordinal))
                        {
                            var reg = new Regex("delta (?<dX>[^ ]*) (?<dY>[^ ]*) (?<dZ>[^ ]*)");
                            dX = ParseNumber(reg.Match(line).Groups["dX"].Value);
                            dY = ParseNumber(reg.Match(reader.ReadLine()).Groups["dY"].Value);
                            dZ = ParseNumber(reg.Match(reader.ReadLine()).Groups["dZ"].Value);
                        }
                        else if (line.StartsWith("object 3 class array type ", StringComparison.Ordinal))
                        {
                            toRead = nX * nY * nZ;
                            state = DxParserState.Values;
                            values = new double[nX][][];
                            for (var ii = 0; ii < nX; ii++)
                            {
                                values[ii] = new double[nY][];
                                for (var jj = 0; jj < nY; jj++)
                                {
                                    values[ii][jj] = new double[nZ];
                                }
                            }
                        }
                    }
                    else
                    {
                        if (numRead == toRead) break;
                        var fields = line.Split(split, StringSplitOptions.RemoveEmptyEntries);
                        for (int l = 0; l < fields.Length; l++)
                        {
                            onNext(fields[l]);
                        }
                    }
                }
            }

            return new ScalarFieldGrid(name)
            {
                CellSize = new Vector3D(dX, dY, dZ),
                Origin = origin,
                SizeX = nX,
                SizeY = nY,
                SizeZ = nZ,
                Values = values
            };
        }

        public static ScalarFieldGrid FromOpenDX(string name, string filename)
        {
            try
            {
                return FromOpenDXInternal(name, filename);
            }
            catch
            {
                throw new ArgumentException(string.Format("'{0}' is not in a valid OpenDX format.", filename));
            }
        }

        public override double? Interpolate(TunnelProfile.Node node, TunnelLayer layer)
        {
            return Interpolate(node.Center);
        }

        public override double? Interpolate(Vector3D position)
        {
            position = position - Origin;
            int iX = (int)Math.Floor(position.X / CellSize.X),
                iY = (int)Math.Floor(position.Y / CellSize.Y),
                iZ = (int)Math.Floor(position.Z / CellSize.Z);

            if (iX >= SizeX || iY >= SizeY || iZ >= SizeZ)
            {
                return null;
            }

            double v = 0.0, total = 0.0, w;

            for (int l = 0; l < 8; l++)
            {
                int i = iX + (l & 1), j = iY + ((l >> 1) & 1), k = iZ + ((l >> 2) & 1);
                w = 1.0 / Dist(i * CellSize.X, j * CellSize.Y, k * CellSize.Z, position);
                v += w * Values[i][j][k];
                total += w;
            }

            return v / total;
        }

        private ScalarFieldGrid(string name)
            : base(name)
        {

        }
    }

    public class SurfaceScalarField
    {
        public TriangulatedSurface Surface { get; private set; }
        
        /// <summary>
        /// A scalar for each vertex in Surface.Vertices
        /// </summary>
        public double?[] Values { get; set; }

        public double MinMagnitude { get; private set; }
        public double MaxMagnitude { get; private set; }

        public static SurfaceScalarField MakeRandom(TriangulatedSurface surface, double min = -1, double max = 1)
        {
            var len = surface.Vertices.Length;
            var values = new double?[len];
            var rnd = new Random();
            Func<double> next = () => min + (max - min) * rnd.NextDouble();
            for (int i = 0; i < len; i++)
            {
                values[i] = next();
            }
            return new SurfaceScalarField(surface, values);
        }

        public static SurfaceScalarField FromField(TriangulatedSurface surface, FieldBase field)
        {
            var len = surface.Vertices.Length;
            var values = surface.Vertices.Select(v => field.Interpolate(v.Position)).ToArray();
            return new SurfaceScalarField(surface, values);
        }

        public SurfaceScalarField(TriangulatedSurface surface, double?[] values)
        {
            this.Surface = surface;
            this.Values = values;

            double min = double.MaxValue, max = double.MinValue;
            values.ForEach(v =>
            {
                if (v.HasValue) min = Math.Min(v.Value, min);
                if (v.HasValue) max = Math.Max(v.Value, max);
            });

            if (min == double.MaxValue) min = max = 0.0;

            MinMagnitude = min;
            MaxMagnitude = max;
        }
    }
}
