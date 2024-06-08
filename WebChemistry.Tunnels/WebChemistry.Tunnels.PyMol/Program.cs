using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using System.IO;

namespace WebChemistry.Tunnels.PyMol
{
    class Program
    {
        class FakeTunnel : ITunnel
        {
            public Tuple<Framework.Math.Point3D, double>[] Profile
            {
                get;
                set;
            }

            public IEnumerable<Residue> SurroundingResidues
            {
                get;
                set;
            }
        }

        static IEnumerable<FakeTunnel> CreateFakeTunnels(IStructure s)
        {
            Random rnd = new Random();

            ResidueCollection rc = new ResidueCollection(s as PdbStructure);

            List<FakeTunnel> tunnels = new List<FakeTunnel>();

            for (int i = 0; i < 5; i++)
            {
                FakeTunnel tunnel = new FakeTunnel();
                tunnel.SurroundingResidues = rc.Skip(rnd.Next(rc.Count() - 15)).Take(15).ToArray();
                tunnel.Profile = tunnel.SurroundingResidues.Select(r => Tuple.Create(r.Atoms.GeometricalCenter(), 2.0)).ToArray();
                tunnels.Add(tunnel);
            }

            return tunnels;
        }

        static void Main(string[] args)
        {
            var structure = StructureReader.ReadPdb("d:\\1TQN.pdb", "structure", false);

            PyMolExporter exporter = new PyMolExporter();
            exporter.SetStructure(File.ReadAllText("d:\\1TQN.pdb"));

            CreateFakeTunnels(structure).Run(t => { Console.WriteLine("{0} {1}", t.Profile.Count(), t.SurroundingResidues.Count()); exporter.AddTunnel(t); });

            Console.ReadLine();

         //   using (var w = new StreamWriter("export.py"))
            {
          //      exporter.Export(w);
            }
        }
    }
}
