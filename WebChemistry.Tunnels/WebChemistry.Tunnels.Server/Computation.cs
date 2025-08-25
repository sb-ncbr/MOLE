/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Tunnels.Core;
    using WebChemistry.Framework.Math;
    using WebChemistry.Tunnels.Core.Geometry;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Queries.Core;

    class TunnelsComputation
    {
        ExecutionContext queryCtx;
        TunnelsConfig Config;

        List<Tuple<Tetrahedron, Tetrahedron>> PathSpecs = new List<Tuple<Tetrahedron, Tetrahedron>>();

        public IStructure Structure { get; set; }
        public ExecutionContext QueryExecutionContext { get { return queryCtx ?? (queryCtx = ExecutionContext.Create(Structure)); } }
        public Complex Complex { get; set; }
        public Stopwatch Timer { get; private set; }

        public List<Vector3D> OriginPoints { get; set; }
        public List<Vector3D> CustomExitPoints { get; set; }
        public List<Vector3D> FoundCustomExitPoints { get; set; }

        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        void WriteDetails(string format, params object[] args)
        {
            if (!Program.NoDetails)
            {
                Console.WriteLine(format, args);
            }
        }

        void IgnoreResidues()
        {
            var rs = Structure.PdbResidues();

            HashSet<PdbResidue> toIgnoreList = new HashSet<PdbResidue>();
            
            foreach (var r in Config.NonActiveParts.Residues)
            {
                var res = rs.FromIdentifier(PdbResidueIdentifier.Create(r.SequenceNumber, r.Chain, r.InsertionCode));                

                if (res != null && !res.IsWater)
                {
                    toIgnoreList.Add(res);
                    res.SetActiveAtomsForTunnelComputation(null);
                }
                else
                {
                    Console.WriteLine("Warning: Residue '{0}' not found. Cannot mark as non-active.", r.ToString());
                }
            }

            var ignoredAtomsList = new List<IAtom>();
            var ignoredResidueList = new List<PdbResidue>();
            if (Config.NonActiveParts.Queries.Length > 0)
            {
                var atomSet = new HashSet<IAtom>();
                var residueSet = new HashSet<PdbResidue>();
                var queryResidueList = new List<PdbResidue>();

                foreach (var q in Config.NonActiveParts.Queries)
                {
                    foreach (var p in q.Query.Matches(QueryExecutionContext))
                    {
                        p.Atoms.ForEach(a =>
                        {
                            if (!atomSet.Add(a)) return;
                            var r = rs.FromIdentifier(a.ResidueIdentifier());
                            if (r == null) return;
                            if (!residueSet.Add(r)) return;
                            queryResidueList.Add(r);
                        });
                    }
                }
                
                foreach (var r in queryResidueList)
                {
                    var atoms = r.Atoms.Where(a => !atomSet.Contains(a)).AsList();
                    if (atoms.Count == 0)
                    {
                        r.SetActiveAtomsForTunnelComputation(null);
                        ignoredResidueList.Add(r);
                    }
                    else
                    {
                        r.SetActiveAtomsForTunnelComputation(atoms);
                        ignoredAtomsList.AddRange(r.Atoms.Where(a => atomSet.Contains(a)));
                    }
                }
            }
            
            if (toIgnoreList.Count > 0)
            {
                WriteDetails("Ignored Residues (Listed):");
                WriteResidueList(toIgnoreList);
            }

            if (ignoredResidueList.Count > 0)
            {
                WriteDetails("Ignored Residues (Query):");
                WriteResidueList(ignoredResidueList);
            }

            if (ignoredAtomsList.Count > 0)
            {
                WriteDetails("Ignored Atoms (Query):");

                var toWrite = ignoredAtomsList.Take(20).OrderBy(a => a.Id).Select(a => string.Format("{0} {1} {2}", a.ElementSymbol, a.PdbName(), a.PdbSerialNumber()));
                ConfigBase.MakeLines(string.Join(", ", toWrite), 70)
                    .ForEach(l => WriteDetails("  " + l));
                if (ignoredAtomsList.Count > 20)
                {
                    WriteDetails("  ...and " + (ignoredAtomsList.Count - 20) + " more.");
                }
            }
        }
        
        void WriteResidueList(ICollection<PdbResidue> residues)
        {
            var toWrite = residues.Take(20).OrderBy(r => r).Select(r => r.ToString());
            ConfigBase.MakeLines(string.Join(", ", toWrite), 70)
                .ForEach(l => WriteDetails("  " + l));
            if (residues.Count > 20)
            {
                WriteDetails("  ...and " + (residues.Count - 20) + " more.");
            }
        }

        void SelectChains()
        {
            if (string.IsNullOrEmpty(Config.Input.SpecificChains)) return;
            var specificChains = Config.Input.SpecificChains.ToLowerInvariant().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Structure.PdbResidues()
                .ForEach(r =>
                {
                    if (!specificChains.Contains(r.ChainIdentifier)) r.SetActiveAtomsForTunnelComputation(null);
                });
        }

        Tuple<Tetrahedron, Vector3D>[] ResolveSurfaceTetrahedrons(TriangulationPoint point)
        {
            var centers = point.ToPoints(Structure, () => QueryExecutionContext);
            if (centers.Length == 0) return new Tuple<Tetrahedron,Vector3D>[0];
            return centers.Select(c => Tuple.Create(Complex.SurfaceCavity.GetTetrahedron(c, Config.Params.Parameters.OriginRadius), c)).Where(p => p.Item1 != null).ToArray();
        }

        void ResolveOriginPoint(TriangulationPoint point)
        {
            var centers = point.ToPoints(Structure, () => QueryExecutionContext);
            if (centers.Length == 0)
            {
                WriteDetails("  => Empty point.");
                return;
            }

            foreach (var center in centers)
            {
                OriginPoints.Add(center);
                var origins = Complex.TunnelOrigins.AddFromPoint(center);
                if (origins.Count == 0)
                {
                    WriteDetails("  => No origins found with OriginRadius.");
                }
                foreach (var o in origins)
                {
                    WriteDetails("  => Id {0}", o.Id);
                    WriteDetails("     Cavity {0}, Delta = {1:0.00}ang, Near {{{2}}}",
                        o.Cavity.Id,
                        center.DistanceTo(o.Tetrahedron.Center),
                        string.Join(", ", o.Tetrahedron.GetResidues(Structure.PdbResidues())));
                }
            }
        }

        void ResolveExitPoints(IEnumerable<TriangulationPoint> points)
        {
            var map = points.Select((p, i) => new { P = p, I = i }).ToDictionary(p => p.I, p => p.P);
            var centers = map.Select(v => Tuple.Create(v.Key, v.Value.ToPoints(Structure, () => QueryExecutionContext))).ToArray();
            
            var nullCenters = centers.Where(c => c.Item2.Length == 0).ToArray();
            var validCenters = centers.Where(c => c.Item2.Length > 0).SelectMany(c => c.Item2.Select(p => Tuple.Create(c.Item1, p))).ToArray();
            var validMap = validCenters.ToDictionary(c => c.Item1, c => c.Item2);

            foreach (var c in validCenters)
            {
                CustomExitPoints.Add(c.Item2);
            }

            foreach (var c in nullCenters)
            {
                var p = map[c.Item1];
                WriteDetails("Custom exit {0}", p.ToString());
                WriteDetails("  => Empty point.");
            }

            var exits = Complex.AddCustomExits(validCenters);
            foreach (var e in exits)
            {
                var p = map[e.Item1];
                WriteDetails("Custom exit {0}", p.ToString());
                if (e.Item2.Length == 0)
                {
                    WriteDetails("  => No exit found within OriginRadius.");
                }
                foreach (var o in e.Item2)
                {
                    FoundCustomExitPoints.Add(o.Pivot.Center);
                    WriteDetails("  => Delta = {0:0.00}ang, Near {{{1}}}",
                        validMap[e.Item1].DistanceTo(o.Pivot.Center),
                        string.Join(", ", o.Pivot.GetResidues(Structure.PdbResidues())));
                }
            }
        }

        void AddOrigins()
        {
            if (Config.Origins.Points.Points.Length == 0) return;

            WriteDetails("");
            foreach (var o in Config.Origins.Points.Points)
            {
                WriteDetails("Origin {0}", o.ToString());
                ResolveOriginPoint(o);
            }
        }

        void AddCustomExits()
        {
            if (Config.CustomExits.Points.Points.Length == 0) return;

            WriteDetails("");
            ResolveExitPoints(Config.CustomExits.Points.Points);
        }

        void AddPathSpecs()
        {
            //if (Config.Paths.Paths.Length == 0) return;

            foreach (var p in Config.Paths.Paths)
            {
                WriteDetails("");
                WriteDetails("Path");
                var starts = ResolveSurfaceTetrahedrons(p.Start);
                var ends = ResolveSurfaceTetrahedrons(p.End);

                if (starts.Length == 0)
                {
                    WriteDetails("  Start {0} => No points found within OriginRadius.", p.Start.ToString());
                }
                else
                {
                    WriteDetails("  Start {0}", p.Start.ToString());
                    foreach (var s in starts)
                    {
                        WriteDetails("    => Delta = {0:0.00}ang, Near {{{1}}}", 
                            s.Item2.DistanceTo(s.Item1.Center),
                            string.Join(", ", s.Item1.GetResidues(Structure.PdbResidues())));
                    }
                }

                if (ends.Length == 0)
                {
                    WriteDetails("  End {0} => No points found within OriginRadius.", p.End.ToString());
                }
                else
                {
                    WriteDetails("  End {0}", p.End.ToString());
                    foreach (var e in ends)
                    {
                        WriteDetails("    => Delta = {0:0.00}ang, Near {{{1}}}",
                            e.Item2.DistanceTo(e.Item1.Center),
                            string.Join(", ", e.Item1.GetResidues(Structure.PdbResidues())));
                    }
                }

                int added = 0;
                HashSet<string> usedPoints = new HashSet<string>(StringComparer.Ordinal);
                foreach (var s in starts)
                {
                    foreach (var e in ends)
                    {
                        if (usedPoints.Add(s.Item2.ToString() + e.Item2.ToString()) && usedPoints.Add(e.Item2.ToString() + s.Item2.ToString()))
                        {
                            PathSpecs.Add(Tuple.Create(s.Item1, e.Item1));
                            added++;
                        }
                    }
                }
                WriteDetails("  Added {0} point pair(s).", added);
            }
        }

        void ComputeTunnels()
        {
            bool computeTunnels = Config.Export.Parameters.ExportTunnels || Config.Export.Parameters.ExportMergedPores;
            if (!computeTunnels) return;

            Console.Write("Computing tunnels... ");

            foreach (var o in Complex.TunnelOrigins.OfType(TunnelOriginType.User))
            {
                Complex.ComputeTunnels(o);                
            }

            if (Config.Origins.UseAutoPoints)
            {
                foreach (var o in Complex.TunnelOrigins.OfType(TunnelOriginType.Computed))
                {
                    Complex.ComputeTunnels(o);
                }
            }

            Console.WriteLine("{0} found.", Complex.Tunnels.Count);
        }

        void ComputePores()
        {
            var compute = Config.Export.Parameters.ExportMergedPores || Config.Export.Parameters.ExportAutoPores || Config.Export.Parameters.ExportUserPores;
            if (!compute) return;

            Console.Write("Computing pores... ");

            if (Config.Export.Parameters.ExportMergedPores)
            {
                Complex.Porify(Complex.Tunnels);
            }

            if (Config.Export.Parameters.ExportAutoPores)
            {
                Complex.ComputePores();
            }

            if (!Config.Export.Parameters.ExportAutoPores && Config.Export.Parameters.ExportUserPores)
            {
                Complex.ComputeUserPores();
            }

            Console.WriteLine("{0} found ({1} pores, {2} merged).", Complex.Pores.Count, Complex.Pores.Count(p => !p.IsMergedPore), Complex.Pores.Count(p => p.IsMergedPore));
        }

        void ComputePaths()
        {
            if (PathSpecs.Count == 0) return;

            Console.Write("Computing paths... ");
            Complex.ComputePaths(PathSpecs);
            Console.WriteLine("{0} found.", Complex.Paths.Count);
        }

        void ComputeFields()
        {
            if (Complex.ScalarFields.Count == 0) return;
            Console.Write("Computing charges... ");
            foreach (var t in Complex.Tunnels) t.ComputeFields(Config.Export.Parameters.MeshDensity);
            foreach (var t in Complex.Pores) t.ComputeFields(Config.Export.Parameters.MeshDensity);
            foreach (var t in Complex.Paths) t.ComputeFields(Config.Export.Parameters.MeshDensity);
            Console.WriteLine("done.");
        }

        Motive ToMotive(Tunnel t)
        {
            var atoms = t.Lining.SelectMany(r => r.Atoms);
            return Motive.FromAtoms(atoms, null, QueryExecutionContext.RequestCurrentContext());
        }

        int Filter(TunnelCollection ts)
        {
            var filter = Config.Params.QueryFilter;
            var toRemove = new List<Tunnel>();
            var args = new object[1];

            foreach (var t in ts)
            {
                args[0] = ToMotive(t);
                var r = filter.Execute(QueryExecutionContext, args);
                if (r == null || !(r is bool))
                {
                    Console.WriteLine("Warning: Tunnel Query Filter did not return a boolean value. Filter ignored.");
                    continue;
                }
                if (!((bool)r))
                {
                    toRemove.Add(t);
                }
            }

            foreach (var t in toRemove) ts.Remove(t);
            return toRemove.Count;
        }

        void FilterTunnels()
        {
            if (Config.Params.QueryFilter == null) return;
            
            Console.Write("Filtering channels... ");
            var removedTunnels = Filter(Complex.Tunnels);
            var removedPores = Filter(Complex.Pores);
            var removedPaths = Filter(Complex.Paths);
            Console.Write("removed {0} tunnel(s), {1} pore(s), {2} path(s).\n", removedTunnels, removedPores, removedPaths);
        }

        void RunInternal()
        {
            Computation.DefaultScheduler = System.Reactive.Concurrency.ThreadPoolScheduler.Instance;            

            // Set the custom VDW radii
            foreach (var v in Config.CustomVdw.Values)
            {
                TunnelVdwRadii.SetRadius(v.Key, v.Value);
            }

            //Config.CustomVdw.Values

            Console.WriteLine();
            Console.Write("Reading structure... ");
            if (Config.Input.Filename == Program.StdInInput)
            {
                var input = Console.In.ReadToEnd();
                Structure = StructureReader.ReadString(Config.Input.Filename + ".pdb", input, customType: Config.Input.ReadAllModels ? StructureReaderType.PdbAssembly : StructureReaderType.Auto).Structure;
            }
            else
            {
                Structure = StructureReader.Read(Config.Input.Filename, customType: Config.Input.ReadAllModels ? StructureReaderType.PdbAssembly : StructureReaderType.Auto).Structure;
            }
            Console.WriteLine("{0} atoms, {1} residues.", Structure.Atoms.Count, Structure.PdbResidues().Count);
            IgnoreResidues();
            SelectChains();

            Console.Write("Computing complex... ");
            Complex = Complex.Create(Structure, Config.Params.Parameters);
            Console.WriteLine("{0} tetrahedrons, {1} edges.", Complex.Triangulation.Vertices.Count, Complex.Triangulation.Edges.Count);

            if (Config.Charges.Sources.Length > 0)
            {
                Console.Write("Reading charge sources... ");
                HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                KDAtomTree _noH = null, _yesH = null;
                Func<KDAtomTree> noHydrogens = (() => _noH ?? (_noH = new KDAtomTree(Structure.Atoms.Where(a => !a.IsWater() && a.ElementSymbol != ElementSymbols.H), method: K3DPivotSelectionMethod.Average)));
                Func<KDAtomTree> yesHydrogens = (() => _yesH ?? (_yesH = 
                    Structure.Atoms.Count(a => a.ElementSymbol == ElementSymbols.H) == 0 
                        ? noHydrogens()
                        : new KDAtomTree(Structure.Atoms.Where(a => !a.IsWater()), method: K3DPivotSelectionMethod.Average)));

                K3DTree<PdbResidue> _residueTree = null;
                Func<K3DTree<PdbResidue>> residueTree = (() => _residueTree ?? (_residueTree = new K3DTree<PdbResidue>(Structure.PdbResidues(), r => r.Atoms.GeometricalCenter(), method: K3DPivotSelectionMethod.Average)));

                foreach (var src in Config.Charges.Sources)
                {
                    //Complex.ScalarFields.Add(ScalarFieldGrid.MakeRandom(f.Name, Structure));

                    if (!usedNames.Add(src.Name)) 
                    {
                        throw new InvalidOperationException(string.Format("Charge source name '{0}' is already in use.", src.Name));
                    }

                    try
                    {
                        switch (src.Type)
                        {
                            case ChargeSourceElementType.RandomGrid: Complex.ScalarFields.Add(ScalarFieldGrid.MakeRandom(src.Name, Structure, src.MinValue, src.MaxValue)); break;
                            case ChargeSourceElementType.ScalarGrid: Complex.ScalarFields.Add(ScalarFieldGrid.FromOpenDX(src.Name, src.Source)); break;
                            case ChargeSourceElementType.AtomValues:
                            {
                                Complex.ScalarFields.Add(AtomValueField.FromFile(src.Name, src.Source, Structure,
                                    src.IgnoreHydrogens, 
                                    AtomValueField.NeedsAtomPivots(src.ValuesMethod) ? (src.IgnoreHydrogens ? noHydrogens() : yesHydrogens()) : null,
                                    AtomValueField.NeedsAtomPivots(src.ValuesMethod) ? residueTree() : null,
                                    src.ValuesMethod, src.Radius, src.K)); 
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException(string.Format("Error reading charge source '{0}': {1}", src.Name, e.Message));
                    }
                }
                Console.WriteLine("{0} read.", Config.Charges.Sources.Length);
            }

            AddOrigins();
            AddCustomExits();
            AddPathSpecs();

            if (!Program.NoDetails) Console.WriteLine();

            ComputeTunnels();
            ComputePores();
            ComputePaths();

            FilterTunnels();

            ComputeFields();
        }
        
        public void Run()
        {
            Timer = Stopwatch.StartNew();
            try
            {
                RunInternal();
            }
            catch (Exception e)
            {
                HasError = false;
                ErrorMessage = e.Message;
                throw;
            }
            finally
            {
                Timer.Stop();
            }
        }

        public TunnelsComputation(TunnelsConfig config)
        {
            this.Config = config;
            this.OriginPoints = new List<Vector3D>();
            this.CustomExitPoints = new List<Vector3D>();
            this.FoundCustomExitPoints = new List<Vector3D>();
        }
    }
}
