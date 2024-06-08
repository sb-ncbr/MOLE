/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Helpers
{
    using System.Collections.Generic;
    using System.IO;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
    using System.Linq;

    public class TunnelPdbExporter
    {
        TextWriter writer;
        Vector3D offset;
        IStructure parent;

        public TunnelPdbExporter WriteLine(string text)
        {
            writer.WriteLine(text);
            return this;
        }

        public TunnelPdbExporter WriteString(string text)
        {
            writer.Write(text);
            return this;
        }

        public TunnelPdbExporter WriteTunnels(IEnumerable<Tunnel> tunnels)
        {
            int atomIdOffset = parent.Atoms.Count > 0 ? parent.Atoms.Max(a => a.PdbSerialNumber()) : 0;
            foreach (var t in tunnels)
            {
                var s = t.ToPdbStructure(8, atomIdOffset, offset);
                atomIdOffset += s.Atoms.Count;
                s.WritePdb(writer);
            }
            return this;
        }

        public TunnelPdbExporter WriteTunnel(Tunnel t)
        {
            var s = t.ToPdbStructure(8, 0, offset);
            s.WritePdb(writer);
            return this;
        }

        public TunnelPdbExporter(TextWriter writer, IStructure parent, Vector3D offset = new Vector3D(), IEnumerable<string> remarks = null)
        {
            this.writer = writer;
            this.offset = offset;
            this.parent = parent;
            if (remarks == null) remarks = new string[0];

            foreach (var r in remarks) writer.WriteLine(r);
        }
    }
}
