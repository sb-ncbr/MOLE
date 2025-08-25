/*
* Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
*/

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Tunnels.Core.Helpers;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
    using System.Xml.Linq;
    using System.Globalization;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Tunnels.Core.Geometry;

    public enum TunnelType
    {
        Tunnel = 0,
        Pore,
        Path
    }

    /// <summary>
    /// Represents a tunnel.
    /// </summary>
    public class Tunnel : InteractiveObject, IEquatable<Tunnel>
    {
        Tetrahedron[] path;
        PdbResidueCollection lining;
        TunnelLining tunnelLayers;
        CubicSplineInterpolation sX, sY, sZ, sR, sFreeR, sBR;

        /// <summary>
        /// Tunnel type
        /// </summary>
        public TunnelType Type { get; private set; }

        /// <summary>
        /// Was the pore created from 2 tunnels?
        /// </summary>
        public bool IsMergedPore { get; private set; }

        /////// <summary>
        /////// Determines whether the tunnel/starpoint is close to the molecular surface.
        /////// </summary>
        ////public bool IsCloseToSurface { get; private set; }

        /// <summary>
        /// Tetrahedrons forming the tunnel.
        /// </summary>
        public Tetrahedron[] Path { get { return path; } }

        /// <summary>
        /// The cavity the tunnel belongs to.
        /// </summary>
        public Cavity Cavity { get; private set; }

        /// <summary>
        /// Start point of the tunnel.
        /// </summary>
        public TunnelOrigin StartPoint { get; private set; }

        /// <summary>
        /// Surrounding residues of the tunnel.
        /// </summary>
        public PdbResidueCollection Lining { get { return lining; } }

        /// <summary>
        /// HetAtm residues.
        /// </summary>
        public IEnumerable<PdbResidue> HetResidues { get; private set; }

        /// <summary>
        /// The opening (= Exit) of the tunnel.
        /// </summary>
        public CavityOpening Opening { get; private set; }

        /// <summary>
        /// Default density of points per angstrom.
        /// </summary>
        public static readonly double DefaultProfileDensity = 8;

        /// <summary>
        /// Tunnel profile with the density 8 points per ang.
        /// </summary>
        public TunnelProfile Profile { get { return GetProfile(DefaultProfileDensity); } }

        /// <summary>
        /// Length of the tunnel.
        /// </summary>
        public double Length { get; private set; }

        //const double defaultProfileDensity = 1.5;
        ///// <summary>
        ///// Default profile (1.5 samples per ang.)
        ///// </summary>
        //public TunnelProfile DefaultProfile { get; private set; }

        /// <summary>
        /// Determines if the tunnel should be automatically displayed.
        /// </summary>
        public bool AutomaticallyDisplay { get; internal set; }

        /// <summary>
        /// Physico-chemical properties of a tunnel.
        /// </summary>
        public TunnelPhysicoChemicalProperties PhysicoChemicalProperties { get; private set; }

        /// <summary>
        /// Layer-weighted physico-chemical properties of a tunnel.
        /// </summary>
        public TunnelPhysicoChemicalProperties LayerWeightedPhysicoChemicalProperties { get; private set; }

        /// <summary>
        /// Fields.
        /// </summary>
        public Dictionary<string, TunnelScalarField> ScalarFields { get; private set; }

        int id;
        /// <summary>
        /// Tunnel Id, mutable!! (Assined by TunnelCollection)
        /// </summary>
        public int Id
        {
            get { return id; }
            internal set
            {
                if (id == value) return;
                id = value;
                NotifyPropertyChanged("Id");
            }
        }

        internal Vector3D GetCenterlinePoint(double t)
        {
            //var t = distance / Length;
            return new Vector3D(sX.Interpolate(t), sY.Interpolate(t), sZ.Interpolate(t));
        }

        TunnelProfile.Node GetProfileNode(TunnelProfile.Node prev, double t)
        {
            var p = GetCenterlinePoint(t);
            var d = prev.Distance + prev.Center.DistanceTo(p);
            return new TunnelProfile.Node(t, d, sR.Interpolate(t), sFreeR.Interpolate(t), sBR.Interpolate(t), p);
        }

        IEnumerable<IAtom> GetSurroundingAtoms(double t, int count = 3)
        {
            var p = GetCenterlinePoint(t);
            return this.Cavity.Complex.KdTree.NearestCount(p, count).Select(a => a.Value).ToArray();
        }

        Dictionary<double, TunnelProfile> profileCache = new Dictionary<double, TunnelProfile>();

        TriangulatedSurface surface = null;

        public TriangulatedSurface GetSurface(double density)
        {
            if (surface != null) return surface;
            surface = IsoSurface.Create(this, density);
            return surface;
        }


        /// <summary>
        /// Calculates the tunnel profile.
        /// </summary>
        /// <param name="pointDensityPerAngstrom">Number of control points per one Angstrom.</param>
        /// <returns>Tunnel profile.</returns>
        public TunnelProfile GetProfile(double pointDensityPerAngstrom)
        {
            if (profileCache.ContainsKey(pointDensityPerAngstrom)) return profileCache[pointDensityPerAngstrom];

            var numPoints = (int)(pointDensityPerAngstrom * Length) + 1;
            var dt = 1.0 / (numPoints - 1);
            var ctp = Enumerable.Range(0, numPoints)
                .Scan(new TunnelProfile.Node(0, 0, sR.Interpolate(0), sFreeR.Interpolate(0), sBR.Interpolate(0), GetCenterlinePoint(0)), (prev, i) => GetProfileNode(prev, dt * i))
                .ToArray();

            //.Select(i => dt * i)
            //.Select(d => GetProfileNode(d))
            //.ToArray();

            var ret = new TunnelProfile(ctp, pointDensityPerAngstrom);
            profileCache.Add(pointDensityPerAngstrom, ret);
            return ret;
        }

        /// <summary>
        /// Computes the lining layers of the tunnel (surrounding residues from origin to exit)
        /// </summary>
        /// <returns>Lining layers</returns>
        public TunnelLining GetLiningLayers()
        {
            const int samplesPerAngstrom = 3, numSurroundingAtoms = 5;

            if (tunnelLayers != null) return tunnelLayers;

            int n = (int)Math.Ceiling(Length * samplesPerAngstrom);

            double dt = 1.0 / (n - 1);

            var layers = Enumerable.Range(0, n)
                // Calculate the profile (centerline + surrounding atoms)
                .Scan(new
                {
                    SurroudingAtoms = GetSurroundingAtoms(0, numSurroundingAtoms),
                    Profile = new TunnelProfile.Node(0, 0, sR.Interpolate(0), sFreeR.Interpolate(0), sBR.Interpolate(0), GetCenterlinePoint(0)),
                    //Radius = sR.Interpolate(0)
                },
                (prev, i) => new
                {
                    SurroudingAtoms = GetSurroundingAtoms(i * dt, numSurroundingAtoms),
                    Profile = GetProfileNode(prev.Profile, dt * i),
                    //Radius = sR.Interpolate(dt * i)
                })
                // Find surrounding residues and determine if they are touched at backbone or not
                .Select(layer =>
                {
                    var rs = Cavity.Complex.Structure.PdbResidues();
                    return new
                    {
                        Layer = layer,
                        Lining = layer.SurroudingAtoms
                            .GroupBy(a => a.ResidueIdentifier())
                            .Select(g => new { Residue = rs.FromIdentifier(g.Key), IsBackbone = g.All(a => a.IsBackboneAtom()) })
                            .Distinct()
                            .OrderBy(r => r.Residue.Number)

                            //.Take(3)
                            .ToArray()
                    };
                })
                // We want to have a nice data representation ...
                .Select(layer => new TunnelLayer
                {
                    SurroundingAtoms = layer.Layer.SurroudingAtoms.ToArray(),
                    Lining = layer.Lining.Select(r => r.Residue).Distinct().ToArray(),
                    BackboneLining = layer.Lining.Where(r => r.IsBackbone).Select(r => r.Residue).ToArray(),
                    NonBackboneLining = layer.Lining.Where(r => !r.IsBackbone).Select(r => r.Residue).ToArray(),
                    Distance = layer.Layer.Profile.Distance,
                    Radius = layer.Layer.Profile.Radius,
                    FreeRadius = layer.Layer.Profile.FreeRadius,
                    BRadius = layer.Layer.Profile.BRadius,
                    Center = layer.Layer.Profile.Center
                })
                .ToArray();
            // Remember only the layers where the lining residues change
            //.GroupBy(layer => string.Concat(layer.Lining.Select(r => r.UniqueIdentifier.ToString())))
            //.Select(group =>
            //    {
            //        var layer = group.First();
            //        var radius = group.Min(l => l.Radius);

            //        return new TunnelLayer
            //        {
            //            SurroundingAtoms = layer.SurroundingAtoms,
            //            Lining = layer.Lining,
            //            BackboneLining = layer.BackboneLining,
            //            NonBackboneLining = layer.NonBackboneLining,
            //            Distance = layer.Distance,
            //            Radius = radius,
            //            Center = layer.Center,
            //            StartDistance = layer.Distance,
            //            EndDistance = group.Last().Distance
            //        };
            //    })
            ////.DistinctUntilChanged(layer => string.Concat(layer.Lining.Select(r => r.UniqueIdentifier.ToString())))
            //.ToArray();

            var first = layers[0];
            var start = 0;
            var fk = string.Concat(first.NonBackboneLining.Select(r => r.Identifier.ToString()))
                    + "B" +
                    string.Concat(first.BackboneLining.Select(r => r.Identifier.ToString()));
            List<TunnelLayer> ret = new List<TunnelLayer>();
            for (int i = 1; i < layers.Length; i++)
            {
                var l = layers[i];
                var key =
                    string.Concat(l.NonBackboneLining.Select(r => r.Identifier.ToString()))
                    + "B" +
                    string.Concat(l.BackboneLining.Select(r => r.Identifier.ToString()));

                if (key.Equals(fk)) continue;

                ret.Add(first);
                first.StartDistance = first.Distance;
                first.EndDistance = l.Distance;
                first.Radius = layers.Skip(start).Take(i - start).Min(t => t.Radius);
                first.FreeRadius = layers.Skip(start).Take(i - start).Min(t => t.FreeRadius);
                first.BRadius = layers.Skip(start).Take(i - start).Min(t => t.BRadius);

                first = l;
                start = i;
                fk = key;
            }

            ret.Add(first);
            first.StartDistance = first.Distance;
            first.EndDistance = layers.Last().Distance;
            first.Radius = layers.Skip(start).Min(t => t.Radius);
            first.FreeRadius = layers.Skip(start).Min(t => t.FreeRadius);
            first.BRadius = layers.Skip(start).Min(t => t.BRadius);
            ret.ForEach(l => ComputePhysicoChemicalProperties(l));

            tunnelLayers = new TunnelLining(ret);

            return tunnelLayers;
        }

        static double BfactorToRmsf(IAtom atom)
        {
            return Math.Sqrt(3 * atom.PdbTemperatureFactor() / (8 * Math.PI * Math.PI));
        }

        /// <summary>
        /// Computes splines for centerline and radius.
        /// </summary>
        /// <param name="tetras"></param>
        /// <param name="complex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        static CubicSplineInterpolation[] GetProfileSpline(IEnumerable<Tetrahedron> tetras, Complex complex, out double length)
        {

            var path = tetras.AsList();

            var dt = 1.0 / (path.Count - 1);

            var ts = Enumerable.Range(0, path.Count).Select(i => dt * i).ToArray();
            var splineX = new CubicSplineInterpolation(ts, path.Select(p => p.VoronoiCenter.X).ToArray());
            var splineY = new CubicSplineInterpolation(ts, path.Select(p => p.VoronoiCenter.Y).ToArray());
            var splineZ = new CubicSplineInterpolation(ts, path.Select(p => p.VoronoiCenter.Z).ToArray());

            int numPoints = 100;
            double d = 1.0 / (numPoints - 1);
            length = 0;
            
            List<double> rs = new List<double>();
            List<double> freeRs = new List<double>();
            List<double> bRs = new List<double>();
            Vector3D u;

            for (int i = 0; i < numPoints - 1; i++)
            {
                double t0 = d * i, t1 = t0 + d;
                u = new Vector3D(splineX.Interpolate(t0), splineY.Interpolate(t0), splineZ.Interpolate(t0));
                var v = new Vector3D(splineX.Interpolate(t1), splineY.Interpolate(t1), splineZ.Interpolate(t1));
                var near = complex.KdTree.NearestCount(u, 5);
                var radius = near.Select(a => a.Value.Position.DistanceTo(u) - a.Value.GetTunnelSpecificVdwRadius()).Min();
                var bRadius = radius + near.Select(a => BfactorToRmsf(a.Value)).Average();
                var freeRadius = complex.FreeKdTree.NearestCount(u, 5).Select(a => a.Value.Position.DistanceTo(u) - a.Value.GetTunnelSpecificVdwRadius()).Min();
                rs.Add(Math.Max(radius, 0.01));
                freeRs.Add(Math.Max(freeRadius, 0.01));
                bRs.Add(Math.Max(bRadius, 0.01));
                length += u.DistanceTo(v);
            }

            u = new Vector3D(splineX.Interpolate(1), splineY.Interpolate(1), splineZ.Interpolate(1));
            var near0 = complex.KdTree.NearestCount(u, 5);
            var r0 = near0.Select(a => a.Value.Position.DistanceTo(u) - a.Value.GetTunnelSpecificVdwRadius()).Min();
            var bR0 = r0 + near0.Select(a => BfactorToRmsf(a.Value)).Average(); ;
            rs.Add(Math.Max(r0, 0.01));
            bRs.Add(Math.Max(bR0, 0.01));
            freeRs.Add(Math.Max(complex.FreeKdTree.NearestCount(u, 5).Select(a => a.Value.Position.DistanceTo(u) - a.Value.GetTunnelSpecificVdwRadius()).Min(), 0.01));


            var splineRadius = new CubicSplineInterpolation(Enumerable.Range(0, numPoints).Select(i => d * i).ToArray(), rs);
            var freeSplineRadius = new CubicSplineInterpolation(Enumerable.Range(0, numPoints).Select(i => d * i).ToArray(), freeRs);
            var bSplineRadius = new CubicSplineInterpolation(Enumerable.Range(0, numPoints).Select(i => d * i).ToArray(), bRs);

            return new CubicSplineInterpolation[]
            {
                splineX,
                splineY,
                splineZ,
                splineRadius,
                freeSplineRadius,
                bSplineRadius
            };
        }

        /// <summary>
        /// Calculates the basic profile. Returns false if the tunnel would end up too short.
        /// </summary>
        /// <returns></returns>
        bool CalculateProfile()
        {
            Dictionary<PdbResidue, int> residueOrder = new Dictionary<PdbResidue, int>();
            var residues = this.path
                .SelectMany(t => t.Vertices.Select(v => v.Atom))
                .Select(a => this.Cavity.Complex.Structure.PdbResidues().FromAtom(a)).ToArray();

            residues.ForEach((r, i) => { if (!residueOrder.ContainsKey(r)) residueOrder[r] = i; });

            lining = PdbResidueCollection.Create(residues.Distinct().OrderBy(r => residueOrder[r]));

            List<Tetrahedron> controlPath = new List<Tetrahedron>();

            int start = 0;
            if (this.Type == TunnelType.Tunnel)
            {
                for (start = 0; start < this.path.Length; start++)
                {
                    var c = path[start].VoronoiCenter;
                    var radius = Cavity.Complex.KdTree.NearestCount(c, 5).Select(a => a.Value.Position.DistanceTo(c) - a.Value.GetTunnelSpecificVdwRadius()).Min();
                    if (radius >= Cavity.Complex.Parameters.InteriorThreshold) break;
                }
            }

            if (start == this.Path.Length) return false;

            controlPath.Add(this.path[start]);
            start++;

            for (int i = start; i < this.path.Length; i++)
            {
                if (this.path[i].Center.DistanceTo(this.path[i - 1].Center) > 0.7)
                {
                    controlPath.Add(this.path[i]);
                }
            }

            //var last = controlPath.Last();
            while (controlPath.Count > 0)
            {
                Tetrahedron t = controlPath[controlPath.Count - 1];
                if (t.ContainsPoint(t.VoronoiCenter) || t.VoronoiCenter.DistanceTo(t.Center) < 3) break;
                //if (t.VoronoiCenter.DistanceTo(last.Center) < last.Clearance) break;

                controlPath.RemoveAt(controlPath.Count - 1);
            }

            if (Type == TunnelType.Pore)
            {
                controlPath = controlPath.SkipWhile(t => !t.ContainsPoint(t.VoronoiCenter) || t.VoronoiCenter.DistanceTo(t.Center) > 3).ToList();
            }

            if (controlPath.Count < 5) return false;
            double length;
            var splines = GetProfileSpline(controlPath, Cavity.Complex, out length);

            this.sX = splines[0];
            this.sY = splines[1];
            this.sZ = splines[2];
            this.sR = splines[3];
            this.sFreeR = splines[4];
            this.sBR = splines[5];
            this.Length = length;

            return true;
        }

        void ComputePhysicoChemicalProperties()
        {
            int count = 0;
            int charge = 0;
            int ionizable = 0;
            double hydropathy = 0.0;
            double hydrophobicity = 0.0;
            double polarity = 0.0;
            double logP = 0.0;
            double logD = 0.0;
            double logS = 0.0;
            //double hydratation = 0.0;
            double mutability = 0.0;
            int positives = 0;
            int negatives = 0;

            // count only side-chain residues
            foreach (var residue in GetLiningLayers().SelectMany(l => l.NonBackboneLining).Distinct())
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                var pc = info.Charge;
                charge += pc;
                ionizable += info.Ionizable;
                if (pc > 0)
                {
                    positives++;
                }
                else if (pc < 0)
                {
                    negatives++;
                }
                //hydropathy += info.Hydropathy;
                //hydratation += info.Hydratation;
                //hydrophobicity += info.Hydrophobicity;
                //polarity += info.Polarity;
                mutability += info.Mutability;
            }

            //hydropathy /= (double)count;
            //hydrophobicity /= (double)count;
            //polarity /= (double)count;
            if (count == 0) mutability = 0;
            else mutability /= (double)count;

            PhysicoChemicalPropertyCalculation.CalculateHydrophibilicyPolarityHydropathy(GetLiningLayers(), out hydrophobicity, out polarity, out hydropathy, out logP, out logD, out logS);

            double wHydropathy = 0.0;
            double wHydrophobicity = 0.0;
            double wPolarity = 0.0;
            double wHydratation = 0.0;
            double wMutability = 0.0;
            double wLogP = 0.0;
            double wLogD = 0.0;
            double wLogS = 0.0;
            double totalWeight = 0.0;

            foreach (var l in GetLiningLayers())
            {
                var w = /*l.Radius * l.Radius +*/ (l.EndDistance - l.StartDistance);
                totalWeight += w;
                wHydropathy += w * l.PhysicoChemicalProperties.Hydropathy;
                //wHydratation += w * l.PhysicoChemicalProperties.Hydratation;
                wHydrophobicity += w * l.PhysicoChemicalProperties.Hydrophobicity;
                wPolarity += w * l.PhysicoChemicalProperties.Polarity;
                wMutability += w * l.PhysicoChemicalProperties.Mutability;
                wLogP += w * l.PhysicoChemicalProperties.LogP;
                wLogD += w * l.PhysicoChemicalProperties.LogD;
                wLogS += w * l.PhysicoChemicalProperties.LogS;
            }

            if (totalWeight > 0.00001)
            {
                wHydropathy /= totalWeight;
                wHydrophobicity /= totalWeight;
                wPolarity /= totalWeight;
                wHydratation /= totalWeight;
                wMutability /= totalWeight;
                wLogP /= totalWeight;
                wLogD /= totalWeight;
                wLogS /= totalWeight;
            }

            PhysicoChemicalProperties = new TunnelPhysicoChemicalProperties(
                Charge: charge,
                Ionizable: ionizable,
                Polarity: polarity,
                //Hydratation: hydratation,
                Hydrophobicity: hydrophobicity,
                Hydropathy: hydropathy,
                LogP: logP,
                LogD: logD,
                LogS: logS,
                Mutability: (int)mutability,
                NumNegatives: negatives,
                NumPositives: positives
            );

            LayerWeightedPhysicoChemicalProperties = new TunnelPhysicoChemicalProperties(
                Charge: charge,
                Ionizable: ionizable,
                Polarity: wPolarity,                
                //Hydratation: wHydratation,
                Hydrophobicity: wHydrophobicity,
                Hydropathy: wHydropathy,
                LogP: wLogP,
                LogD: wLogD,
                LogS: wLogS,
                Mutability: (int)wMutability,
                NumNegatives: negatives,
                NumPositives: positives
            );

            //Lining.Where(r => r.IsB)
        }

        static void ComputePhysicoChemicalProperties(TunnelLayer layer)
        {
            int count = 0;
            int charge = 0;
            int ionizable = 0;
            double hydropathy = 0.0;
            double hydrophobicity = 0.0;
            double logP = 0.0;
            double logD = 0.0;
            double logS = 0.0;
            double polarity = 0.0;
            double mutability = 0.0;
            int positives = 0;
            int negatives = 0;

            // count only side-chain residues
            foreach (var residue in layer.NonBackboneLining)
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                var pc = info.Charge;
                charge += pc;
                ionizable += info.Ionizable;
                if (pc > 0)
                {
                    positives++;
                }
                else if (pc < 0)
                {
                    negatives++;
                }
                //hydropathy += info.Hydropathy;
                //hydrophobicity += info.Hydrophobicity;
                //polarity += info.Polarity;
                mutability += info.Mutability;
            }

            //hydropathy /= (double)count;
            //hydrophobicity /= (double)count;
            //polarity /= (double)count;
            if (count > 0) mutability /= (double)count;

            PhysicoChemicalPropertyCalculation.CalculateHydrophibilicyPolarityHydropathy(layer.ToSingletonArray(), out hydrophobicity, out polarity, out hydropathy, out logP, out logD, out logS);

            layer.PhysicoChemicalProperties = new TunnelPhysicoChemicalProperties(
                Charge: charge,
                Ionizable: ionizable,
                Polarity: polarity,
                Hydrophobicity: hydrophobicity,
                Hydropathy: hydropathy,
                LogP: logP,
                LogD: logD,
                LogS: logS,
                Mutability: (int)mutability,
                NumNegatives: negatives,
                NumPositives: positives
            );

            //Lining.Where(r => r.IsB)
        }

        void FindHetResidues()
        {
            var tree = Cavity.Complex.Structure.InvariantKdAtomTree();
            var ids = GetProfile(1)
                .SelectMany(n => tree
                    .NearestRadius(n.Center, 1.2 * n.Radius)
                    .Where(a => a.Value.IsHetAtom() && !a.Value.IsWater())
                    .Select(a => a.Value.ResidueIdentifier()))
                .Concat(this.Path.SelectMany(t => t.Vertices.Where(a => a.Atom.IsHetAtom() && !a.Atom.IsWater()).Select(a => a.Atom.ResidueIdentifier())))
                .Distinct();

            HetResidues = ids.Select(id => Cavity.Complex.Structure.PdbResidues().FromIdentifier(id)).OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).ToArray();
        }

        /// <summary>
        /// Creates the tunnel.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="edges"></param>
        /// <param name="o"></param>
        /// <param name="cavity"></param>
        /// <returns></returns>
        internal static Tunnel Create(TunnelOrigin start, IEnumerable<Edge> edges, CavityOpening o, Cavity cavity)
        {
            Tunnel tunnel = new Tunnel();
            tunnel.Cavity = cavity;
            tunnel.StartPoint = start;
            tunnel.Opening = o;

            List<Tetrahedron> path = new List<Tetrahedron>() { start.Tetrahedron };
            var first = edges.First();

            var current = start.Tetrahedron;

            //tunnel.edges = edges.ToArray();

            edges.ForEach(e =>
            {
                current = e.Other(current);
                path.Add(current);
            });

            path = path.TakeWhile(p => !p.IsBoundary).ToList();

            if (cavity.Complex.Parameters.FilterTunnelBoundaryLayers)
            {
                int bc = 0;
                var newPath = new List<Tetrahedron>();
                foreach (var t in path)
                {
                    if (!cavity.HasBoundaryVertex(t) || t.DepthLength > 3) newPath.Add(t);
                    else
                    {
                        bc++;
                        if (bc <= 1) newPath.Add(t);
                        else break;
                    }
                }
                path = newPath;
            }

            if (path.Count == 0) return null;

            tunnel.path = path.ToArray();

            bool ok = tunnel.CalculateProfile();
            if (!ok) return null;
            //tunnel.DefaultProfile = tunnel.GetProfile(defaultProfileDensity);

            tunnel.ComputePhysicoChemicalProperties();
            tunnel.FindHetResidues();

            return tunnel;
        }

        static List<Tetrahedron> FilterPoreBoundaryLayers(List<Tetrahedron> path, Cavity cavity)
        {
            int bc = 0;
            var newPath = new List<Tetrahedron>();
            foreach (var t in path)
            {
                if (!cavity.HasBoundaryVertex(t) || t.DepthLength > 3) newPath.Add(t);
                else
                {
                    bc++;
                    if (bc <= 1) newPath.Add(t);
                    else break;
                }
            }

            newPath.Reverse();
            var ret = new List<Tetrahedron>();
            ret.AddRange(newPath.Take(2));
            ret.AddRange(newPath.Skip(2).TakeWhile(t => t.DepthLength <= 3));
            bc = 0;
            foreach (var t in newPath.Skip(2).SkipWhile(t => t.DepthLength <= 3))
            {
                if (!cavity.HasBoundaryVertex(t) || t.DepthLength > 3) ret.Add(t);
                else
                {
                    bc++;
                    if (bc <= 1) ret.Add(t);
                    else break;
                }
            }
            ret.Reverse();
            return ret;
        }

        /// <summary>
        /// Creates a pore tunnel.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="edges"></param>
        /// <param name="o"></param>
        /// <param name="cavity"></param>
        /// <returns></returns>
        internal static Tunnel CreatePore(CavityOpening start, IEnumerable<Edge> edges, CavityOpening end, Cavity cavity)
        {
            Tunnel tunnel = new Tunnel();
            tunnel.Cavity = cavity;
            tunnel.StartPoint = null;
            tunnel.Opening = end;
            tunnel.Type = TunnelType.Pore;

            List<Tetrahedron> path = new List<Tetrahedron>() { start.Pivot };
            var first = edges.First();

            var current = start.Pivot;

            //tunnel.edges = edges.ToArray();

            edges.ForEach(e =>
            {
                current = e.Other(current);
                path.Add(current);
            });

            int tl;
            for (tl = path.Count - 1; tl >= 0; tl--)
            {
                if (!path[tl].IsBoundary) break;
            }

            if (tl < 10) return null;

            path = path.Take(tl + 1).ToList();

            if (cavity.Complex.Parameters.FilterTunnelBoundaryLayers)
            {
                path = FilterPoreBoundaryLayers(path, cavity);
            }

            if (path.Count == 0) return null;

            if (path.Max(t => t.Depth) < 6) return null;

            tunnel.path = path.ToArray();

            bool ok = tunnel.CalculateProfile();
            if (!ok) return null;
            //tunnel.DefaultProfile = tunnel.GetProfile(defaultProfileDensity);

            tunnel.ComputePhysicoChemicalProperties();
            tunnel.FindHetResidues();

            return tunnel;
        }

        internal static Tunnel CreateMergedPore(CavityOpening start, IEnumerable<Tetrahedron> tetras, CavityOpening end, Cavity cavity)
        {
            Tunnel tunnel = new Tunnel();
            tunnel.Cavity = cavity;
            tunnel.StartPoint = null;
            tunnel.Opening = end;
            tunnel.Type = TunnelType.Pore;
            tunnel.IsMergedPore = true;

            List<Tetrahedron> path = new List<Tetrahedron>() { start.Pivot };
            //var first = edges.First();

            //var current = start.Pivot;

            ////tunnel.edges = edges.ToArray();

            //edges.ForEach(e =>
            //{
            //    current = e.Other(current);
            //    path.Add(current);
            //});

            //int tl;
            //for (tl = path.Count - 1; tl >= 0; tl--)
            //{
            //    if (!path[tl].IsBoundary) break;
            //}

            //if (tl < 10) return null;

            path = tetras.ToList(); //path.Take(tl + 1).ToList();

            if (path.Count == 0) return null;

            if (path.Max(t => t.Depth) < 6) return null;

            tunnel.path = path.ToArray();

            bool ok = tunnel.CalculateProfile();
            if (!ok) return null;
            //tunnel.DefaultProfile = tunnel.GetProfile(defaultProfileDensity);

            tunnel.ComputePhysicoChemicalProperties();
            tunnel.FindHetResidues();

            return tunnel;
        }

        internal static Tunnel CreatePath(Tetrahedron start, Tetrahedron end, IEnumerable<Edge> edges, Cavity cavity)
        {
            Tunnel tunnel = new Tunnel();
            tunnel.Cavity = cavity;
            tunnel.StartPoint = null;
            tunnel.Opening = null;
            tunnel.Type = TunnelType.Path;

            List<Tetrahedron> path = new List<Tetrahedron>() { start };
            var first = edges.First();

            var current = start;

            //tunnel.edges = edges.ToArray();

            edges.ForEach(e =>
            {
                current = e.Other(current);
                path.Add(current);
            });

            int tl;
            for (tl = path.Count - 1; tl >= 0; tl--)
            {
                if (!path[tl].IsBoundary) break;
            }

            if (tl < 10) return null;

            path = path.Take(tl + 1).ToList();

            if (path.Count == 0) return null;

            if (path.Max(t => t.Depth) < 6) return null;

            tunnel.path = path.ToArray();

            bool ok = tunnel.CalculateProfile();
            if (!ok) return null;
            //tunnel.DefaultProfile = tunnel.GetProfile(defaultProfileDensity);

            tunnel.ComputePhysicoChemicalProperties();
            tunnel.FindHetResidues();

            return tunnel;
        }

        public void ComputeFields(double meshDensity)
        {
            var iso = IsoSurface.Create(this, meshDensity);
            var cps = Profile.ToArray();
            var ts = cps.Select(n => n.T).ToArray();
            var lining = GetLiningLayers();

            foreach (var f in Cavity.Complex.ScalarFields)
            {
                var surface = SurfaceScalarField.FromField(iso, f);

                var currentLayerIndex = 0;
                double[] values = new double[cps.Length];
                int index = 0;
                foreach (var node in cps)
                {
                    var layer = lining[currentLayerIndex];
                    if (node.Distance > layer.EndDistance)
                    {
                        currentLayerIndex = Math.Min(currentLayerIndex + 1, lining.Count - 1);
                        layer = lining[currentLayerIndex];
                    }
                    var v = f.Interpolate(node, layer);
                    if (v.HasValue) values[index] = v.Value;
                    index++;
                }

                var spline = new CubicSplineInterpolation(ts, values);

                ScalarFields[f.Name] = new TunnelScalarField(this, surface, spline, values.Min(), values.Max());
            }
        }

        public IStructure ToPdbStructure(double densityPerAngstrom, int atomIdOffset, Vector3D offset)
        {
            var atoms = GetProfile(densityPerAngstrom)
                .Select((n, i) => PdbAtom.Create(
                    atomIdOffset + i + 1,
                    ElementSymbols.Empty,
                    serialNumber: atomIdOffset + i + 1,
                    name: "X",
                    residueName: "TUN",
                    chainIdentifier: Type == TunnelType.Path ? "P" : (Type == TunnelType.Pore ? "T" : "H"),
                    residueSequenceNumber: this.id,
                    occupancy: n.Distance,
                    temperatureFactor: n.Radius,
                    position: n.Center + offset));
            return Structure.Create("tunnel" + this.Id, atoms.ToAtomCollection());
        }

        public XElement ToXml(Vector3D offset = new Vector3D(), int? customId = null, string includeCharge = null)
        {
            Func<TunnelLayer, int, string> rd = (l, i) =>
            {
                string ret = "";
                if (i < l.Lining.Count)
                {
                    var r = l.Lining[i];
                    ret = r.ToString();
                    if (l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r)) ret += " Backbone";
                }
                return ret;
            };

            Func<TunnelLayer, int, bool> isBackbone = (l, i) =>
            {
                if (i < l.Lining.Count)
                {
                    var r = l.Lining[i];
                    if (l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r)) return true;
                }
                return false;
            };

            Func<FlowResidue, string> frs = r => r.IsBackbone ? r.Residue.ToString() + " Backbone" : r.Residue.ToString();

            var name = this.Type.ToString();

            string soName = name.ToUpper();  //isPores ? "PORE" : "TUNNEL";
            string nodeName = name; //isPores ? "Pore" : "Tunnel";
            string filePrefix = name.ToLower() + "_";  // isPores ? "pore_" : "tunnel_";

            var profileNode = new XElement("Profile");
            var ctp = Profile;

            Dictionary<TunnelProfile.Node, double> charge = null;
            if (includeCharge != null)
            {
                charge = ScalarFields[includeCharge].GetValues(Profile);
            }

            ctp.Select(p =>
            {
                var ret = new XElement("Node",
                    new XAttribute("Radius", p.Radius.ToString("0.000", CultureInfo.InvariantCulture)),
                    new XAttribute("FreeRadius", p.FreeRadius.ToString("0.000", CultureInfo.InvariantCulture)),
                    new XAttribute("BRadius", p.BRadius.ToString("0.000", CultureInfo.InvariantCulture)),
                    new XAttribute("T", p.T.ToString("0.00000", CultureInfo.InvariantCulture)),
                    new XAttribute("Distance", p.Distance.ToString("0.000", CultureInfo.InvariantCulture)),
                    new XAttribute("X", (p.Center.X + offset.X).ToString("0.000", CultureInfo.InvariantCulture)),
                    new XAttribute("Y", (p.Center.Y + offset.Y).ToString("0.000", CultureInfo.InvariantCulture)),
                    new XAttribute("Z", (p.Center.Z + offset.Z).ToString("0.000", CultureInfo.InvariantCulture)));

                if (includeCharge != null) ret.Add(new XAttribute("Charge", charge[p].ToStringInvariant("0.000")));

                return ret;
            })
                .ForEach(n => profileNode.Add(n));

            var layers = GetLiningLayers();
            var bneck = layers.Count() > 1 ? layers.Skip(1).MinBy(l => l.Radius)[0] : null;

            var props = LayerWeightedPhysicoChemicalProperties;
            var wPropertiesNode = new XElement("LayerWeightedProperties",
                //new XAttribute("Hydratation", props.Hydratation.ToStringInvariant("0.00")),
                new XAttribute("Hydrophobicity", props.Hydrophobicity.ToStringInvariant("0.00")),
                new XAttribute("Hydropathy", props.Hydropathy.ToStringInvariant("0.00")),
                new XAttribute("LogP", props.LogP.ToStringInvariant("0.00")),
                new XAttribute("LogD", props.LogD.ToStringInvariant("0.00")),
                new XAttribute("LogS", props.LogS.ToStringInvariant("0.00")),
                new XAttribute("Polarity", props.Polarity.ToStringInvariant("0.00")),
                new XAttribute("Mutability", props.Mutability));

            var layersNode = new XElement("Layers");
            var rflow = new XElement("ResidueFlow", string.Join(",", layers.ResidueFlow.Select(r => frs(r)).ToArray()));
            var hetResiduesNode = new XElement("HetResidues", string.Join(",", HetResidues.Select(r => r.ToString()).ToArray()));
            layersNode.Add(rflow);
            layersNode.Add(hetResiduesNode);
            layersNode.Add(wPropertiesNode);
            layers
                .Select(l =>
                {
                    var asFlow = l.Lining
                        .Select((r, i) => new FlowResidue(r, isBackbone(l, i)))
                        .OrderBy(r => layers.GetFlowIndex(r))
                        .ToArray();

                    var ret = object.ReferenceEquals(l, bneck) ?
                        new XElement("Layer",
                            new XAttribute("MinRadius", l.Radius.ToStringInvariant("0.00000")),
                            new XAttribute("MinFreeRadius", l.FreeRadius.ToStringInvariant("0.00000")),
                            new XAttribute("MinBRadius", l.BRadius.ToStringInvariant("0.00000")),
                            new XAttribute("StartDistance", l.StartDistance.ToStringInvariant("0.00000")),
                            new XAttribute("EndDistance", l.EndDistance.ToStringInvariant("0.00000")),
                            new XAttribute("Bottleneck", "1"))
                        :
                        new XElement("Layer",
                            new XAttribute("MinRadius", l.Radius.ToStringInvariant("0.00000")),
                            new XAttribute("MinFreeRadius", l.FreeRadius.ToStringInvariant("0.00000")),
                            new XAttribute("MinBRadius", l.BRadius.ToStringInvariant("0.00000")),
                            new XAttribute("StartDistance", l.StartDistance.ToStringInvariant("0.00000")),
                            new XAttribute("EndDistance", l.EndDistance.ToStringInvariant("0.00000")));

                    ret.Add(new XAttribute("LocalMinimum", l.IsLocalMinimum ? "1" : "0"));

                    var sr = string.Join(",", asFlow.Select(r => frs(r)).ToArray());//  l.Lining.Select((r, i) => rd(l, i)).ToArray());
                    ret.Add(new XElement("Residues", sr));

                    var flowIndices = string.Join(",", asFlow.Select(r => layers.GetFlowIndex(r).ToString()).ToArray());
                    ret.Add(new XElement("FlowIndices", flowIndices));

                    //l.Lining.Select((r, i) => frs(new TunnelLining.FlowResidue(r, isBackbone(l, i))));

                    var prps = l.PhysicoChemicalProperties;
                    var pn = new XElement("Properties",
                        new XAttribute("Charge", prps.Charge),
                        new XAttribute("Ionizable", prps.Ionizable),
                        new XAttribute("NumPositives", prps.NumPositives),
                        new XAttribute("NumNegatives", prps.NumNegatives),
                        //new XAttribute("Hydratation", prps.Hydratation.ToStringInvariant("0.00")),
                        new XAttribute("Hydrophobicity", prps.Hydrophobicity.ToStringInvariant("0.00")),
                        new XAttribute("Hydropathy", prps.Hydropathy.ToStringInvariant("0.00")),
                        new XAttribute("LogP", prps.LogP.ToStringInvariant("0.00")),
                        new XAttribute("LogD", prps.LogD.ToStringInvariant("0.00")),
                        new XAttribute("LogS", prps.LogS.ToStringInvariant("0.00")),
                        new XAttribute("Polarity", prps.Polarity.ToStringInvariant("0.00")),
                        new XAttribute("Mutability", prps.Mutability));
                    ret.Add(pn);

                    return ret;
                })
                .ForEach(l => layersNode.Add(l));

            props = PhysicoChemicalProperties;
            var propertiesNode = new XElement("Properties",
                new XAttribute("Charge", props.Charge),
                new XAttribute("Ionizable", props.Ionizable),
                new XAttribute("NumPositives", props.NumPositives),
                new XAttribute("NumNegatives", props.NumNegatives),
                //new XAttribute("Hydratation", props.Hydratation.ToStringInvariant("0.00")),
                new XAttribute("Hydrophobicity", props.Hydrophobicity.ToStringInvariant("0.00")),
                new XAttribute("Hydropathy", props.Hydropathy.ToStringInvariant("0.00")),
                new XAttribute("LogP", props.LogP.ToStringInvariant("0.00")),
                new XAttribute("LogD", props.LogD.ToStringInvariant("0.00")),
                new XAttribute("LogS", props.LogS.ToStringInvariant("0.00")),
                new XAttribute("Polarity", props.Polarity.ToStringInvariant("0.00")),
                new XAttribute("Mutability", props.Mutability));

            XElement rootNode = null;

            switch (Type)
            {
                case TunnelType.Pore:
                    rootNode = new XElement(nodeName,
                        new XAttribute("Id", customId ?? Id),
                        //new XAttribute("IsCloseToSurface", IsCloseToSurface ? "1" : "0"),
                        //new XAttribute("CavityType", Cavity.Type.ToString()),
                        new XAttribute("Cavity", Cavity.Id.ToString()),
                        new XAttribute("Auto", IsMergedPore ? "0" : "1"),
                        propertiesNode,
                        profileNode,
                        layersNode);
                    break;
                case TunnelType.Tunnel:
                    rootNode = new XElement(nodeName,
                        new XAttribute("Id", customId ?? Id),
                        //new XAttribute("IsCloseToSurface", IsCloseToSurface ? "1" : "0"),
                        //new XAttribute("CavityType", Cavity.Type.ToString()),
                        new XAttribute("Cavity", Cavity.Id.ToString()),
                        new XAttribute("Auto", StartPoint.Type == TunnelOriginType.Computed ? "1" : "0"),
                        propertiesNode,
                        profileNode,
                        layersNode);
                    break;
                case TunnelType.Path:
                    rootNode = new XElement(nodeName,
                        new XAttribute("Id", customId ?? Id),
                        propertiesNode,
                        profileNode,
                        layersNode);
                    break;
            }

            return rootNode;
        }

        public object ToJson(Vector3D offset = new Vector3D(), int? customId = null, string includeCharge = null)
        {
            Func<TunnelLayer, int, string> rd = (l, i) =>
            {
                string ret = "";
                if (i < l.Lining.Count)
                {
                    var r = l.Lining[i];
                    ret = r.ToString();
                    if (l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r)) ret += " Backbone";
                }
                return ret;
            };

            Func<TunnelLayer, int, bool> isBackbone = (l, i) =>
            {
                if (i < l.Lining.Count)
                {
                    var r = l.Lining[i];
                    if (l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r)) return true;
                }
                return false;
            };

            Func<FlowResidue, string> frs = r => r.IsBackbone ? r.Residue.ToString() + " Backbone" : r.Residue.ToString();

            var name = this.Type.ToString();

            string soName = name.ToUpper();  //isPores ? "PORE" : "TUNNEL";
            string nodeName = name; //isPores ? "Pore" : "Tunnel";
            string filePrefix = name.ToLower() + "_";  // isPores ? "pore_" : "tunnel_";

            var ctp = this.Profile;

            Dictionary<TunnelProfile.Node, double> charge = null;
            if (includeCharge != null)
            {
                charge = ScalarFields[includeCharge].GetValues(this.Profile);
            }

            var Profile = ctp.Select(p => new
            {
                Radius = Math.Round(p.Radius, 3),
                FreeRadius = Math.Round(p.FreeRadius, 3),
                BRadius = Math.Round(p.BRadius, 3),
                T = Math.Round(p.T, 6),
                Distance = Math.Round(p.Distance, 3),
                X = Math.Round(p.Center.X + offset.X, 3),
                Y = Math.Round(p.Center.Y + offset.Y, 3),
                Z = Math.Round(p.Center.Z + offset.Z, 3),
                Charge = includeCharge != null ? Math.Round(charge[p], 3) : 0.0
            }).ToArray();

            var layers = GetLiningLayers();
            var bneck = layers.Count() > 1 ? layers.Skip(1).MinBy(l => l.Radius)[0] : null;

            var props = LayerWeightedPhysicoChemicalProperties;
            var Layers = new
            {
                ResidueFlow = layers.ResidueFlow.Select(r => frs(r)).ToArray(),
                HetResidues = HetResidues.Select(r => r.ToString()).ToArray(),
                LayerWeightedProperties = new
                {
                    Hydrophobicity = Math.Round(props.Hydrophobicity, 2),
                    Hydropathy = Math.Round(props.Hydropathy, 2),
                    Polarity = Math.Round(props.Polarity, 2),
                    LogP = Math.Round(props.LogP, 2),
                    LogD = Math.Round(props.LogD, 2),
                    LogS = Math.Round(props.LogS, 2),
                    Mutability = props.Mutability
                },
                LayersInfo = layers
                    .Select(l =>
                    {
                        var asFlow = l.Lining
                            .Select((r, i) => new FlowResidue(r, isBackbone(l, i)))
                            .OrderBy(r => layers.GetFlowIndex(r))
                            .ToArray();

                        return new
                        {
                            LayerGeometry = object.ReferenceEquals(l, bneck) ?
                                (object)new
                                {
                                    MinRadius = Math.Round(l.Radius, 3),
                                    MinFreeRadius = Math.Round(l.FreeRadius, 3),
                                    MinBRadius = Math.Round(l.BRadius, 3),
                                    StartDistance = Math.Round(l.StartDistance, 3),
                                    EndDistance = Math.Round(l.EndDistance, 3),
                                    LocalMinimum = l.IsLocalMinimum,
                                    Bottleneck = true
                                } :
                                (object)new
                                {
                                    MinRadius = Math.Round(l.Radius, 3),
                                    MinFreeRadius = Math.Round(l.FreeRadius, 3),
                                    MinBRadius = Math.Round(l.BRadius, 3),
                                    StartDistance = Math.Round(l.StartDistance, 3),
                                    EndDistance = Math.Round(l.EndDistance, 3),
                                    LocalMinimum = l.IsLocalMinimum,

                                },
                            Residues = asFlow.Select(r => frs(r)).ToArray(),
                            FlowIndices = asFlow.Select(r => layers.GetFlowIndex(r).ToString()).ToArray(),
                            Properties = l.PhysicoChemicalProperties.ToJson()
                        };
                    }).ToArray()
            };

            props = PhysicoChemicalProperties;
            var Properties = PhysicoChemicalProperties.ToJson();


            bool auto = false;
            if (Type == TunnelType.Pore) auto = IsMergedPore;
            else if (Type == TunnelType.Pore) auto = StartPoint.Type == TunnelOriginType.Computed;

            //var surface = this.GetSurface(1.33);

            return new
            {
                Type = nodeName,
                Id = this.Id.ToString(),
                Fingerprint = GetTunnelFp(this),
                Cavity = Cavity != null ? Cavity.Id.ToString() : null,
                Auto = auto,
                Properties = Properties,
                Profile = Profile,
                Layers = Layers
                //Mesh = surface.ToJson()
            };
        }

        private string GetTunnelFp(Tunnel t)
        {
            return "L" + t.Length.ToString("0.00");
        }

        /// <summary>
        /// 
        /// </summary>
        private Tunnel()
        {
            ScalarFields = new Dictionary<string, TunnelScalarField>();
        }

        /// <summary>
        /// Compares the lining residues. 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Tunnel other)
        {
            if (other == null) return false;

            if (this.lining.Count() != other.lining.Count()) return false;

            var ret = Enumerable.Zip(this.lining, other.lining, (l, r) => l.Identifier == r.Identifier).All(e => e);
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Compares the lining residues.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Tunnel)
                return Equals(obj as Tunnel);
            return false;
        }
    }
}