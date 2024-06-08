/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.WPF.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WebChemistry.Tunnels.WPF.Model;
    using WebChemistry.Framework.Core;

    static class CSAService
    {
        static Dictionary<string, CSAEntry[]> activeSites;

        public static void Init()
        {
            if (!File.Exists("CSA.dat"))
            {
                activeSites = new Dictionary<string, CSAEntry[]>(StringComparer.InvariantCultureIgnoreCase);
                return;
            }

            var split = new char[] { ',' };

            activeSites = File.ReadLines("CSA.dat")
                .Skip(1)
                .Select(l => l.Split(split, StringSplitOptions.RemoveEmptyEntries))
                .Where(f => f.Length == 8)
                .Select(f => new CSAEntry
                {
                    PdbID = f[0],
                    SiteNumber = int.Parse(f[1]),
                    //ResidueType = f[2],
                    ChainID = f[3].Trim(),
                    ResidueNumber = int.Parse(f[4]),
                    //ChemicalFunction = f[5],
                    //EvidenceType = f[6],
                    //LiteratureEntry = f[7]
                })
                .GroupBy(e => e.PdbID)
                .ToDictionary(e => e.Key.ToUpper(), g => g.ToArray(), StringComparer.InvariantCultureIgnoreCase);
        }

        public static IEnumerable<CSAInfo> GetActiveSites(IStructure structure)
        {
            if (activeSites == null) Init();
            if (!activeSites.ContainsKey(structure.Id)) return Enumerable.Empty<CSAInfo>();
            return CSAInfo.FromEntries(activeSites[structure.Id], structure);
        }
    }
}
