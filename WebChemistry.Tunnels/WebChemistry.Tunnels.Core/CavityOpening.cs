/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using QuickGraph;
    using QuickGraph.Algorithms;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Represents an exit from the protein.
    /// </summary>
    public class CavityOpening
    {
        public Tetrahedron Pivot { get; private set; }
        public bool IsUser { get; set; }

        void Cover(IEnumerable<Tetrahedron> faces)
        {

        }

        public CavityOpening(IEnumerable<Tetrahedron> faces)
        {
            this.Pivot = faces.MaxBy(f => f.MaxClearance)[0];
        }

        public CavityOpening(Tetrahedron pivot)
        {
            this.Pivot = pivot;
        }

        static bool Common(Tetrahedron a, Tetrahedron b)
        {
            int cv = 0;
            for (int i = 0; i < 4; i++)
            {
                var v = a.Vertices[i];
                for (int j = 0; j < 4; j++)
                {
                    if (object.ReferenceEquals(v, b.Vertices[j]))
                    {
                        ++cv;
                        break;
                    }
                }
                if (cv > 1) return true;
            }

            return false;
        }

        /// <summary>
        /// Creates cavity openings from the give tetrahedra.
        /// </summary>
        /// <param name="boundary"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        internal static void Create(IEnumerable<Tetrahedron> boundary, double coverRadius, out IEnumerable<CavityOpening> covering, out IEnumerable<CavityOpening> poreExits)
        {
            var candidates = boundary.ToArray(); //boundary.Where(f => f.MaxClearance > threshold).ToArray();

            UndirectedGraph<Tetrahedron, UndirectedEdge<Tetrahedron>> graph = new UndirectedGraph<Tetrahedron, UndirectedEdge<Tetrahedron>>(false);
            graph.AddVertexRange(candidates);

            
            for (int i = 0; i < candidates.Length - 1; i++)
            {
                for (int j = i + 1; j < candidates.Length; j++)
                {
                    //candidates[i].Vertices.Intersect(candidates[j].Vertices).Count() > 1
                    if (Common(candidates[i], candidates[j]) 
                        /* candidates[i].FindAdjacentIndex(candidates[j]) != -1*/)
                    {
                        graph.AddEdge(new UndirectedEdge<Tetrahedron>(candidates[i], candidates[j]));
                    }
                }
            } 

            Dictionary<Tetrahedron, int> comps = new Dictionary<Tetrahedron, int>();
            var ic = graph.ConnectedComponents(comps);

            List<CavityOpening> openings = new List<CavityOpening>();
            List<CavityOpening> poreExitList = new List<CavityOpening>();

            for (int i = 0; i < ic; i++)
            {
                var component = candidates.Where(c => comps[c] == i).ToList();
                //var mainPivot = component.MaxBy(f => f.MaxClearance);

                poreExitList.Add(new CavityOpening(component.MaxBy(t => t.Volume)[0]));

                List<Tetrahedron> pivots = new List<Tetrahedron>();

                Cover(component, coverRadius, pivots);
                foreach (var p in pivots) openings.Add(new CavityOpening(p));
                
                //openings.Add(new CavityOpening(candidates.Where(c => comps[c] == i)));
            }

            covering = openings;
            poreExits = poreExitList;
        }

        static void Cover(List<Tetrahedron> component, double radius, List<Tetrahedron> pivots)
        {
            var pivot = component.MaxBy(f => f.MaxClearance)[0];
            pivots.Add(pivot);

            var uncovered = component.Where(t => t.Center.DistanceTo(pivot.Center) > radius).ToList();
            if (uncovered.Count > 0) Cover(uncovered, radius, pivots);
        }

        public override int GetHashCode()
        {
            return Pivot.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as CavityOpening;
            if (other == null) return false;
            return object.ReferenceEquals(other.Pivot, this.Pivot);
        }
    }
}
