/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebChemistry.Tunnels.WPF.Model
{
    class CSAEntry
    {
        public string PdbID { get; set; }
        public int SiteNumber { get; set; }
        //public string ResidueType { get; set; }
        public string ChainID { get; set; }
        public int ResidueNumber { get; set; }
        //public string ChemicalFunction { get; set; }
        //public string EvidenceType { get; set; }
        //public string LiteratureEntry { get; set; }
    }
}
