/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using QuickGraph;

    /// <summary>
    /// Usable extension methods.
    /// </summary>
    static class Extensions
    {
        /// <summary>
        /// Enumerates adjacent vertices.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public static IEnumerable<Tetrahedron> AdjacentVertices(this UndirectedGraph<Tetrahedron, Edge> graph, Tetrahedron vertex)
        {
            var list = graph.AdjacentEdges(vertex) as List<Edge>;
            for (int i = 0; i < list.Count; i++)
            {
                yield return list[i].GetOtherVertex(vertex);
            }
        }

        public static List<Edge> AdjacentEdgeList(this UndirectedGraph<Tetrahedron, Edge> graph, Tetrahedron vertex)
        {
            return graph.AdjacentEdges(vertex) as List<Edge>;
        }
    }
}
