/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */


namespace WebChemistry.Tunnels.Core.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Core;

    public class SurfaceVertex
    {
        public int Id;
        public Vector3D Position;
        public Vector3D Normal;

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            var v = obj as SurfaceVertex;
            if (v == null) return false;
            return v.Position.Equals(this.Position);
        }

        public SurfaceVertex(int id, Vector3D position)
        {
            this.Id = id;
            this.Position = position;
        }
    }

    public class SurfaceTriangle
    {
        public SurfaceVertex A, B, C;

        public SurfaceTriangle(SurfaceVertex x, SurfaceVertex y, SurfaceVertex z)
        {
            A = x;
            B = y;
            C = z;
        }
    }

    public class TriangulatedSurface
    {
        public SurfaceVertex[] Vertices { get; private set; }
        public SurfaceTriangle[] Triangles { get; private set; }

        public void WriteMesh(TextWriter w)
        {
            var vertexCount = Vertices.Length;
            w.WriteLine(vertexCount);

            var culture = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var v in Vertices)
            {
                w.WriteLine(string.Format(culture, "{0:0.0000} {1:0.0000} {2:0.0000}", v.Position.X, v.Position.Y, v.Position.Z));
            }

            w.WriteLine(Triangles.Count());
            foreach (var t in Triangles)
            {
                w.WriteLine(4);
                w.WriteLine(t.A.Id);
                w.WriteLine(t.B.Id);
                w.WriteLine(t.C.Id);
                w.WriteLine(t.A.Id);
            }
        }

        static TriangulatedSurface FromFacets(IEnumerable<Facet> facets)
        {
            var vertices = facets
                .SelectMany(f => f.Vertices)
                .Distinct()
                .Select(v => new SurfaceVertex(v.Atom.Id, v.Position))
                .ToArray();
            var map = vertices.ToDictionary(v => v.Id);
            var tris = facets.Select(f => new SurfaceTriangle(map[f.Vertices[0].Atom.Id], map[f.Vertices[1].Atom.Id], map[f.Vertices[2].Atom.Id])).ToArray();
            return new TriangulatedSurface(vertices, tris);
        }
        
        public static void FromCavity(Cavity c, out TriangulatedSurface inner, out TriangulatedSurface boundary)
        {
            inner = FromFacets(c.Boundary.Where(f => !f.IsBoundary).AsList());
            boundary = FromFacets(c.Boundary.Where(f => f.IsBoundary).AsList());
        }

        public object ToJson()
        {
            var verticies = new double[Vertices.Length * 3];
            var tris = new int[Triangles.Length * 3];

            int o = 0;
            foreach (var v in Vertices)
            {
                verticies[o++] = Math.Round(v.Position.X, 2);
                verticies[o++] = Math.Round(v.Position.Y, 2);
                verticies[o++] = Math.Round(v.Position.Z, 2);
            }

            o = 0;
            foreach (var t in Triangles)
            {
                tris[o++] = t.A.Id;
                tris[o++] = t.B.Id;
                tris[o++] = t.C.Id;
            }

            return new
            {
                Vertices = verticies,
                Triangles = tris
            };
        }

        public TriangulatedSurface(IList<SurfaceVertex> vertices, IList<SurfaceTriangle> triangles)
        {
            Vertices = vertices.ToArray();
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Id = i;
            }
            Triangles = triangles.ToArray();
        }
    }
}
