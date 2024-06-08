/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Server.Parameters
{
    using Framework.Core.Pdb;
    using Framework.Geometry;
    using Framework.Math;
    using Queries.Core;
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.Tunnels.Core;
    using WebChemistry.Tunnels.Core.Export;
    using WebChemistry.Tunnels.Core.Geometry;


    class All
    {
        public IO IO { get; set; }
        public ComplexParameters Complex { get; set; }
        public List<ChargeSource> Charges { get; set; }
        public Export Export { get; set; }
        public Dictionary<ElementSymbol, double> CustomVdw { get; set; }
        public List<NonActivePart> NonActiveParts { get; set; }
        public List<TriangulationPoint> CustomExits { get; set; }

        public bool UseAutoOrigins { get; set; }
        public List<TriangulationPoint> Origins { get; set; }
        public List<Path> Paths { get; set; }

        public All()
        {
            IO = new IO();
            Export = new Export();
            Complex = new ComplexParameters();
            Charges = new List<ChargeSource>();
            CustomVdw = new Dictionary<ElementSymbol, double>();
            NonActiveParts = new List<NonActivePart>();
            CustomExits = new List<TriangulationPoint>();
            Origins = new List<TriangulationPoint>();
            Paths = new List<Path>();
        }
    }

    class IO
    {
        [Newtonsoft.Json.JsonIgnore]
        public string Filename { get; set; }
        public string SpecificChains { get; set; }
        public bool ReadAllModels { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string WorkingDirectory { get; set; }
    }

    class ChargeSource
    {
        public enum SourceType
        {
            RandomGrid,
            ScalarGrid,
            AtomValues
        }

        public class RandomGrid
        {
            public double MinValue { get; set; }
            public double MaxValue { get; set; }
        }

        public class ScalarGrid
        {
            public string Source { get; set; }
        }

        public class AtomValues
        {
            public string Source { get; set; }
            public bool IgnoreHydrogens { get; set; }
            public double Radius { get; set; }
            public int K { get; set; }
            public AtomValueFieldInterpolationMethod ValuesMethod { get; set; }
        }

        public SourceType Type { get; set; }
        public string Name { get; set; }
        public object Params { get; set; }

        public FieldBase MakeGrid(IStructure structure, Func<KDAtomTree> noHydrogens, Func<KDAtomTree> yesHydrogens, Func<K3DTree<PdbResidue>> residueTree)
        {
            try
            {
                switch (Type)
                {
                    case SourceType.RandomGrid: return ScalarFieldGrid.MakeRandom(Name, structure, (Params as RandomGrid).MinValue, (Params as RandomGrid).MaxValue);
                    case SourceType.ScalarGrid: return ScalarFieldGrid.FromOpenDX(Name, (Params as ScalarGrid).Source);
                    case SourceType.AtomValues:
                        {
                            var prms = Params as AtomValues;
                            return AtomValueField.FromFile(Name, prms.Source, structure,
                                prms.IgnoreHydrogens,
                                AtomValueField.NeedsAtomPivots(prms.ValuesMethod) ? (prms.IgnoreHydrogens ? noHydrogens() : yesHydrogens()) : null,
                                AtomValueField.NeedsAtomPivots(prms.ValuesMethod) ? residueTree() : null,
                                prms.ValuesMethod, prms.Radius, prms.K);
                        }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(string.Format("Error reading charge source '{0}': {1}", Name, e.Message));
            }
            return null;
        }
    }

    class Export
    {
        public bool FormatMesh { get; set; }
        public bool FormatMeshGz { get; set; }
        public bool FormatPyMol { get; set; }
        public bool FormatPdbProfile { get; set; }
        public bool FormatPdbStructure { get; set; }
        public bool FormatCSV { get; set; }
        public bool FormatJSON { get; set; }
        public bool FormatChargeSurface { get; set; }

        public bool ExportCavities { get; set; }
        public bool ExportMergedPores { get; set; }
        public bool ExportUserPores { get; set; }
        public bool ExportAutoPores { get; set; }
        public bool ExportTunnels { get; set; }

        public double MeshDensity { get; set; }

        public PyMolSurfaceType PyMolSurfaceType { get; set; }
        public string PyMolPDBId { get; set; }
        public PyMolSpectrumPalette PyMolChargePalette { get; set; }
    }

    class NonActivePart
    {
        public enum PartType
        {
            Residue,
            Query
        }

        public PartType Type { get; set; }
        public object Params { get; set; }
    }

    class TriangulationPoint
    {
        public enum PointType
        {
            Residues,
            ResidueSnaps,
            Points,
            Query
        }

        public PointType Type { get; set; }
        public object Params { get; set; }

        public Vector3D[] ToPoints(IStructure structure, Func<ExecutionContext> ctx)
        {
            return new Vector3D[0];
        }
    }

    class Path
    {
        public TriangulationPoint Start { get; set; }
        public TriangulationPoint End { get; set; }
    }

    class Residue
    {
        public string Chain { get; set; }
        public char InsertionCode { get; set; }
        public int SequenceNumber { get; set; }
    }

    class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}