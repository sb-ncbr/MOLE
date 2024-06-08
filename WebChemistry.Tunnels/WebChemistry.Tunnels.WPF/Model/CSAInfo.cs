/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.Tunnels.WPF.Model
{
    class CSAInfo
    {
        public string PdbID { get; set; }
        public int SiteNumber { get; set; }
        //public string ChemicalFunction { get; set; }
        //public string EvidenceType { get; set; }
        //public string LiteratureEntry { get; set; }

        public ObservableResidueCollection Residues { get; private set; }

        public static IEnumerable<CSAInfo> FromEntries(IEnumerable<CSAEntry> entries, IStructure structure)
        {
            var aarr = entries.ToArray();
            var residues = structure.PdbResidues();
            return entries
                .GroupBy(e => e.SiteNumber)
                .Where(g => residues.FromIdentifier(PdbResidueIdentifier.Create(g.First().ResidueNumber, g.First().ChainID, ' ')) != null)
                .Select(e => new CSAInfo(e.OrderBy(r => r.ChainID).ThenBy(r => r.ResidueNumber), structure))
                .ToArray();
        }

        private CSAInfo(IEnumerable<CSAEntry> entries, IStructure structure)
        {
            var fe = entries.First();
            PdbID = fe.PdbID;
            SiteNumber = fe.SiteNumber;
            //ChemicalFunction = fe.ChemicalFunction;
            //EvidenceType = fe.EvidenceType;
            //LiteratureEntry = fe.LiteratureEntry;

            Residues = new ObservableResidueCollection(entries.Select(r => structure.PdbResidues().FromIdentifier(PdbResidueIdentifier.Create(r.ResidueNumber, r.ChainID, ' '))));
        }
    }
}
