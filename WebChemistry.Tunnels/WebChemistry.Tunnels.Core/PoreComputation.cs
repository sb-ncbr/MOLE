/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using QuickGraph.Algorithms;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Geometry;
    using System;

    class OpeningPositionComparer : IComparer<CavityOpening>
    {

        public int Compare(CavityOpening x, CavityOpening y)
        {
            var px = x.Pivot.Center;
            var py = y.Pivot.Center;

            if (px.X == py.X)
            {
                if (px.Y == py.Y)
                {
                    return px.Z.CompareTo(py.Z);
                }
                return px.Y.CompareTo(py.Y);
            }
            return px.X.CompareTo(py.X);
        }
    }

    public partial class Complex
    {
        IEnumerable<T> Pick<T>(List<T> xs, int n)
        {
            if (n <= 0) return Enumerable.Empty<T>();
            if (xs.Count < n) return xs;

            var r = (int)Math.Ceiling((double)(xs.Count) / (double)n);
            var ret = xs.Where((_, i) => i % r == 0).ToList();

            if (ret.Count == n) return ret;

            var rest = xs.Where((_, i) => i % r != 0).ToList();
            return ret.Concat(Pick(rest, n - ret.Count));
        }

        IEnumerable<Tunnel> GetPores(Cavity cavity, bool userOnly = false)
        {
            var exitCandidates = userOnly ? cavity.PoreExits.Where(e => e.IsUser).ToArray() : cavity.PoreExits.ToArray();

            if (exitCandidates.Length < 2) return new Tunnel[0];

            List<Tunnel> ret = new List<Tunnel>();

            List<CavityOpening> exits;

            int maxExits = 17;
            if (exitCandidates.Length > maxExits)
            {
                exits = Pick(exitCandidates.ToList(), maxExits).ToList();
            }
            else
            {
                exits = exitCandidates.ToList();
            }

            exits.Sort(new OpeningPositionComparer());

            var wf = MakeEdgeWeightFunction();
            for (int i = 0; i < exits.Count - 1; i++)
            {
                var a = exits[i];

                var paths = cavity.CavityGraph.ShortestPathsDijkstra(wf, a.Pivot);

                for (int j = i + 1; j < exits.Count; j++)
                {
                    var b = exits[j];

                    IEnumerable<Edge> path;

                    if (paths(b.Pivot, out path))
                    {
                        var t = Tunnel.CreatePore(a, path, b, cavity);
                        if (t != null) ret.Add(t);
                    }
                }
            }

            ret = ret.Where(t => FilterBottleneck(t)).ToList();
            ret = FilterTunnels(new Tunnel[0], ret).Take(30).ToList();
            return ret;
        }

        IEnumerable<Tunnel> GetPoresFromSameOrigin(IEnumerable<Tunnel> tunnels)
        {
            var ts = tunnels.ToArray();
            List<Tunnel> ret = new List<Tunnel>();

            for (int i = 0; i < ts.Length - 1; i++)
            {
                var x = ts[i];
                for (int j = i + 1; j < ts.Length; j++)
                {
                    var y = ts[j];

                    if (y.Path[y.Path.Length - 1].Center.DistanceTo(x.Path[x.Path.Length - 1].Center) > (x.Length + y.Length) / 2)
                    {
                        var path = x.Path.Reverse().Concat(y.Path.Skip(1));
                        var t = Tunnel.CreateMergedPore(x.Opening, path, y.Opening, x.Cavity);

                        if (t != null) ret.Add(t);
                    }
                }
            }

            return FilterTunnels(new Tunnel[0], ret.Where(t => FilterBottleneck(t)).ToList());
        }


        IEnumerable<Tunnel> PorifyInternal(IEnumerable<Tunnel> tunnels)
        {
            return tunnels
                .GroupBy(t => t.StartPoint)
                .SelectMany(ts => GetPoresFromSameOrigin(ts))
                .ToArray();
        }

        /// <summary>
        /// Creates a computation that computes all pores in a given cavity. Tunnels are added to Comlex.Pores
        /// </summary>
        /// <param name="cavity">In which cavity, if null computes everything</param>
        /// <returns>Computation object representing the pore computation.</returns>
        public Computation ComputePoresAsync(Cavity cavity = null)
        {
            return Computation.Create(() =>
            {
                if (cavity != null)
                {
                    var tunnels = GetPores(cavity);
                    this.Pores.AddRange(tunnels);
                }
                else
                {
                    foreach (var c in this.Cavities)
                    {
                        var tunnels = GetPores(c);
                        this.Pores.AddRange(tunnels);
                    }
                }
            });
        }

        /// <summary>
        /// Creates a computation that computes all pores in a given cavity. Tunnels are added to Comlex.Pores
        /// </summary>
        /// <returns>Computation object representing the pore computation.</returns>
        public Computation ComputeUserPoresAsync()
        {
            return Computation.Create(() =>
            {                
                foreach (var c in this.Cavities)
                {
                    var tunnels = GetPores(c, true);
                    this.Pores.AddRange(tunnels);
                }

                var sp = GetPores(this.SurfaceCavity, true);
                var filtered = FilterTunnels(this.Pores.ToArray(), sp.ToList());
                this.Pores.AddRange(filtered);
            });
        }

        /// <summary>
        /// Creates pores from tunnels.
        /// </summary>
        /// <param name="tunnels"></param>
        /// <returns></returns>
        public Computation PorifyAsync(IEnumerable<Tunnel> tunnels)
        {
            return Computation.Create(() =>
            {
                var pores = PorifyInternal(tunnels);
                this.Pores.AddRange(pores);
            });
        }

        /// <summary>
        /// Computes all pores in a given cavity. Pores are added to Comlex.Pores
        /// </summary>
        /// <param name="origin">Origin to return</param>
        /// <returns>Computation object representing the pore computation.</returns>
        public void ComputePores(Cavity cavity = null)
        {
            ComputePoresAsync(cavity).RunSynchronously();
        }

        public void ComputeUserPores()
        {
            ComputeUserPoresAsync().RunSynchronously();
        }

        /// <summary>
        /// Creates tunnels from pores.
        /// </summary>
        /// <param name="tunnels"></param>
        public void Porify(IEnumerable<Tunnel> tunnels)
        {
            PorifyAsync(tunnels).RunSynchronously();
        }
    }
}
