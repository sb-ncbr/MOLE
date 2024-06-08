/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using QuickGraph;
    using QuickGraph.Algorithms;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;
    using System.Xml.Linq;
    using System.Globalization;
    using WebChemistry.Framework.Core.Pdb;
    
    /// <summary>
    /// Type of the cavity
    /// </summary>
    public enum CavityType
    {
        Cavity,
        Void,
        MolecularSurface
    }

    /// <summary>
    /// This class represents empty space inside a molecule.
    /// </summary>
    public class Cavity : InteractiveObject
    {
        Tetrahedron[] boundaryTetrahedrons;
        bool isSurface;
        HashSet<PdbResidue> boundaryResidueSet;

        /// <summary>
        /// Gets the Id of the cavity.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Type of the cavity.
        /// </summary>
        public CavityType Type { get; private set; }

        /// <summary>
        /// Gets the complex the cavity belongs  to.
        /// </summary>
        public Complex Complex { get; private set; }
        
        /// <summary>
        /// Gets the tetrahedrons of the cavity.
        /// </summary>
        public HashSet<Tetrahedron> Tetrahedrons { get; private set; }

        /// <summary>
        /// Gets the openings of the cavity.
        /// </summary>
        public ObservableCollection<CavityOpening> Openings { get; private set; }

        /// <summary>
        /// Gets the pore exits of the cavity (largest tetrahedron for each boundary component).
        /// </summary>
        public ObservableCollection<CavityOpening> PoreExits { get; private set; }

        /// <summary>
        /// Gets the boundary facets of the cavity.
        /// </summary>
        public IEnumerable<Facet> Boundary { get; private set; }

        /// <summary>
        /// Residues at the cavity boundary.
        /// </summary>
        public IEnumerable<PdbResidue> BoundaryResidues { get; private set; }
        
        /// <summary>
        /// Residues that are not boundary.
        /// </summary>
        public IEnumerable<PdbResidue> InnerResidues { get; private set; }

        /// <summary>
        /// Properties of the boundary residues.
        /// </summary>
        public TunnelPhysicoChemicalProperties BoundaryProperties { get; private set; }

        /// <summary>
        /// Properties of the inner residues.
        /// </summary>
        public TunnelPhysicoChemicalProperties InnerProperties { get; private set; }

        /// <summary>
        /// Properties of all residues.
        /// </summary>
        public TunnelPhysicoChemicalProperties GlobalProperties { get; private set; }

        /// <summary>
        /// Gets the cavity graph. The graphs is a voronoi mesh of the tetrahedrons.
        /// </summary>
        public UndirectedGraph<Tetrahedron, Edge> CavityGraph { get; private set; }

        ///// <summary>
        ///// KdTree of tetrahedron geometrical centers.
        ///// </summary>
        //public K3DTree<Tetrahedron> KdTetra { get; private set; }

        /// <summary>
        /// Volume of the cavity. Approximated by subtracting 4 tetrahedrons defined by
        /// the VDW radius of atoms in each of the tetrahedron's vertices.
        /// </summary>
        public double Volume { get; private set; }  
      
        /// <summary>
        /// The depth of the cavity. If the cavity does not touch the surface, the depth
        /// is set to int.MaxValue.
        /// </summary>
        public int Depth { get; private set; }

        /// <summary>
        /// The depth length of the cavity.
        /// </summary>
        public double DepthLength { get; private set; }

        /// <summary>
        /// Finds a shortest path between 2 tetrahedrons.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        internal IEnumerable<Edge> FindPath(Tetrahedron from, Tetrahedron to)
        {
            if (!CavityGraph.ContainsVertex(from) || !CavityGraph.ContainsVertex(to)) return null;

            var wf = Complex.MakeEdgeWeightFunction();
            var paths = CavityGraph.ShortestPathsDijkstra(wf, from);

            IEnumerable<Edge> path;

            if (paths(to, out path)) return path;

            return null;
        }

        internal TunnelOrigin GetOrigin(Vector3D point, double radius, TunnelOriginType type = TunnelOriginType.User)
        {
            //var t = KdTetra.Nearest(point, 10, radius).Where(p => p.VoronoiCenter.DistanceTo(point) < radius).FirstOrDefault();

            double minDist = double.MaxValue;
            Tetrahedron min = null;
            foreach (var t in CavityGraph.Vertices)
            {
                if (t.Depth < 5) continue;
                var d = t.VoronoiCenter.DistanceToSquared(point);
                if (d < minDist)
                {
                    min = t;
                    minDist = d;
                }
            }

            if (minDist > radius * radius) return null;

            return new TunnelOrigin(min, Complex, type);
        }

        internal CavityOpening GetOpening(Vector3D point, double radius)
        {
            //var t = KdTetra.Nearest(point, 10, radius).Where(p => p.VoronoiCenter.DistanceTo(point) < radius).FirstOrDefault();

            double minDist = double.MaxValue;
            Tetrahedron min = null;
            foreach (var t in Boundary)
            {
                var d = t.Tetrahedron.Center.DistanceToSquared(point);
                if (d < minDist)
                {
                    min = t.Tetrahedron;
                    minDist = d;
                }
            }

            if (minDist > radius * radius) return null;

            return new CavityOpening(min);
        }

        public Tetrahedron GetTetrahedron(Vector3D point, double radius)
        {
            //var t = KdTetra.Nearest(point, 10, radius).Where(p => p.VoronoiCenter.DistanceTo(point) < radius).FirstOrDefault();

            double minDist = double.MaxValue;
            Tetrahedron min = null;
            foreach (var t in CavityGraph.Vertices)
            {
                //if (t.Depth < 5) continue;
                var d = t.Center.DistanceToSquared(point);
                if (d < minDist)
                {
                    min = t;
                    minDist = d;
                }
            }

            if (minDist > radius * radius) return null;

            return min;
        }

        public bool HasBoundaryVertex(Tetrahedron t)
        {
            var rs = Complex.Structure.PdbResidues();
            for (int i = 0; i < 4; i++)
            {
                var r = rs.FromAtom(t.Vertices[i].Atom);
                if (boundaryResidueSet.Contains(r)) return true;
            }
            return false;
        }

        void CalculateProperties()
        {
            BoundaryProperties = PhysicoChemicalPropertyCalculation.CalculateResidueProperties(BoundaryResidues.AsList());
            InnerProperties = PhysicoChemicalPropertyCalculation.CalculateResidueProperties(InnerResidues.AsList());
            GlobalProperties = PhysicoChemicalPropertyCalculation.CalculateResidueProperties(BoundaryResidues.Concat(InnerResidues).ToArray());
        }

        /// <summary>
        /// Creates a standard cavity.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="complex"></param>
        /// <returns></returns>
        internal static Cavity Create(UndirectedGraph<Tetrahedron, Edge> graph, Complex complex)
        {
            var boundaryTetra = graph.Vertices.Where(v => v.IsBoundary).ToArray();
 
            List<Facet> boundary = new List<Facet>();
            Tetrahedron[] nb = new Tetrahedron[3];

            foreach (var v in graph.Vertices)
            {
                var edges = graph.AdjacentEdgeList(v);
                if (edges.Count >= 4) continue;
                for (int i = 0; i < edges.Count; i++) nb[i] = edges[i].Other(v);
                foreach (var facet in Facet.Boundary(v, nb, edges.Count))
                {
                    boundary.Add(facet);
                }
            }

            IEnumerable<CavityOpening> cover, poreExits;
            CavityOpening.Create(boundaryTetra, complex.Parameters.SurfaceCoverRadius, out cover, out poreExits);

            var residues = complex.Structure.PdbResidues();
            var boundaryResidues = boundary
                .Where(f => f.IsBoundary)
                .SelectMany(b => b.Vertices)
                .Select(v => residues.FromAtom(v.Atom))
                .ToHashSet();

            var innerResidues = graph.Vertices
                .SelectMany(b => b.Vertices)
                .Select(v => residues.FromAtom(v.Atom))
                .Where(r => !boundaryResidues.Contains(r))
                .ToHashSet();

            var cavity = new Cavity
            {
                CavityGraph = graph,
                Type = boundaryResidues.Count == 0 ? CavityType.Void : CavityType.Cavity,
                Tetrahedrons = new HashSet<Tetrahedron>(graph.Vertices),
                Complex = complex,
                Depth = graph.Vertices.Max(v => v.Depth),
                DepthLength = graph.Vertices.Max(v => v.DepthLength),

                //KdTetra = new K3DTree<Tetrahedron>(graph.Vertices, f => f.VoronoiCenter),
                Boundary = boundary, //graph.Vertices.SelectMany(v => Facet.Boundary(v, graph.AdjacentEdges(v).Select(e => e.Other(v)).ToArray())).ToArray(),
                BoundaryResidues = boundaryResidues.OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).ToArray(),
                InnerResidues = innerResidues.OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).ToArray(),

                boundaryTetrahedrons = boundaryTetra,
                boundaryResidueSet = boundaryResidues,
                Openings = new ObservableCollection<CavityOpening>(cover.ToList()),
                PoreExits = new ObservableCollection<CavityOpening>(poreExits.ToList()),

                Volume = graph.Vertices.Sum(f => f.Volume)
            };
            cavity.CalculateProperties();
            return cavity;
        }

        /// <summary>
        /// Creates a cavity from the entire molecule.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="complex"></param>
        /// <returns></returns>
        internal static Cavity CreateSurface(UndirectedGraph<Tetrahedron, Edge> graph, Complex complex)
        {
            List<Facet> boundary = new List<Facet>();
            Tetrahedron[] nb = new Tetrahedron[3];

            foreach (var v in graph.Vertices)
            {
                var edges = graph.AdjacentEdgeList(v);
                if (edges.Count >= 4) continue;
                for (int i = 0; i < edges.Count; i++) nb[i] = edges[i].Other(v);
                foreach (var facet in Facet.Boundary(v, nb, edges.Count))
                {
                    boundary.Add(facet);
                }
            }

            var residues = complex.Structure.PdbResidues();
            var boundaryResidues = boundary
                .Where(f => f.IsBoundary)
                .SelectMany(b => b.Vertices)
                .Select(v => residues.FromAtom(v.Atom))
                .ToHashSet();

            var innerResidues = graph.Vertices
                .SelectMany(b => b.Vertices)
                .Select(v => residues.FromAtom(v.Atom))
                .Where(r => !boundaryResidues.Contains(r))
                .ToHashSet();
                
            var cavity = new Cavity
            {
                Id = 0,
                Type = CavityType.MolecularSurface,
                Tetrahedrons = new HashSet<Tetrahedron>(graph.Vertices),
                isSurface = true,
                CavityGraph = graph,
                Complex = complex,
                Depth = int.MaxValue,
                DepthLength = double.MaxValue,

                //KdTetra = new K3DTree<Tetrahedron>(graph.Vertices, f => f.VoronoiCenter),
                Boundary = boundary, //graph.Vertices.SelectMany(v => Facet.Boundary(v, graph.AdjacentEdges(v).Select(e => e.Other(v)).ToArray())).ToArray(),
                boundaryResidueSet = boundaryResidues,
                BoundaryResidues = boundaryResidues.OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).ToArray(),
                InnerResidues = innerResidues.OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).ToArray(),

                boundaryTetrahedrons = null,
                Openings = new ObservableCollection<CavityOpening>(),
                PoreExits = new ObservableCollection<CavityOpening>(),

                Volume = graph.Vertices.Sum(f => f.Volume)
            };
            cavity.CalculateProperties();
            return cavity;
        }

        /// <summary>
        /// Adds a custom opening.
        /// </summary>
        /// <param name="o"></param>
        public void AddOpening(CavityOpening o)
        {
            Openings.Add(o);
        }

        /// <summary>
        /// Add user opening as both opening and pore exit.
        /// </summary>
        /// <param name="o"></param>
        public void AddUserOpening(CavityOpening o)
        {
            o.IsUser = true;
            Openings.Add(o);
            PoreExits.Add(o);
        }

        /// <summary>
        /// Removes a custom opening.
        /// </summary>
        /// <param name="o"></param>
        public void RemoveOpening(CavityOpening o)
        {
            Openings.Remove(o);
            PoreExits.Remove(o);
        }

        /// <summary>
        /// Updates openings.
        /// </summary>
        internal void UpdateOpenings()
        {
            if (isSurface) return;

            Openings.Clear();
            PoreExits.Clear();

            IEnumerable<CavityOpening> cover, poreExits;

            CavityOpening.Create(boundaryTetrahedrons, Complex.Parameters.SurfaceCoverRadius, out cover, out poreExits);

            cover.ForEach(o => Openings.Add(o));
            poreExits.ForEach(o => PoreExits.Add(o));
        }

        public XElement ToXml()
        {
            var boundaryResidues = string.Join(",", BoundaryResidues.Select(r => r.ToString()).ToArray());
            var innerResidues = string.Join(",", InnerResidues.Select(r => r.ToString()).ToArray());

            var ret = new XElement("Cavity",
                new XAttribute("Type", Type.ToString()),
                new XAttribute("Volume", Volume.ToString("0.000", CultureInfo.InvariantCulture)),
                new XAttribute("Depth", Depth),
                new XAttribute("DepthLength", DepthLength),
                new XAttribute("Id", Id),
                new XElement("Boundary", 
                    new XElement("Residues", boundaryResidues),
                    BoundaryProperties.ToXml()),
                new XElement("Inner", 
                    new XElement("Residues", innerResidues),
                    InnerProperties.ToXml()),
                GlobalProperties.ToXml());
            
            return ret;
        }

        public object ToJson()
        {
            Geometry.TriangulatedSurface inner, boundary;
            Geometry.TriangulatedSurface.FromCavity(this, out inner, out boundary);


            return new
            {
                Type = Type.ToString(),
                Volume = System.Math.Round(Volume, 3),
                Depth = Depth,
                DepthLength = DepthLength,
                Id = Id,
                Boundary = new {
                    Residues = BoundaryResidues.Select(r => r.ToString()).ToArray(),
                    Properties = BoundaryProperties.ToJson()
                },
                Inner = new
                {
                    Residues = InnerResidues.Select(r => r.ToString()).ToArray(),
                    Properties = InnerProperties.ToJson()
                },
                Properties = GlobalProperties.ToJson(),
                Mesh = new {
                    Inner = inner.ToJson(),
                    Boundary = boundary.ToJson(),
                }
            };
        }

        private Cavity()
        {
        }
    }
}
