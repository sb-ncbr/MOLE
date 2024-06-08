/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using WebChemistry.Framework.Math;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// A class that represents a tunnel profile.
    /// </summary>
    public class TunnelProfile : IEnumerable<TunnelProfile.Node>
    {
        /// <summary>
        /// A profile node.
        /// </summary>
        public class Node
        {
            // Unique within a given profile.
            internal int Index;

            public override bool Equals(object obj)
            {
                var other = obj as Node;
                if (other == null) return false;
                return Index == other.Index;
            }

            public override int GetHashCode()
            {
                return Index;
            }

            /// <summary>
            /// Center of the node.
            /// </summary>
            public Vector3D Center { get; private set; }

            /// <summary>
            /// Approximate distance from the origin of the tunnel.
            /// </summary>
            public double Distance { get; private set; }

            /// <summary>
            /// Spline parameter.
            /// </summary>
            public double T { get; private set; }

            /// <summary>
            /// Radius of the node
            /// </summary>
            public double Radius { get; private set; }

            /// <summary>
            /// Radius to the closest backbone or HET atom.
            /// </summary>
            public double FreeRadius { get; private set; }

            public double BRadius { get; private set; }

            internal Node(double t, double distance, double radius, double freeRadius, double bradius, Vector3D center)
            {
                this.T = t;
                this.Distance = distance;
                this.Radius = radius;
                this.FreeRadius = freeRadius;
                this.BRadius = bradius;
                this.Center = center;
            }
        }

        Node[] nodes;

        /// <summary>
        /// Indexer.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Node this[int i]
        {
            get { return nodes[i]; }
        }

        /// <summary>
        /// Node count.
        /// </summary>
        public int Count
        {
            get { return nodes.Length; }
        }

        /// <summary>
        /// Density of the profile nodes.
        /// </summary>
        public double Density
        {
            get;
            private set;
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="density"></param>
        internal TunnelProfile(Node[] nodes, double density)
        {
            this.nodes = nodes;
            this.Density = density;
            this.nodes.ForEach((n, i) => n.Index = i);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TunnelProfile.Node> GetEnumerator()
        {
            return nodes.AsEnumerable().GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        /// <summary>
        /// Create CSV/XML exporter.
        /// </summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        public ListExporter<Node> CreateExporter(string separator = ",")
        {
            return nodes.GetExporter(separator)
               .AddExportableColumn(p => p.T.ToStringInvariant("0.000"), ColumnType.Number, "T")
               .AddExportableColumn(p => p.Distance.ToStringInvariant("0.000"), ColumnType.Number, "Distance")
               .AddExportableColumn(p => p.Radius.ToStringInvariant("0.000"), ColumnType.Number, "Radius")
               .AddExportableColumn(p => p.FreeRadius.ToStringInvariant("0.000"), ColumnType.Number, "FreeRadius")
               .AddExportableColumn(p => p.BRadius.ToStringInvariant("0.000"), ColumnType.Number, "BRadius")
               .AddExportableColumn(p => p.Center.X.ToStringInvariant("0.000"), ColumnType.Number, "X")
               .AddExportableColumn(p => p.Center.Y.ToStringInvariant("0.000"), ColumnType.Number, "Y")
               .AddExportableColumn(p => p.Center.Z.ToStringInvariant("0.000"), ColumnType.Number, "Z");
        }
    }
}
