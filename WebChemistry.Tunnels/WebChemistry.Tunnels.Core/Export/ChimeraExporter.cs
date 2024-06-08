using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.Core.Export
{
    public class ChimeraExporter
    {
        #region Variables
        public TextWriter Writer { get; set; }
        public Vector3D Offset { get; set; }

        private static string[] Header = new string[] {
            "To run this script please open it in Chimera using 'Open' command.",
            "",
            "If you find this tool useful for your work, please cite it as:",
            "Sehnal D, Svobodová Vařeková R, Berka K, Pravda L, Navrátilová V, Banáš P, Ionescu C-M, Otyepka M, Koča J: MOLE 2.0: advanced approach for analysis of biomacromolecular channels. J. Cheminform. 2013, 5:39.",
            ""
        };

        private static Dictionary<string, string> Colors = new Dictionary<string, string> {
            {"red", "(1,0,0,.5)" },
            {"orange red", "(1,0.27,0,.5)" },
            {"orange", "(1,0.5,0,.5)" },
            {"yellow", "(1,1,0,.5)" },
            {"green", "(0,1,0,.5)" },
            {"forest green", "(0.13,0.54,0.13,.5)" },
            {"cyan", "(0,1,1,.5)" },
            {"light sea green", "(0.12,0.7,0.66,.5)" },
            {"blue", "(0,0,1,.5)" },
            {"cornflower blue", "(0.39,0.58,0.93,.5)" },
            {"medium blue", "(0.2,0.2,0.8,.5)" },
            {"purple", "(0.63,0.13,0.94,.5)" },
            {"hot pink", "(1,0.41,0.7,.5)" },
            {"magenta", "(1,0,1,.5)" },
            {"spring green", "(0,1,0.5,.5)" },
            {"plum", "(0.86,0.62,0.86,.5)" },
            {"sky blue", "(0.53,0.81,0.92,.5)" },
            {"goldenrod", "(0.85,0.65,0.13,.5)" },
            {"olive drab", "(0.42,0.56,0.13,.5)" },
            {"coral", "(1,0.5,0.31,.5)" },
            {"rosy brown", "(0.74,0.56,0.56,.5)" },
            {"slate gray", "(0.44,0.5,0.57,.5)" }};
        #endregion Variables

        #region Constructor

        public ChimeraExporter(TextWriter writer, Vector3D? offset = null, string[] header = null)
        {
            this.Writer = writer;
            this.Offset = offset.HasValue ? offset.Value : new Vector3D();

            new string[] {
                "import chimera, _surface, numpy",
                "from numpy import array, single as floatc, intc",
                "",
                "def addAtom(molecule, id, residue, x, y, z, radius):",
                "    at = molecule.newAtom(id, chimera.Element(\"Tunn\"))",
                "    at.setCoord(chimera.Coord(x,y,z))",
                "    at.radius = radius",
                "    residue.addAtom(at)",
                ""
            }.ForEach(x => Writer.WriteLine(x));
        }
        #endregion

        public ChimeraExporter AddFetch(string id)
        {
            Writer.WriteLine($"chimera.runCommand('open cifID:{id}')");
            return this;
        }

        public ChimeraExporter AddTunnels(IEnumerable<Tunnel> tunnels, SurfaceType surfaceType = SurfaceType.Spheres)
        {
            tunnels.ForEach((t, i) => AddTunnel(GetCommandName(t) + (i + 1).ToString(), t, surfaceType));
            tunnels.ForEach((t, i) => ShowRepresentation(i + 1, surfaceType));

            return this;
        }

        public ChimeraExporter AddCavities(string prefix, IEnumerable<Cavity> cavities)
        {
            cavities.ForEach((c, i) => AddCavity(prefix + (i + 1).ToString(), i, c));
            return this;
        }

        private void ShowRepresentation(int i, SurfaceType surfaceType)
        {
            Writer.WriteLine($"chimera.runCommand('color {Colors.ElementAt(i % Colors.Count).Key} #{i}')");
            Writer.WriteLine($"chimera.runCommand('{(surfaceType == SurfaceType.Spheres ? "repr cpk" : "surface")} : {i}')");
        }

        public ChimeraExporter AddTunnel(string id, Tunnel t, SurfaceType surfaceType)
        {
            Writer.WriteLine($"def {id}(tunnelObject):");
            Writer.WriteLine($"    tunnel = tunnelObject.newResidue(\"{id}\", \" \", 1, \" \")");

            foreach (var item in t.Profile)
            {
                Writer.WriteLine($"    addAtom(tunnelObject, \"{id}\", tunnel, {item.Center.X:.00}, {item.Center.Y:.00}, {item.Center.Z:.00}, {item.Radius:.00})");
            }
            Writer.WriteLine("tunnelObject = chimera.Molecule()");
            Writer.WriteLine($"tunnelObject.name = \"{id}\"");
            Writer.WriteLine($"{id}(tunnelObject)");
            Writer.WriteLine("chimera.openModels.add([tunnelObject])\n");

            return this;
        }


        public ChimeraExporter AddCavity(string id, int count, Cavity c)
        {
            Writer.WriteLine($"{id} = _surface.SurfaceModel()");

            WriteChimeraTriangles(id, c);

            Writer.WriteLine($"{id}Piece = {id}.addPiece({id}V, {id}I, {Colors.ElementAt(count % Colors.Count).Value})");
            Writer.WriteLine($"{id}Piece.display = True");
            Writer.WriteLine($"{id}Piece.displayStyle = {id}Piece.Solid");


            Writer.WriteLine($"chimera.openModels.add([{id}])");

            return this;
        }

        private void WriteChimeraTriangles(string id, Cavity c)
        {
            StringBuilder vertices = new StringBuilder($"{id}V = []\n");
            StringBuilder indices = new StringBuilder($"{id}I = []\n");

            indices.AppendLine($"for x in range(0,{c.Boundary.Count()}):");
            indices.AppendLine($"    {id}I.append((x*3, x*3+1, x*3+2))");

            foreach (var item in c.Boundary)
            {
                for (int i = 0; i < 3; i++)
                {
                    vertices.AppendLine($"{id}V.append(({item.Vertices[i].Position.X:.00},{item.Vertices[i].Position.Y:.00},{item.Vertices[i].Position.Z:.00}))");
                }
            }

            Writer.WriteLine(vertices.ToString());
            Writer.WriteLine(indices.ToString());

            Writer.WriteLine($"{id}V = array(tuple({id}V), floatc)");
            Writer.WriteLine($"{id}I = array(tuple({id}I), intc)");
        }

        public string GetCommandName(Tunnel t)
        {
            if (t.Type == TunnelType.Pore) return t.IsMergedPore ? "MergedPore" : "Pore";
            return t.Type.ToString();
        }

    }
}