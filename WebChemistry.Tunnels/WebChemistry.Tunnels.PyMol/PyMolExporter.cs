using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using System.IO;

namespace WebChemistry.Tunnels.PyMol
{
    class PyMolExporter
    {
        // tady si vymysli svou vlastni interni reprezentaci
        string pdbSource;
        List<ITunnel> tunnels;

        public void SetStructure(string pdbSource)
        {
            // tohle bude text pdb souboru, ktery obsahuje puvodni strukturu
            // chtel bych to dat do jednoho souboru, protoze silverlight umi ulozit pouze jeden soubor v dany okamzik (tj. nema pristup do slozky, kde by mohl vytvorit vic souboru).
            this.pdbSource = pdbSource;
        }

        public void AddTunnel(ITunnel tunnel)
        {
            // prida tunel .. zatim bude stacit, kdyz to vykresli koule se stredem v ITunnel.Profile/Item1 a polomerem Item2
            // do budoucna mozna i izoplocha reprezentovana trojuhelniky atd., to se uvidi

            // dale bych rad, aby po otevreni bylo v pymolu dostupne jednoduche UI, kde by si uzivatel mohl vyhrat tunely, ktere se zobrazi a u kazdeho tunelu (treba jako tooltip),
            // byly zobrazeny nejake informace (okolni rezidua (ITunnel.SurroundingResidues)

            this.tunnels.Add(tunnel);
        }

        public void Export(TextWriter writer)
        {
            // tohle vytvori finalni soubor - zapises vsechna potrebna data pomoci writer.WriteLine
        }

        public PyMolExporter()
        {
            this.tunnels = new List<ITunnel>();
        }
    }
}
