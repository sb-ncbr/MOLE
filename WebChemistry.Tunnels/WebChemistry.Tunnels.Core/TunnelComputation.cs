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

    public partial class Complex
    {
        public Func<Edge, double> MakeEdgeWeightFunction()
        {
            switch (Parameters.WeightFunction)
            {
                case TunnelWeightFunction.LengthAndRadius: return e => e.Weight;
                case TunnelWeightFunction.Length: return e => e.Length;
                case TunnelWeightFunction.Constant: return _ => 1;
                case TunnelWeightFunction.VoronoiScale: return e => e.VoronoiWeight;
                default: throw new InvalidOperationException("Unknown weight function.");
            }
        }

        bool FilterBottleneck(Tunnel t)
        {
            var profile = t.Profile.SkipWhile(n => n.Distance < Parameters.BottleneckRadius).ToArray();

            if (profile.Length == 0) return true;

            if (Parameters.BottleneckTolerance == 0.0)
            {
                return !profile.Any(n => n.Radius < Parameters.BottleneckRadius);
            }

            double? start = profile.First().Radius < Parameters.BottleneckRadius ? 0.0 : (double?)null;
            foreach (var n in profile.Skip(1))
            {
                if (n.Radius < Parameters.BottleneckRadius)
                {
                    if (start.HasValue)
                    {
                        if (n.Distance - start.Value < Parameters.BottleneckTolerance) continue;
                        else return false;
                    }
                    else
                    {
                        start = n.Distance;
                    }
                }
                else
                {
                    start = null;
                }
            }

            return true;
        }

        bool RemoveLonger(Tunnel shorter, K3DTree<TunnelProfile.Node> longerTree)
        {
            return (double)shorter.GetProfile(6).Count(n => longerTree.Nearest(n.Center).Priority <= 1)
                / (double)shorter.GetProfile(6).Count > Parameters.MaxTunnelSimilarity;
        }

        //bool RemoveLonger(Tunnel shorter, Tunnel longer)
        //{
        //    var dx = 1.0 / 100.0;
        //    int closeCount = 0;
        //    for (int i = 0; i <= 100; i++)
        //    {
        //        double t = dx * i;
        //        if (shorter.GetCenterlinePoint(t).DistanceToSquared(longer.GetCenterlinePoint(t)) <= 1) closeCount++;
        //    }

        //    return (double)closeCount / 100.0 > Parameters.MaxTunnelSimilarity;
        //}

        IEnumerable<Tunnel> GetTunnels(TunnelOrigin from)
        {
            // Find the tunnels that are already computed.
            var forbidden = Tunnels.Where(t => t.StartPoint == from).ToArray();

            // sources are all cavities that contain the suggestion ball
            // this is one cavity and the the "whole molecule", which is included 
            // because of custom exits
            var sources = Cavities
                .Where(ch => ch.Tetrahedrons.Contains(from.Tetrahedron))
                .Concat(EnumerableEx.Return(SurfaceCavity));

            // Compute all tunnels that are not already computed
            var wf = MakeEdgeWeightFunction();

            var ret =
                sources.SelectMany(s =>
                {
                    var openings = Parameters.UseCustomExitsOnly ? s.Openings.Where(o => o.IsUser).ToArray() : s.Openings.ToArray();

                    if (openings.Length == 0) return Enumerable.Empty<Tunnel>();

                    var paths = s.CavityGraph.ShortestPathsDijkstra(wf, from.Tetrahedron);
                    return openings.Where(p => !forbidden.Any(f => f.Opening == p)).SelectMany(o =>
                    {
                        IEnumerable<Edge> path;
                        if (paths(o.Pivot, out path))
                        {
                            var t = Tunnel.Create(from, path, o, s);
                            return t != null ? EnumerableEx.Return(t) : Enumerable.Empty<Tunnel>();
                        }
                        else return Enumerable.Empty<Tunnel>();
                    });
                })
                .Where(t => FilterBottleneck(t))
                .ToList();

            //for (int i = 0; i < tunnels.Length - 1; i++)
            //{
            //    bool isGood = true;
            //    for (int j = i + 1; j < tunnels.Length; j++)
            //    {
            //        if (tunnels[i].Equals(tunnels[j]))
            //        {
            //            isGood = false;
            //            break;
            //        }
            //    }

            //    if (isGood) ret.Add(tunnels[i]);
            //}
            //if (tunnels.Count() > 0) ret.Add(tunnels.Last());

            return FilterTunnels(forbidden, ret);
        }

        private IEnumerable<Tunnel> FilterTunnels(Tunnel[] forbidden, List<Tunnel> ret)
        {
            ret.ForEach((t, i) => t.Id = i);
            var tunnelsById = ret.ToDictionary(t => t.Id);

            var tunnelTrees = new Func<int, K3DTree<TunnelProfile.Node>>(id => K3DTree.Create(tunnelsById[id].GetProfile(2), n => n.Center)).Memoize();
            Func<Tunnel, Tunnel, double> tunnelRmsd = (a, b) =>
            {
                var tree = tunnelTrees(b.Id);
                return Math.Sqrt(a.GetProfile(2)
                                  .Select(n => n.Center.DistanceToSquared(tree.Nearest(n.Center).Value.Center))
                                  .Average());
            };

            Func<Tunnel, Tunnel, double> tunnelDistance = (a, b) => 0.5 * (tunnelRmsd(a, b) + tunnelRmsd(b, a));

            // Check for tunnel similarity
            var ordered = ret.OrderBy(t => t.Path.Length).ToArray();
            HashSet<Tunnel> toRemove = new HashSet<Tunnel>();


            // first stage filter
            for (int i = 0; i < ordered.Length - 1; i++)
            {
                var pivot = ordered[i];

                if (toRemove.Contains(pivot)) continue;

                //if (!pivot.AutomaticallyDisplay) continue;

                var max = pivot.Path.Length;

                for (int j = i + 1; j < ordered.Length; j++)
                {
                    var other = ordered[j];

                    if (toRemove.Contains(other)) continue;

                    //if (pivot.Length / other.Length > 0.95)
                    //{
                    //    var d = tunnelDistance(pivot, other);
                    //    if (d < 0.5) toRemove.Add(other);
                    //}

                    if (RemoveLonger(pivot, tunnelTrees(other.Id)))
                    {
                        toRemove.Add(other);
                    }
                }
            }

            foreach (var t in toRemove) ret.Remove(t);

            ordered = ret.OrderBy(t => t.Path.Length).ToArray();
            foreach (var t in ordered) t.AutomaticallyDisplay = true;

            for (int i = 0; i < ordered.Length - 1; i++)
            {
                var pivot = ordered[i];

                //if (!pivot.AutomaticallyDisplay) continue;

                var max = pivot.Path.Length;

                for (int j = i + 1; j < ordered.Length; j++)
                {
                    var other = ordered[j];

                    //if (toRemove.Contains(other)) continue;

                    int commonCount;
                    for (commonCount = 0; commonCount < max; commonCount++)
                    {
                        if (pivot.Path[commonCount] != other.Path[commonCount]) break;
                    }

                    if ((double)commonCount / (double)max > 0.85)
                    {
                        other.AutomaticallyDisplay = false;
                    }
                }
            }

            // Add already computed tunnels
            ret.AddRange(forbidden);

            return FilterTunnels(ret);
        }

        /// <summary>
        /// Filter the tunnels based on user given criteria.
        /// </summary>
        /// <param name="tunnels"></param>
        /// <returns></returns>
        IEnumerable<Tunnel> FilterTunnels(List<Tunnel> tunnels)
        {
            return tunnels.Where(t => t.Type == TunnelType.Pore ? t.Length >= Parameters.MinPoreLength : t.Length >= Parameters.MinTunnelLength).ToList();
        }

        /// <summary>
        /// Creates a computation that computes all tunnels from the given origin. Tunnels are added to Comlex.Tunnel
        /// </summary>
        /// <param name="origin">Origin to return</param>
        /// <returns>Computation object representing the tunnel computation.</returns>
        public Computation ComputeTunnelsAsync(TunnelOrigin origin)
        {
            return Computation.Create(() =>
            {
                var tunnels = GetTunnels(origin);
                this.Tunnels.AddRange(tunnels);
            });
        }

        /// <summary>
        /// Computes all tunnels from the given origin. Tunnels are added to Comlex.Tunnel
        /// </summary>
        /// <param name="origin">Origin to return</param>
        /// <returns>Computation object representing the tunnel computation.</returns>
        public void ComputeTunnels(TunnelOrigin origin)
        {
            ComputeTunnelsAsync(origin).RunSynchronously();
        }
    }
}
