/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using QuickGraph;
    using QuickGraph.Collections;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;

    public partial class Complex
    {
        UndirectedGraph<Tetrahedron, Edge> cavityGraph;
        Vertex[] pivots;

        internal static IEnumerable<IEnumerable<Tetrahedron>> GetComponents(HashSet<Tetrahedron> data)
        {
            var hc = data;
            var all = hc.ToArray();
            ForestDisjointSet<Tetrahedron> tau = new ForestDisjointSet<Tetrahedron>(hc.Count);

            foreach (var sigma in all)
            {
                tau.MakeSet(sigma);

                //foreach (Tetrahedron t in sigma.AdjacentFaces.Where(f => hc.Contains(f)))
                for (int i = 0; i < 4; i++)
                {
                    var t = sigma.Adjacency[i];
                    if (!hc.Contains(t)) continue;

                    if (t == null) continue;

                    if (tau.Contains(t))
                    {
                        tau.Union(t, sigma);
                    }
                }
            }

            Dictionary<Tetrahedron, List<Tetrahedron>> components = new Dictionary<Tetrahedron, List<Tetrahedron>>();
            
            for (int i = 0; i < tau.ElementCount; i++)
            {
                var x = all[i];
                var key = tau.FindSet(x);
                List<Tetrahedron> component;

                if (components.TryGetValue(key, out component)) component.Add(x);
                else components[key] = new List<Tetrahedron>() { x };
            }

            return components.Values;
        }

        UndirectedGraph<Tetrahedron, Edge> ToGraph(HashSet<Tetrahedron> vertices)
        {
            var graph = new UndirectedGraph<Tetrahedron, Edge>(false);
            graph.AddVertexRange(vertices);
            foreach (var v in vertices) graph.AddEdgeRange(this.cavityGraph.AdjacentEdges(v).Where(e => vertices.Contains(e.Other(v))));
            return graph;
        }

        HashSet<Tetrahedron> RemoveOutsideAndInterior(out Cavity surfaceCavity)
        {
            HashSet<Tetrahedron> depressionFaces = new HashSet<Tetrahedron>();

            foreach (var t in Triangulation.Vertices.Where(v => v.IsBoundary))
            {
                depressionFaces.Add(t);
                cavityGraph.RemoveVertex(t);
            }
            
            var snapshot = depressionFaces.ToList();

            while (snapshot.Count > 0)
            {
                var layer = new List<Tetrahedron>();

                for (int i = 0; i < snapshot.Count; i++)
                {
                    var face = snapshot[i];

                    for (int j = 0; j < 4; j++)
                    {
                        var of = face.Adjacency[j];
                        if (of == null) continue;

                        if (of.IsSolventAccessible(Parameters.ProbeRadius))
                        {
                            if (depressionFaces.Add(of))
                            {
                                cavityGraph.RemoveVertex(of);
                                layer.Add(of);
                            }
                        }
                        of.IsBoundary = true;
                    }
                }

                //if (snapshot.Length == depressionFaces.Count) break;
                snapshot = layer;
            }

            //cavityGraph.RemoveVertexIf(v => depressionFaces.Contains(v));

            //HashSet<Tetrahedron> cavityGraphVertices = new HashSet<Tetrahedron>();
            //foreach (var cell in Triangulation.Cells)
            //{
            //    if (/*!cell.IsInterior(Parameters.InteriorThreshold) && */!depressionFaces.Contains(cell)) cavityGraphVertices.Add(cell);
            //}
            //cavityGraph = ToGraph(cavityGraphVertices);

            var surfaceGraph = new UndirectedGraph<Tetrahedron, Edge>();
            surfaceGraph.AddVerticesAndEdgeRange(cavityGraph.Edges);
            surfaceCavity = Cavity.CreateSurface(surfaceGraph, this);

            //surface = cavityGraph.Vertices.SelectMany(v => Facet.Boundary(v, cavityGraph.AdjacentVertices(v).ToArray())).ToArray();

            // do "fan-out"
            //var interior = cavityGraph.Vertices.Where(v => v.IsInterior(parameters.InteriorThreshold)).ToArray();
            //var finalInterior = interior.ToHashSet();
            //var perVertex = data.ToDictionary(v => v, _ => new List<Tetrahedron>());
            //cavityGraph.Vertices.Run(t => t.Vertices.Run(v => perVertex[v].Add(t)));

            //cavityGraph.Vertices
            //    .Where(v => !interior.Contains(v))
            //    .SelectMany(t => t.Vertices).ToHashSet()
            //    .Run(v => perVertex[v].Run(s => finalInterior.Remove(s)));

            //cavityGraph.RemoveVertexIf(v => finalInterior.Contains(v));

            cavityGraph.RemoveVertexIf(v => v.IsInterior(Parameters.InteriorThreshold));

            return depressionFaces;
        }

        void Bfs(Tetrahedron start)
        {
           // HashSet<Tetrahedron> visited = new HashSet<Tetrahedron>();
            var q = new System.Collections.Generic.Queue<Tetrahedron>();
            q.Enqueue(start);

            //visited.Add(start);

            while (q.Count > 0)
            {
                var c = q.Dequeue();

                var list = cavityGraph.AdjacentEdgeList(c);
                //foreach (var e in cavityGraph.AdjacentEdges(c))
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i].Other(c);
                    int nd = Math.Min(c.Depth + 1, v.Depth);

                    if (nd < v.Depth)
                    {
                        v.Depth = nd;
                        q.Enqueue(v);
                    }
                }
            }
        }


        void BfsL(Tetrahedron start)
        {
            // HashSet<Tetrahedron> visited = new HashSet<Tetrahedron>();
            var q = new System.Collections.Generic.Queue<Tetrahedron>();
            q.Enqueue(start);

            //visited.Add(start);

            while (q.Count > 0)
            {
                var c = q.Dequeue();

                var list = cavityGraph.AdjacentEdgeList(c);
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i].Other(c);
                    double dl = Math.Min(c.DepthLength + list[i].Length, v.DepthLength);

                    if (dl < v.DepthLength)
                    {
                        v.DepthLength = dl;
                        q.Enqueue(v);
                    }
                }
            }
        }

        void ComputeDepth()
        {
            cavityGraph.Vertices.ForEach(f => f.Depth = f.IsBoundary ? 0 : int.MaxValue);            
            cavityGraph.Vertices.Where(f => f.IsBoundary).ForEach(f => Bfs(f));
        }

        void ComputeDepthLength()
        {
            cavityGraph.Vertices.ForEach(f => f.DepthLength = f.IsBoundary ? 0.0 : double.MaxValue);
            cavityGraph.Vertices.Where(f => f.IsBoundary).ForEach(f => BfsL(f));
        }

        UndirectedGraph<Tetrahedron, Edge> RemoveShallowVertices(UndirectedGraph<Tetrahedron, Edge> graph)
        {
            HashSet<Tetrahedron> safeVertices = new HashSet<Tetrahedron>();
            
            foreach (var vertex in graph.Vertices.Where(v => v.Depth == Parameters.MinDepth + 1))
            {
                var edges = graph.AdjacentEdgeList(vertex);
                for (int i = 0; i < edges.Count; i++)
                {
                    var v = edges[i].GetOtherVertex(vertex);
                    safeVertices.Add(v);
                }

                //.Run(v => graph.AdjacentVertices(v).Run(u => safeVertices.Add(u)));
            }

            for (int i = Parameters.MinDepth; i >= 0; i--)
            {
                var snapshot = safeVertices.Where(v => v.Depth == i).ToArray();
                foreach (var vertex in snapshot)
                {
                    var edges = graph.AdjacentEdgeList(vertex);
                    for (int j = 0; j < edges.Count; j++)
                    {
                        var v = edges[j].GetOtherVertex(vertex);
                        safeVertices.Add(v);
                    }
                }
                //  snapshot.Run(v => graph.AdjacentVertices(v).Run(u => safeVertices.Add(u)));
            }

            for (int i = Parameters.MinDepth; i >= 0; i--)
            {
                graph.Vertices
                    .Where(v =>
                        {
                            if (v.Depth != i || safeVertices.Contains(v)) return false;

                            var edges = graph.AdjacentEdgeList(v);
                            for (int j = 0; j < edges.Count; j++)
                            {
                                if (edges[j].GetOtherVertex(v).Depth > i) return false;
                            }

                            return true;
                            //return v.Depth == i && !safeVertices.Contains(v) && graph.AdjacentVertices(v).All(u => u.Depth <= i);
                        })
                    .ToArray()
                    .ForEach(v => graph.RemoveVertex(v));
            }

            return graph;
        }

        Complex Create(ComputationProgress progress)
        {
            Random rnd = new Random(0);

            progress.UpdateStatus("Initializing...");

            var structure = Structure;

            structure.Atoms.ForEach(a =>
            {
                a.Position = new Vector3D(a.Position.X + 0.0001 * rnd.NextDouble() - 0.00005, a.Position.Y + 0.0001 * rnd.NextDouble() - 0.00005, a.Position.Z + 0.0001 * rnd.NextDouble() - 0.00005);
            });

            IEnumerable<IAtom> pivotsSeq = structure.PdbResidues()
                    // HOH and small residues are evil
                    .Where(r => r.ActiveAtomsForTunnelComputation() != null && !r.IsWater /*&& r.Atoms.Count > 3*/)
                    .SelectMany(r => r.ActiveAtomsForTunnelComputation());

            if (Parameters.IgnoreHETAtoms) pivotsSeq = pivotsSeq.Where(a => !a.PdbRecordName().EqualOrdinalIgnoreCase("HETATM"));
            if (Parameters.IgnoreHydrogens) pivotsSeq = pivotsSeq.Where(a => a.ElementSymbol != ElementSymbols.H);

            pivots = pivotsSeq.Select(a => new Vertex(a)).ToArray();
            
            progress.UpdateStatus("Triangulating...");
            VoronoiMesh3D<Vertex, Tetrahedron, Edge> voronoi;
            //voronoi = VoronoiMesh.Create<Vertex, Tetrahedron, Edge>(pivots);
            voronoi = VoronoiMesh3D<Vertex, Tetrahedron, Edge>.Create(pivots, v => v.Position);
            
            progress.UpdateStatus("Updating Cells...");
            Parallel.ForEach(voronoi.Vertices, c => c.Init());
            
            //foreach (var c in voronoi.Cells) c.Update();
            //foreach (var e in voronoi.Edges) e.Update();

            //voronoiGraph = new UndirectedGraph<Tetrahedron, Edge>();
            //voronoiGraph.AddVerticesAndEdgeRange(voronoi.Edges);

            //voronoi.Mesh.Vertices.Run(v => v.Clearance = voronoi.Mesh.AdjacentEdges(v).Max(e => e.Clearance));
            //foreach (var v in voronoi.Cells)
            //{
            //    double max = double.NegativeInfinity;
            //    var edges = voronoiGraph.AdjacentEdgeList(v);
            //    for (int i = 0; i < edges.Count; i++)
            //    {
            //        if (edges[i].Clearance > max) max = edges[i].Clearance;
            //    }
            //    v.Clearance = max; // voronoi.Mesh.AdjacentEdges(v).Max(e => e.Clearance);
            //}
            this.Triangulation = voronoi;
            this.KdTree = new KDAtomTree(pivots.Select(v => v.Atom), method: K3DPivotSelectionMethod.Average);
            this.FreeKdTree = new KDAtomTree(pivots.Where(v => v.Atom.IsBackboneAtom() || v.Atom.IsHetAtom()).Select(v => v.Atom), method: K3DPivotSelectionMethod.Average);

            ComputeComplex(progress);

            return this;
        }

        private void ComputeComplex(ComputationProgress progress)
        {
            cavityGraph = new UndirectedGraph<Tetrahedron, Edge>();
            cavityGraph.AddVerticesAndEdgeRange(Triangulation.Edges);
            foreach (var v in Triangulation.Vertices) { v.IsBoundary = false; }
            foreach (var v in Triangulation.Vertices)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (v.Adjacency[i] == null)
                    {
                        v.IsBoundary = true;
                        break;
                    }
                }
            }

            progress.UpdateStatus("Computing Cavities...");

            // if strict, need to update the edges 1st!

            //if (Parameters.StrictInterior) foreach (var e in cavityGraph.Edges) e.Update(this);
            //foreach (var c in Triangulation.Vertices) c.Update(Parameters.StrictInterior, cavityGraph);

            //if (Parameters.StrictInterior) Parallel.ForEach(cavityGraph.Edges, e => e.Update(this));
            Parallel.ForEach(Triangulation.Vertices, c => c.Update(Parameters.StrictInterior, cavityGraph));

            Cavity surfaceCavity;
            var outside = RemoveOutsideAndInterior(out surfaceCavity);
            
            ComputeDepth();

            Parallel.ForEach(cavityGraph.Edges, e => e.Update(this));
            Parallel.ForEach(surfaceCavity.CavityGraph.Edges, e => e.Update(this));
            
            RemoveShallowVertices(cavityGraph);

            ComputeDepth();
            ComputeDepthLength();


            var vertices = cavityGraph.Vertices.ToHashSet();
            var components = GetComponents(vertices);

            this.SurfaceCavity = surfaceCavity;

            var channels = components
                .Where(comp => comp.Any(v => v.IsBoundary))
             //   .AsParallel()
                .Select(g => Cavity.Create(ToGraph(g.ToHashSet()), this))
                .Where(c => c.DepthLength > Parameters.MinDepthLength && c.Depth > Parameters.MinDepth)
              //  .AsSequential()
                .OrderByDescending(cav => cav.Volume)
                .ToArray();

            var voids = components
                .Where(comp => comp.All(v => !v.IsBoundary))
             // .AsParallel()
                .Select(g => Cavity.Create(ToGraph(g.ToHashSet()), this))
                .Where(cav => cav.Volume > 0 && cav.Tetrahedrons.Count > 20)
            //  .AsSequential()
                .OrderByDescending(cav => cav.Volume)
                .ToArray();

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].Id = i + 1;
            }

            for (int i = 0; i < voids.Length; i++)
            {
                voids[i].Id = i + 1;
            }

            progress.UpdateStatus("Generating Suggestions...");

            this.Cavities = new ReadOnlyCollection<Cavity>(channels);
            this.Voids = new ReadOnlyCollection<Cavity>(voids);
            this.TunnelOrigins = new TunnelOriginCollection(this);
        }

        /// <summary>
        /// Tries to add custom exits wherever possible.
        /// </summary>
        /// <param name="xs"></param>
        public IList<Tuple<int, CavityOpening[]>> AddCustomExits(IEnumerable<Tuple<int, Vector3D>> xs)
        {
            xs = xs.ToArray();

            var ret = xs.ToDictionary(x => x.Item1, _ => new HashSet<CavityOpening>());

            foreach (var c in new[] { SurfaceCavity }.Concat(Cavities))
            {
                var used = new HashSet<Tetrahedron>();
                foreach (var x in xs)
                {
                    var exit = c.GetOpening(x.Item2, Parameters.OriginRadius);
                    if (exit != null && used.Add(exit.Pivot))
                    {
                        c.AddUserOpening(exit);
                        ret[x.Item1].Add(exit);
                    }
                }
            }

            return ret.Select(v => Tuple.Create(v.Key, v.Value.ToArray())).ToArray();
        }
        
        /// <summary>
        /// Creates a computation object for the complex.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Computation<Complex> CreateAsync(IStructure structure, ComplexParameters parameters)
        {
            Complex c = new Complex { Structure = structure, Parameters = parameters };
            return Computation.Create(p => c.Create(p));
        }

        /// <summary>
        /// Creates the complex.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Complex Create(IStructure structure, ComplexParameters parameters)
        {
            return CreateAsync(structure, parameters).RunSynchronously();
        }

        async void UpdateOpenings(ComputationProgress p)
        {
            p.UpdateStatus("Updating openings...");
            await TaskEx.WhenAll(Cavities.Select(ch => TaskEx.Run(() => ch.UpdateOpenings())).ToArray());
        }
        
        /// <summary>
        /// Creates a computation object that updates the openings.s
        /// </summary>
        /// <returns></returns>
        public Computation UpdateOpeningsAsync()
        {
            return Computation.Create(UpdateOpenings);
        }
        
        /// <summary>
        /// Computation object that updates the complex (call when the parameters change)
        /// </summary>
        /// <returns></returns>
        public Computation UpdateAsync()
        {
            return Computation.Create(p => ComputeComplex(p));
        }

        private Complex()
        {
            Tunnels = new TunnelCollection();
            Pores = new TunnelCollection();
            Paths = new TunnelCollection();
            ScalarFields = new ObservableCollection<Geometry.FieldBase>();
        }
    }
}
