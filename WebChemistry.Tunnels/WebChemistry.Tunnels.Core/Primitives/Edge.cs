/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;
    using QuickGraph;
    
    /// <summary>
    /// Edge in a Voronoi graph.
    /// </summary>
    public class Edge : VoronoiEdge3D<Vertex, Tetrahedron>, IUndirectedEdge<Tetrahedron>, IEdge<Tetrahedron>
    {
        /// <summary>
        /// Edge weight used for finding paths in the molecule.
        /// Computed as length / (clearance + 0.000001)
        /// </summary>
        public double Weight
        {
            get;
            private set;
        }

        double? voronoiWeight;
        public double VoronoiWeight
        {
            get
            {
                if (voronoiWeight.HasValue) return voronoiWeight.Value;
                var w = Weight;
                w *= Source.VoronoiCenter.DistanceToSquared(Source.Center) + Target.VoronoiCenter.DistanceToSquared(Target.Center);
                voronoiWeight = w;
                return w;
            }
        }

        public double Length
        {
            get;
            private set;
        }

        public double Clearance
        {
            get;
            private set;
        }

        public Tetrahedron Other(Tetrahedron v)
        {
            if (object.ReferenceEquals(v, this.Source)) return this.Target;
            return this.Source;
        }

        /// <summary>
        /// Updates the edge (= computes clearance and weight)
        /// </summary>
        public void Update(Complex complex)
        {
            //var vert = this.Source.Vertices.Intersect(this.Target.Vertices).ToArray();
            var line = Line3D.Create(Source.VoronoiCenter, Target.VoronoiCenter);

            int pi = this.Source.FindAdjacentIndex(this.Target);
            double clearance = double.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                if (i == pi) continue;

                var v = Source.Vertices[i];
                var d = line.DistanceTo(v.Atom.Position) - v.Atom.GetTunnelSpecificVdwRadius();
                if (d < clearance) clearance = d;
            }

           // var center = line.Origin + (0.5 * (Target.VoronoiCenter - Source.VoronoiCenter));
            // var clearance = 2 * complex.KdTree.Nearest(center, 5).Select(a => a.Position.DistanceTo(center) - a.GetTunnelSpecificVdwRadius()).Min();

            //   Clearance = vert.Min(v => line.DistanceTo(v.Atom.Position) - v.Atom.GetTunnelSpecificVdwRadius());
            double len = Source.VoronoiCenter.DistanceTo(Target.VoronoiCenter);

            if (clearance < 0)
            {
                clearance = 0;
                Weight = 100000000;
            }
            else
            {                
                Weight = len / (clearance * clearance + 0.000001);
            }
            this.Length = len;
            this.Clearance = clearance;
        }

        /// <summary>
        /// 
        /// </summary>
        public Edge()
        {

        }
    }
}
