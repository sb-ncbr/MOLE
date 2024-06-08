/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// ...
    /// </summary>
    public class TunnelPhysicoChemicalProperties
    {
        public static readonly int NumLayerProperties = 4;

        public int Charge { get; private set; }
        public int Ionizable { get; set; }
        public int NumPositives { get; private set; }
        public int NumNegatives { get; private set; }
        public double Hydropathy { get; private set; }
        public double Hydrophobicity { get; private set; }
        public double Polarity { get; private set; }
        public double LogP { get; set; }
        public double LogD { get; set; }
        public double LogS { get; set; }
        //public double Hydratation { get; private set; }
        public int Mutability { get; private set; }

        public TunnelPhysicoChemicalProperties(int Charge, int Ionizable, double Hydropathy, double Hydrophobicity, double Polarity, int Mutability, double LogP, double LogD, double LogS, int NumPositives = 0, int NumNegatives = 0)
        {
            this.Charge = Charge;
            this.Ionizable = Ionizable;
            this.Hydropathy = Hydropathy;
            this.Hydrophobicity = Hydrophobicity;
            this.Polarity = Polarity;
            this.LogP = LogP;
            this.LogD = LogD;
            this.LogS = LogS;
            //this.Hydratation = Hydratation;
            this.Mutability = Mutability;
            this.NumNegatives = NumNegatives;
            this.NumPositives = NumPositives;
        }

        public XElement ToXml()
        {
            return new XElement("Properties",
                new XAttribute("Charge", Charge),
                new XAttribute("Ionizable", Ionizable),
                new XAttribute("NumPositives", NumPositives),
                new XAttribute("NumNegatives", NumNegatives),
                //new XAttribute("Hydratation", props.Hydratation.ToStringInvariant("0.00")),
                new XAttribute("Hydrophobicity", Hydrophobicity.ToStringInvariant("0.00")),
                new XAttribute("Hydropathy", Hydropathy.ToStringInvariant("0.00")),
                new XAttribute("Polarity", Polarity.ToStringInvariant("0.00")),
                new XAttribute("LogP", LogP.ToStringInvariant("0.00")),
                new XAttribute("LogD", LogD.ToStringInvariant("0.00")),
                new XAttribute("LogS", LogS.ToStringInvariant("0.00")),
                new XAttribute("Mutability", Mutability));
        }

        public object ToJson()
        {
            return new
            {
                Charge = Charge,
                Ionizable = Ionizable,
                NumPositives = NumPositives,
                NumNegatives = NumNegatives,
                //Hydratation = props.Hydratation.ToStringInvariant("0.00"),
                Hydrophobicity = Math.Round(Hydrophobicity, 2),
                Hydropathy = Math.Round(Hydropathy, 2),
                Polarity = Math.Round(Polarity, 2),
                LogP = Math.Round(LogP, 2),
                LogD = Math.Round(LogD, 2),
                LogS = Math.Round(LogS, 2),
                Mutability = Mutability
            };
        }
    }

    public static class PhysicoChemicalPropertyCalculation
    {
        public static TunnelPhysicoChemicalProperties CalculateResidueProperties(IList<PdbResidue> residues)
        {
            int count = 0;
            int charge = 0;
            int ionizable = 0;
            double hydropathy = 0.0;
            double hydrophobicity = 0.0;
            double polarity = 0.0;
            double logP = 0.0;
            double logD = 0.0;
            double logS = 0.0;
            //double hydratation = 0.0;
            double mutability = 0.0;
            int positives = 0;
            int negatives = 0;

            // count only side-chain residues
            foreach (var residue in residues)
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                var pc = info.Charge;
                ionizable += info.Ionizable;
                charge += pc;
                if (pc > 0)
                {
                    positives++;
                }
                else if (pc < 0)
                {
                    negatives++;
                }
                //hydropathy += info.Hydropathy;
                //hydratation += info.Hydratation;
                //hydrophobicity += info.Hydrophobicity;
                //polarity += info.Polarity;
                mutability += info.Mutability;
            }

            //hydropathy /= (double)count;
            //hydrophobicity /= (double)count;
            //polarity /= (double)count;
            if (count == 0) mutability = 0;
            else mutability /= (double)count;

            PhysicoChemicalPropertyCalculation.CalculateHydrophibilicyPolarityHydropathy(residues, out hydrophobicity, out polarity, out hydropathy, out logP, out logD, out logS);

            return new TunnelPhysicoChemicalProperties(
                Charge: charge,
                Ionizable: ionizable,
                Polarity: polarity,
                //Hydratation: hydratation,
                Hydrophobicity: hydrophobicity,
                Hydropathy: hydropathy,
                LogP: logP,
                LogS: logS,
                LogD: logD,
                Mutability: (int)mutability,
                NumNegatives: negatives,
                NumPositives: positives
            );
        }

        public static void CalculateHydrophibilicyPolarityHydropathy(IList<PdbResidue> residues, out double hydrophobicity, out double polarity, out double hydropathy, out double logP, out double logD, out double logS)
        {
            hydrophobicity = 0;
            polarity = 0;
            hydropathy = 0;
            logP = 0;
            logD = 0;
            logS = 0;

            int count = 0;

            foreach (var residue in residues)
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                hydropathy += info.Hydropathy;
                hydrophobicity += info.Hydrophobicity;
                polarity += info.Polarity;
                logP += info.LogP;
                logD += info.LogD;
                logS += info.LogS;
            }

            var infoGLY = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("GLY");
            var infoASN = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("ASN");
            var infoBB = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("BACKBONE");

            foreach (var residue in residues)
            {
                count++;
                polarity += infoASN.Polarity;
                hydrophobicity += infoGLY.Hydrophobicity;
                hydropathy += infoGLY.Hydropathy;
                logP += infoBB.LogP;
                logD += infoBB.LogD;
                logS += infoBB.LogS;
            }

            if (count == 0)
            {
                hydropathy = hydrophobicity = polarity = 0;
            }
            else
            {
                hydropathy /= (double)count;
                hydrophobicity /= (double)count;
                polarity /= (double)count;
                logP /= (double)count;
                logD /= (double)count;
                logS /= (double)count;
            }
        }

        public static void CalculateHydrophibilicyPolarityHydropathy(IEnumerable<TunnelLayer> layers, out double hydrophobicity, out double polarity, out double hydropathy, out double logP, out double logD, out double logS)
        {
            hydrophobicity = 0;
            polarity = 0;
            hydropathy = 0;
            logP = 0;
            logD = 0;
            logS = 0;

            int count = 0;

            foreach (var residue in layers.SelectMany(l => l.NonBackboneLining))
            {
                var info = TunnelPhysicoChemicalPropertyTable.GetResidueProperties(residue);
                if (info == null) continue;

                count++;
                hydropathy += info.Hydropathy;
                hydrophobicity += info.Hydrophobicity;
                polarity += info.Polarity;
                logP += info.LogP;
                logD += info.LogD;
                logS += info.LogS;
            }

            var infoGLY = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("GLY");
            var infoASN = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("ASN");
            var infoBB = TunnelPhysicoChemicalPropertyTable.GetResidueProperties("BACKBONE");

            foreach (var residue in layers.SelectMany(l => l.BackboneLining))
            {
                count++;
                polarity += infoASN.Polarity;
                hydrophobicity += infoGLY.Hydrophobicity;
                hydropathy += infoGLY.Hydropathy;
                logP += infoBB.LogP;
                logD += infoBB.LogD;                
                logS += infoBB.LogS;
            }

            if (count == 0)
            {
                hydropathy = hydrophobicity = polarity = logP = logD = logS = 0;
            }
            else
            {
                hydropathy /= (double)count;
                hydrophobicity /= (double)count;
                polarity /= (double)count;
                logP /= (double)count;
                logD /= (double)count;
                logS /= (double)count;
            }
        }
    }

    /// <summary>
    /// Information about physico chemical properties of a tunnel
    /// </summary>
    public class TunnelPhysicoChemicalPropertyTable
    {
        public static TunnelPhysicoChemicalProperties GetResidueProperties(PdbResidue residue)
        {
            TunnelPhysicoChemicalProperties ret;
            if (info.TryGetValue(residue.Name, out ret)) return ret;
            return null;
        }

        public static TunnelPhysicoChemicalProperties GetResidueProperties(string name)
        {
            TunnelPhysicoChemicalProperties ret;
            if (info.TryGetValue(name, out ret)) return ret;
            return null;
        }

        private static Dictionary<string, TunnelPhysicoChemicalProperties> info = new Dictionary<string, TunnelPhysicoChemicalProperties>(StringComparer.OrdinalIgnoreCase) {
            {"ALA", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  1.8,
                Hydrophobicity:  0.02,
                Polarity:  0,
                LogP: 1.08,
                LogD: 1.08,
                LogS: 0.59,                    
                //Hydratation: 1,
                Mutability: 100)
            },
            {"ARG", new TunnelPhysicoChemicalProperties(
                Charge: 1,
                Ionizable: 1,
                Hydropathy:  -4.5,
                Hydrophobicity:  -0.42,
                Polarity:  52,
                LogP: -0.08,
                LogD: -2.49,
                LogS: 1.63,                
                //Hydratation: 2.3,
                Mutability: 83
            )},
            {"ASN", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -3.5,
                Hydrophobicity:  -0.77,
                Polarity:  3.38,
                LogP: -1.03,
                LogD: -1.03,
                LogS: 0.54,
                //Hydratation: 2.2,
                Mutability: 104
            )},
            {"ASP", new TunnelPhysicoChemicalProperties(
                Charge: -1,
                Ionizable:1,
                Hydropathy:  -3.5,
                Hydrophobicity:  -1.04,
                Polarity:  49.7,
                LogP: -0.22,
                LogD: -3,
                LogS: 2.63,
                //Hydratation: 6.5,
                Mutability: 86
            )},
            {"CYS", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  2.5,
                Hydrophobicity:  0.77,
                Polarity:  1.48,
                LogP: 0.84,
                LogD: 0.84,
                LogS: 0.16,
                //Hydratation: 0.1,
                Mutability: 44
            )},
            {"GLU", new TunnelPhysicoChemicalProperties(
                Charge: -1,
                Ionizable: 1,
                Hydropathy:  -3.5,
                Hydrophobicity:  -1.14,
                Polarity:  49.9,
                LogP: 0.48,
                LogD: -2.12,
                LogS: 2.23,
                //Hydratation: 6.2,
                Mutability: 77
            )},
            {"GLN", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -3.5,
                Hydrophobicity:  -1.1,
                Polarity:  3.53,
                LogP: -0.33,
                LogD: -0.33,
                LogS: 0.13,
                //Hydratation: 2.1,
                Mutability: 84
            )},
            {"GLY", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -0.4,
                Hydrophobicity:  -0.8,
                Polarity:  0,
                LogP: 0,
                LogD: 0,
                LogS: 0,
                //Hydratation: 1.1,
                Mutability: 50
            )},
            {"HIS", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -3.2,
                Hydrophobicity:  0.26,
                Polarity:  51.6,
                LogP: -0.01,
                LogD: -0.11,
                LogS: -0.2,
                //Hydratation: 2.8,
                Mutability: 91
            )},
            {"ILE", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  4.5,
                Hydrophobicity:  1.81,
                Polarity:  0.13,
                LogP: 2.24,
                LogD: 2.24,
                LogS: -1.85,
                //Hydratation: 0.8,
                Mutability: 103
            )},
            {"LEU", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  3.8,
                Hydrophobicity:  1.14,
                Polarity:  0.13,
                LogP: 2.08,
                LogD: 2.08,
                LogS: -1.79,
                //Hydratation: 0.8,
                Mutability: 54
            )},
            {"LYS", new TunnelPhysicoChemicalProperties(
                Charge: 1,
                Ionizable: 1,
                Hydropathy:  -3.9,
                Hydrophobicity:  -0.41,
                Polarity:  49.5,
                LogP: 0.7,
                LogD: -1.91,
                LogS: 1.46,
                //Hydratation: 5.3,
                Mutability: 72
            )},
            {"MET", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  1.9,
                Hydrophobicity:  1,
                Polarity:  1.43,
                LogP: 1.48,
                LogD: 1.48,
                LogS: -0.72,
                //Hydratation: 0.7,
                Mutability: 93
            )},
            {"PHE", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  2.8,
                Hydrophobicity:  1.35,
                Polarity:  0.35,
                LogP: 2.49,
                LogD: 2.49,
                LogS: -1.81,
                //Hydratation: 1.4,
                Mutability: 51
            )},
            {"PRO", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -1.6,
                Hydrophobicity:  -0.09,
                Polarity:  1.58,
                LogP: 1.8,
                LogD: 1.8,
                LogS: -1.3,
                //Hydratation: 0.9,
                Mutability: 58
            )},
            {"SER", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -0.8,
                Hydrophobicity:  -0.97,
                Polarity:  1.67,
                LogP: -0.52,
                LogD: -0.52,
                LogS: 1.11,
                //Hydratation: 1.7,
                Mutability: 117
            )},
            {"THR", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -0.7,
                Hydrophobicity:  -0.77,
                Polarity:  1.66,
                LogP: -0.16,
                LogD: -0.16,
                LogS: 0.77,
                //Hydratation: 1.5,
                Mutability: 107
            )},
            {"TRP", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -0.9,
                Hydrophobicity:  1.71,
                Polarity:  2.1,
                LogP: 2.59,
                LogD: 2.59,
                LogS: -2.48,
                //Hydratation: 1.9,
                Mutability: 25
            )},
            {"TYR", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  -1.3,
                Hydrophobicity:  1.11,
                Polarity:  1.61,
                LogP: 2.18,
                LogD: 2.18,
                LogS: -1.44,
                //Hydratation: 2.1,
                Mutability: 50
            )},
            {"VAL", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy:  4.2,
                Hydrophobicity:  1.13,
                Polarity:  0.13,
                LogP: 1.8,
                LogD: 1.8,
                LogS: -1.3,
                //Hydratation: 0.9,
                Mutability: 98
            )},
            {"BACKBONE", new TunnelPhysicoChemicalProperties(
                Charge: 0,
                Ionizable: 0,
                Hydropathy: -0.4,
                Hydrophobicity: 0.0, // not defined for backbone
                Polarity: 3.5,
                Mutability: 0, // not defined for backobe
                LogP: -0.86,
                LogD: -0.86,
                LogS: 0.81
                )
            }
        };
    }
}

