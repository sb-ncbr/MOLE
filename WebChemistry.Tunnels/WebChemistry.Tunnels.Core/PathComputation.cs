/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Collections.Generic;
    using QuickGraph.Algorithms;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
        
    public partial class Complex
    {
        IEnumerable<Tunnel> GetPaths(IEnumerable<Tuple<Vector3D, Vector3D>> startAndEndPoints)
        {
            var graph = SurfaceCavity.CavityGraph;

            List<Tunnel> ret = new List<Tunnel>();

            var wf = MakeEdgeWeightFunction();
            foreach (var p in startAndEndPoints)
            {
                var a = SurfaceCavity.GetTetrahedron(p.Item1, Parameters.OriginRadius);
                var b = SurfaceCavity.GetTetrahedron(p.Item2, Parameters.OriginRadius);
                
                var paths = graph.ShortestPathsDijkstra(wf, a);
                IEnumerable<Edge> path;
                if (paths(b, out path))
                {
                    var t = Tunnel.CreatePath(a, b, path, SurfaceCavity);
                    if (t != null) ret.Add(t);
                }
            }

            return ret;
        }

        IEnumerable<Tunnel> GetPaths(IEnumerable<Tuple<Tetrahedron, Tetrahedron>> startAndEndPoints)
        {
            var graph = SurfaceCavity.CavityGraph;

            List<Tunnel> ret = new List<Tunnel>();

            var wf = MakeEdgeWeightFunction();
            foreach (var p in startAndEndPoints)
            {
                var a = p.Item1;
                var b = p.Item2;

                var paths = graph.ShortestPathsDijkstra(wf, a);
                IEnumerable<Edge> path;
                if (paths(b, out path))
                {
                    var t = Tunnel.CreatePath(a, b, path, SurfaceCavity);
                    if (t != null) ret.Add(t);
                }
            }

            return ret;
        }

        public void ComputePaths(IEnumerable<Tuple<Tetrahedron, Tetrahedron>> startAndEndPoints)
        {
            var paths = GetPaths(startAndEndPoints);
            Paths.AddRange(paths);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startAndEndPoints"></param>
        /// <returns></returns>
        public Computation ComputePathsAsync(IEnumerable<Tuple<Vector3D, Vector3D>> startAndEndPoints)
        {
            return Computation.Create(() =>
            {
                var paths = GetPaths(startAndEndPoints);
                Paths.AddRange(paths);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startAndEndPoints"></param>
        public void ComputePaths(IEnumerable<Tuple<Vector3D, Vector3D>> startAndEndPoints)
        {
            ComputePathsAsync(startAndEndPoints).RunSynchronously();
        }
    }
}
