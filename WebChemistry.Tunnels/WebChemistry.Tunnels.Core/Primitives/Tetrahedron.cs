/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Linq;
    using QuickGraph;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Core.Pdb;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a tetrahedron, d'oh.
    /// </summary>
    public sealed class Tetrahedron : TriangulationCell3D<Vertex, Tetrahedron>
    {
        /// <summary>
        /// The voronoi center of the tetrahedron.
        /// </summary>
        public Vector3D VoronoiCenter { get; private set; }

        /// <summary>
        /// Geometric center of the tetrahedron.
        /// </summary>
        public Vector3D Center { get; private set; }

        /// <summary>
        /// This is computed from the edge lengths.
        /// </summary>
        public double MaxClearance { get; set; }

        ///// <summary>
        ///// Clearance computed as min from edge clearances.
        ///// </summary>
        //public double Clearance { get; set; }

        /// <summary>
        /// Volume of the tetrahedron. Approximate (vdw spheres approximated by cutting tetrahedrons from the vertices.)
        /// </summary>
        public double Volume { get; set; }
               
        /// <summary>
        /// Depth of the tetrahedron.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Depth of the tetrahedron.
        /// </summary>
        public double DepthLength { get; set; }

        /// <summary>
        /// Tetrahedron is boundary if it has less than 4 neighbors.
        /// </summary>
        public bool IsBoundary { get; set; }

        public int FindAdjacentIndex(Tetrahedron neighbor)
        {
            for (int i = 0; i < 4; i++)
            {
                if (object.ReferenceEquals(Adjacency[i], neighbor)) return i;
            }
            return -1;
        }

        double CheckPlaneDist(int i, int j, int k, int v)
        {
            var vs = Vertices;
            var p = Plane3D.FromPoints(vs[i].Atom.Position, vs[j].Atom.Position, vs[k].Atom.Position);
            var pt = vs[v].Atom.Position;
            var td = p.DistanceTo(pt) - vs[v].Atom.GetTunnelSpecificVdwRadius();
            return td;
            //if (td > d) d = td;
        }

        public void Init()
        {
            var vs = Vertices;
            Center = (0.25 * (vs[0].Atom.Position + vs[1].Atom.Position + vs[2].Atom.Position + vs[3].Atom.Position));
            Volume = CalculateVolume();

            var points = new Vector3D[4];
            for (int i = 0; i < 4; i++) points[i] = vs[i].Atom.Position;

            Vector3D center;
            double radius;

            MathHelper.SphereFromPoints(points, out center, out radius);
            VoronoiCenter = center;
        }

        public bool ContainsPoint(Vector3D point)
        {
            int dim = Vertices.Length;
            Matrix m = new Matrix(dim);

            for (int i = 0; i < dim; i++)
            {
                var p = this.Vertices[i].Position;
                m[i, 0] = p.X;
                m[i, 1] = p.Y;
                m[i, 2] = p.Z;
                m[i, dim - 1] = 1;
            }

            double d0 = m.Determinant();
            double[] d = new double[dim];
            Vector buff = new Vector(dim - 1);

            for (int i = 0; i < dim; i++)
            {
                buff[0] = m[i, 0];
                m[i, 0] = point.X;
                buff[1] = m[i, 1];
                m[i, 1] = point.Y;
                buff[2] = m[i, 2];
                m[i, 2] = point.Z;

                d[i] = m.Determinant();
                for (int j = 0; j < dim - 1; j++)
                {
                    m[i, j] = buff[j];
                }


                if (System.Math.Sign(d0) != System.Math.Sign(d[i])) return false;
            }

            if (System.Math.Abs(d.Sum() - d0) > 0.000001) return false;

            return true;
        }
        
        /// <summary>
        /// Computes: VoronoiCenter, Center, MaxClearance, Volume
        /// </summary>
        public void Update(bool strict, UndirectedGraph<Tetrahedron, Edge> graph)
        {
            double d = -1000.0, td;

            //Action<int, int, int, int> checkPlaneDist = (i, j, k, v) =>
            //    {
            //        var p = Plane.FromPoints(vs[i].Atom.Position, vs[j].Atom.Position, vs[k].Atom.Position);
            //        var pt = vs[v].Atom.Position;
            //        var td = p.DistanceTo(pt) - vs[v].Atom.GetTunnelSpecificVdwRadius();
            //        if (td > d) d = td;
            //    };

            //checkPlaneDist(0, 1, 2, 3);
            //checkPlaneDist(0, 1, 3, 2);
            //checkPlaneDist(0, 2, 3, 1);
            //checkPlaneDist(1, 2, 3, 0);

            //strict = true;
            if (strict)
            {
                var edges = graph.AdjacentEdgeList(this);
                for (int i = 0; i < edges.Count; i++)
                {
                    var cl = edges[i].Clearance;
                    if (cl > d) d = cl;
                }
                //d *= 2;
            }
            else
            {
                var vs = Vertices;

                for (int i = 0; i < 3; i++)
                {
                    double r1 = vs[i].Atom.GetTunnelSpecificVdwRadius();
                    for (int j = i + 1; j < 4; j++)
                    {
                        double r2 = vs[j].Atom.GetTunnelSpecificVdwRadius();
                        if (vs[i] != null && vs[j] != null)
                        {
                            td = vs[i].Position.DistanceTo(vs[j].Position) - r1 - r2;
                            if (td > d) d = td;
                        }
                    }
                }

                //for (int i = 0; i < 4; i++)
                //{
                //    td = 2 * (this.VoronoiCenter.DistanceTo(vs[i].Atom.Position) - vs[i].Atom.GetTunnelSpecificVdwRadius());
                //    if (td > d) d = td;
                //}

                td = CheckPlaneDist(0, 1, 2, 3);
                if (td > d) d = td;
                td = CheckPlaneDist(0, 1, 3, 2);
                if (td > d) d = td;
                td = CheckPlaneDist(0, 2, 3, 1);
                if (td > d) d = td;
                td = CheckPlaneDist(1, 2, 3, 0);
                if (td > d) d = td;
            }

            MaxClearance = d;
        }

        /// <summary>
        /// Checks if the tetrahedron is adjacent with another one.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool IsAdjacent(Tetrahedron t)
        {
            return t.Adjacency.Contains(this);
        }

        /// <summary>
        /// Compares interior radius to MaxClearance.
        /// </summary>
        /// <param name="interiorRadius"></param>
        /// <returns></returns>
        public bool IsInterior(double interiorRadius)
        {
            return MaxClearance < 2 * interiorRadius;
        }

        double PartialVolume(int i)
        {
            var vs = Vertices;
            double r = this.Vertices[i].Atom.GetTunnelSpecificVdwRadius();
            var a = (vs[(i + 1) % 4].Atom.Position - vs[i].Atom.Position).Normalize();
            var b = (vs[(i + 2) % 4].Atom.Position - vs[i].Atom.Position).Normalize();
            var c = (vs[(i + 3) % 4].Atom.Position - vs[i].Atom.Position).Normalize();

            return r * r * r * Math.Abs(Vector3D.DotProduct(a, Vector3D.CrossProduct(b, c))) / 6.0;
        }

        /// <summary>
        /// Calculates the volume of the tetrahedron.
        /// </summary>
        /// <returns></returns>
        double CalculateVolume()
        {
            // var vs = this.Vertices.Select(v => (Vector3D)v.Atom.Position).ToArray();
            var vs = Vertices;
            var a = vs[1].Atom.Position - vs[0].Atom.Position;
            var b = vs[2].Atom.Position - vs[0].Atom.Position;
            var c = vs[3].Atom.Position - vs[0].Atom.Position;

            double total = Math.Abs(Vector3D.DotProduct(a, Vector3D.CrossProduct(b, c))) / 6.0;
            double area = total;
            for (int i = 0; i < 4; i++) area -= PartialVolume(i);
            return area;
        }

        /// <summary>
        /// Determines if a solvent of a given radius can pass this tetrahedron (atoms have VDW radii)
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public bool IsSolventAccessible(double radius)
        {
            return MaxClearance > 2 * radius || Center.DistanceToSquared(VoronoiCenter) > radius * radius;
        }

        /// <summary>
        /// Residues around the tetrahedron.
        /// </summary>
        /// <param name="residues"></param>
        /// <returns></returns>
        public IList<PdbResidue> GetResidues(PdbResidueCollection residues)
        {
            return Vertices.Select(v => residues.FromAtom(v.Atom)).Where(r => r != null).Distinct().OrderBy(r => r).ToArray();
        }
    }
}
