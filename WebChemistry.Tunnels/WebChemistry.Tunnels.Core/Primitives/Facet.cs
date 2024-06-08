/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents a facet of a tetrahedron.
    /// </summary>
    public class Facet
    {
        /// <summary>
        /// Is the facet boundary?
        /// </summary>
        public bool IsBoundary { get; set; }

        /// <summary>
        /// The tetrahedron corresponding to the facet.
        /// </summary>
        public Tetrahedron Tetrahedron { get; set; }

        /// <summary>
        /// Vertices of the facet.
        /// </summary>
        public Vertex[] Vertices { get; private set; }

        /// <summary>
        /// this is the 4th tetrahedral point. Makes sense for boundary faces only
        /// </summary> 
        public Vertex Pivot { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        private Facet(Tetrahedron t)
        {
            Tetrahedron = t;
        }

        /// <summary>
        /// Creates a facet from points.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="t"></param>
        /// <param name="pivot"></param>
        /// <param name="isBoundary"></param>
        /// <returns></returns>
        static Facet FromPoints(Vertex[] vertices, Tetrahedron t, Vertex pivot = null, bool isBoundary = false)
        {
            pivot = pivot ?? vertices[0];

            var normal = Vector3D.CrossProduct(vertices[2].Position - vertices[1].Position, vertices[0].Position - vertices[1].Position);
            if (Vector3D.DotProduct(normal, pivot.Position) - Vector3D.DotProduct(normal, vertices[0].Position) < 0)
            {
                var s = vertices[2];
                vertices[2] = vertices[1];
                vertices[1] = s;
            }

            return new Facet(t) { Vertices = vertices, IsBoundary = t.IsBoundary, Pivot = pivot ?? vertices[0] };
        }

        /// <summary>
        /// Computes the area of the facet.
        /// </summary>
        /// <returns></returns>
        public double GetArea()
        {
            var vs = this.Vertices.Select(v => (Vector3D)v.Atom.Position).ToArray();

            Func<int, double> partial = i =>
            {
                double r = this.Vertices[i].Atom.GetTunnelSpecificVdwRadius();
                Vector3D[] e = new Vector3D[2];
                int count = 0;
                for (int j = 0; j < 3; j++)
                {
                    if (i != j) e[count++] = (vs[j] - vs[i]).Normalize();
                }

                return 0.5 * Math.Abs(Vector3D.CrossProduct(r * e[0], r * e[1]).Length);
            };

            var a = vs[1] - vs[0];
            var b = vs[2] - vs[0];
            double total = 0.5 * Math.Abs(Vector3D.CrossProduct(a, b).Length);
            double area = total - Enumerable.Range(0, 3).Sum(i => partial(i));
            return area;
        }        

        /// <summary>
        /// Gets boundary facets of the given tetrahedron.
        /// </summary>
        /// <param name="tetrahedron"></param>
        /// <param name="neighbors"></param>
        /// <returns></returns>
        public static IEnumerable<Facet> Boundary(Tetrahedron tetrahedron, IList<Tetrahedron> neighbors, int neighborCount = -1)
        {
            int count = neighborCount == -1 ? neighbors.Count : neighborCount;

            switch (count)
            { 
                case 0:
                    //return new Facet[]
                    {
                        yield return FromPoints(new Vertex[] { tetrahedron.Vertices[0], tetrahedron.Vertices[1], tetrahedron.Vertices[2] }, tetrahedron, tetrahedron.Vertices[3], tetrahedron.IsBoundary);
                        yield return FromPoints(new Vertex[] { tetrahedron.Vertices[0], tetrahedron.Vertices[1], tetrahedron.Vertices[3] }, tetrahedron, tetrahedron.Vertices[2], tetrahedron.IsBoundary);
                        yield return FromPoints(new Vertex[] { tetrahedron.Vertices[0], tetrahedron.Vertices[2], tetrahedron.Vertices[3] }, tetrahedron, tetrahedron.Vertices[1], tetrahedron.IsBoundary);
                        yield return FromPoints(new Vertex[] { tetrahedron.Vertices[1], tetrahedron.Vertices[2], tetrahedron.Vertices[3] }, tetrahedron, tetrahedron.Vertices[0], tetrahedron.IsBoundary);
                    }
                    break;
                case 1:
                    {
                        int i = tetrahedron.FindAdjacentIndex(neighbors[0]);
                        var tip = tetrahedron.Vertices[i];
                        var vs = tetrahedron.Vertices;

                        int i0 = (i + 1) % 4, i1 = (i + 2) % 4, i2 = (i + 3) % 4;

                        //return new Facet[]
                        {
                            yield return FromPoints(new Vertex[] { tip, vs[i0], vs[i1] }, tetrahedron, vs[i2], tetrahedron.IsBoundary);
                            yield return FromPoints(new Vertex[] { tip, vs[i0], vs[i2] }, tetrahedron, vs[i1], tetrahedron.IsBoundary);
                            yield return FromPoints(new Vertex[] { tip, vs[i1], vs[i2] }, tetrahedron, vs[i0], tetrahedron.IsBoundary);
                        }
                        break;
                    }
                case 2:
                    {
                        int i = tetrahedron.FindAdjacentIndex(neighbors[0]);
                        int j = tetrahedron.FindAdjacentIndex(neighbors[1]);
                        int u = -1;
                        int v = -1;

                        for (int k = 0; k < 4; k++)
                        {
                            if (k != i && k != j)
                            {
                                if (u == -1) u = k;
                                else v = k;
                            }
                        }

                        var vs = tetrahedron.Vertices;

                        //return new Facet[]
                        {
                            yield return FromPoints(new Vertex[] { vs[i], vs[j], vs[u] }, tetrahedron, vs[v], tetrahedron.IsBoundary);
                            yield return FromPoints(new Vertex[] { vs[i], vs[j], vs[v] }, tetrahedron, vs[u], tetrahedron.IsBoundary);
                        }
                        break;
                    }
                case 3:
                    {
                        int i = tetrahedron.FindAdjacentIndex(neighbors[0]);
                        int j = tetrahedron.FindAdjacentIndex(neighbors[1]);
                        int k = tetrahedron.FindAdjacentIndex(neighbors[2]);
                        int p;
                        for (p = 0; p < 4; p++) if (p != i && p != j && p != k) break;
                        var vs = tetrahedron.Vertices;

                        //return new Facet[]
                        {
                            yield return FromPoints(new Vertex[] { vs[i], vs[j], vs[k] }, tetrahedron, vs[p], tetrahedron.IsBoundary);
                        }
                        break;
                    }
                default:
                    break;
                    //return Enumerable.Empty<Facet>();
            }
        }
    }
}
