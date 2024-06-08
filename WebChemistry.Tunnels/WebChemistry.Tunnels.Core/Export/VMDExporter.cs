using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.Core.Export
{
    public class VmdExporter
    {

        #region Variables
        public TextWriter Writer { get; set; }
        public Vector3D Offset { get; set; }

        private static string[] Header = new string[] {
            "To run this script please run it in VMD as source script.vmd",
            "",
            "If you find this tool useful for your work, please cite it as:",
            "Sehnal D, Svobodová Vařeková R, Berka K, Pravda L, Navrátilová V, Banáš P, Ionescu C-M, Otyepka M, Koča J: MOLE 2.0: advanced approach for analysis of biomacromolecular channels. J. Cheminform. 2013, 5:39.",
            ""
        };

        private static string[] Colors = new string[] {
            "blue","red","gray","orange","yellow","tan",
            "silver","green","white","pink","cyan","purple",
            "lime","mauve","ochre","iceblue","black","yellow2",
            "yellow3","green2","green3","cyan2","cyan3",
            "blue2","blue3","violet","violet2","magenta","magenta2",
            "red2","red3","orange2","orange3",
            };
        #endregion Variables


        #region Constructor
        public VmdExporter(TextWriter writer, Vector3D? offset = null, string[] header = null)
        {
            this.Writer = writer;
            this.Offset = offset.HasValue ? offset.Value : new Vector3D();

            writer.WriteLine("package require http");
            foreach (var l in header ?? Header)
            {
                writer.WriteLine("# " + l);
            }

            new string[] {
            "proc add_atom {id center r} {",
            " set atom [atomselect top \"index $id\"]",
            " $atom set {x y z}  $center",
            " $atom set radius $r",
            "}\n" }.ForEach(x => writer.WriteLine(x));
        }
        #endregion

        public VmdExporter AddFetch(string id)
        {
            new string[] {
                 "proc fetch_protein { url } {",
                 " set token [::http::geturl $url]",
                 " set data [::http::data $token]",
                 " ::http::cleanup $token",
                $" set fileName \"{id}.cif\"",
                 " set file [open $fileName \"w\"]",
                 " puts -nonewline $file $data",
                $" mol new \"{id}.cif\" type {{pdbx}}",
            "}\n" }.ForEach(x => Writer.WriteLine(x));

            Writer.WriteLine($"fetch_protein http://www.ebi.ac.uk/pdbe/entry-files/download/{id}.cif");
            return this;
        }

        public VmdExporter AddTunnels(IEnumerable<Tunnel> tunnels, string name = "", SurfaceType surfaceType = SurfaceType.Spheres)
        {
            tunnels.ForEach((t, i) => AddTunnel(name, i + 1, t, surfaceType));
            Writer.WriteLine("display resetview");

            return this;
        }

        public VmdExporter AddCavities(IEnumerable<Cavity> cavities)
        {
            cavities.ForEach((t, i) => AddCavity(i + 1, t));
            Writer.WriteLine("display resetview");

            return this;

        }

        public VmdExporter AddTunnel(string name, int id, Tunnel t, SurfaceType surfaceType)
        {

            Writer.WriteLine("set tun{0} [mol new atoms {1}]", id, t.Profile.Count);
            Writer.WriteLine("mol top $tun{0}", id);
            Writer.WriteLine("animate dup $tun{0}", id);
            Writer.WriteLine("mol color ColorID {0}", id % 26);
            Writer.WriteLine($"mol representation {(surfaceType == SurfaceType.Spheres ? "VDW 1 60" : "QuickSurf 0.4 1.0 1.0")}");

            for (int i = 0; i < t.Profile.Count; i++)
            {
                Writer.WriteLine("add_atom {0} {{{{ {1:0.00} {2:0.00} {3:0.00} }}}} {4:0.00}",
                    i, t.Profile[i].Center.X, t.Profile[i].Center.Y, t.Profile[i].Center.Z, t.Profile[i].Radius);
            }

            Writer.WriteLine($"mol delrep 0 $tun{id}");
            Writer.WriteLine($"mol addrep $tun{id}");
            Writer.WriteLine("mol selection {{all}}");
            Writer.WriteLine("mol rename top {{{0}{1}}}", (string.IsNullOrEmpty(name) ? "" : name + "_") + GetCommandName(t), id);
            Writer.WriteLine("");
            return this;
        }

        public VmdExporter AddCavity(int id, Cavity c)
        {
            {
                Writer.WriteLine($"mol load graphics cavity{id}");
                Writer.WriteLine("draw materials on");
                Writer.WriteLine("draw color {0}", Colors[id % Colors.Length]);
                Writer.WriteLine("draw material Opaque");

                foreach (var item in c.Boundary)
                {
                    Writer.WriteLine("draw triangle {{{0:0.00} {1:0.00} {2:0.00}}} {{{3:0.00} {4:0.00} {5:0.00}}} {{{6:0.00} {7:0.00} {8:0.00}}}",
                        item.Vertices[0].Position.X, item.Vertices[0].Position.Y, item.Vertices[0].Position.Z,
                        item.Vertices[1].Position.X, item.Vertices[1].Position.Y, item.Vertices[1].Position.Z,
                        item.Vertices[2].Position.X, item.Vertices[2].Position.Y, item.Vertices[2].Position.Z);
                }
                Writer.WriteLine("");

                return this;
            }
        }

        public VmdExporter AddCavities(string prefix, IEnumerable<Cavity> cavities)
        {
            cavities.ForEach((c, i) => AddCavity((i + 1), c));
            return this;
        }

        public string GetCommandName(Tunnel t)
        {
            if (t.Type == TunnelType.Pore) return t.IsMergedPore ? "MergedPore" : "Pore";
            return t.Type.ToString();
        }
    }
}
