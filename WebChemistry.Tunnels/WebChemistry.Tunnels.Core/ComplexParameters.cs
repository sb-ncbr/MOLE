/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Linq;
    using System.Globalization;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using System.Collections.Generic;

    public static class ResidueEx
    {
        /////// <summary>
        /////// Used to determine which residues participate in the triangulation.
        /////// </summary>
        ////public static readonly PropertyDescriptor<bool> IsActiveForTunnelComputationProperty
        ////    = PropertyHelper.Bool("IsActiveForTunnelComputation", category: "Residue", isImmutable: false);
        
        ////public static bool IsActiveForTunnelComputation(this PdbResidue residue)
        ////{
        ////    return residue.GetProperty(IsActiveForTunnelComputationProperty, true);
        ////}

        ////public static void SetIsActiveForTunnelComputation(this PdbResidue residue, bool value)
        ////{
        ////    residue.SetProperty(IsActiveForTunnelComputationProperty, value);
        ////}

        public static readonly PropertyDescriptor<IList<IAtom>> ActiveAtomsForTunnelComputationProperty
            = PropertyHelper.OfType<IList<IAtom>>("ActiveAtomsForTunnelComputation", category: "Residue", isImmutable: false);

        public static IList<IAtom> ActiveAtomsForTunnelComputation(this PdbResidue residue)
        {
            return residue.GetProperty(ActiveAtomsForTunnelComputationProperty, residue.Atoms);
        }

        public static void SetActiveAtomsForTunnelComputation(this PdbResidue residue, IList<IAtom> atoms)
        {
            residue.SetProperty(ActiveAtomsForTunnelComputationProperty, atoms);
        }
    }
    
    public enum TunnelWeightFunction
    {
        /// <summary>
        /// Uses the Voronoi distance in the weight.
        /// </summary>
        VoronoiScale = 0, 
        /// <summary>
        /// Uses length and radius to compute the weight
        /// </summary>
        LengthAndRadius,
        /// <summary>
        /// Uses length only.
        /// </summary>
        Length,
        /// <summary>
        /// Uses weigh one for each tetrahedron.
        /// </summary>
        Constant
    }

    public class ComplexParameters
    {   
        /// <summary>
        /// Probe radius to determine the molecular surface.
        /// </summary>
        public double ProbeRadius { get; set; }

        /// <summary>
        /// Effectively the minimum tunnel radius.
        /// </summary>
        public double InteriorThreshold { get; set; }

        /// <summary>
        /// If true, uses a less restrictive calculation of the protein interior.
        /// </summary>
        public bool StrictInterior { get; set; }

        /// <summary>
        /// Minimum radius of a tunnel opening.
        /// </summary>
        public double SurfaceCoverRadius { get; set; }

        /// <summary>
        /// Determines how far to search for a cavity from the origin point.
        /// </summary>
        public double OriginRadius { get; set; }

        /// <summary>
        /// The minimal distance between two auto origins in a given cavity.
        /// </summary>
        public double AutoOriginCoverRadius { get; set; }

        /// <summary>
        /// Maximum number of automatically computed origins per cavity.
        /// </summary>
        public int MaxAutoOriginsPerCavity { get; set; }

        /////// <summary>
        /////// Minimum tunnel length in Ang.
        /////// </summary>
        ////public double MinimumTunnelLength { get; set; }

        /////// <summary>
        /////// Number of control points per 1 Ang.
        /////// </summary>
        ////public double TunnelControlPointsRatio { get; set; }

        /// <summary>
        /// Minimum number of tetrahedron layers to be considered a cavity.
        /// </summary>
        public int MinDepth { get; set; }

        /// <summary>
        /// Minimum tetrahedron depth to be considered a cavity.
        /// </summary>
        public double MinDepthLength { get; set; }

        /// <summary>
        /// Ignore HET atoms in triangulation computation.
        /// </summary>
        public bool IgnoreHETAtoms { get; set; }

        /// <summary>
        /// Determines whether to ignore Hydrogens or not.
        /// </summary>
        public bool IgnoreHydrogens { get; set; }

        /// <summary>
        /// Determines the minimum radius of a tunnel in angstroms.
        /// Default is 1.2.
        /// </summary>
        public double BottleneckRadius { get; set; }

        /// <summary>
        /// Determines the minimum radius of a tunnel in angstroms.
        /// Default is 0.0.
        /// </summary>
        public double BottleneckTolerance { get; set; }

        /// <summary>
        /// Only custom exits are used for the computation.
        /// </summary>
        public bool UseCustomExitsOnly { get; set; }

        /// <summary>
        /// Weight function used for the computation.
        /// </summary>
        public TunnelWeightFunction WeightFunction { get; set; }

        /// <summary>
        /// Determines the minimum length of a tunnel.
        /// </summary>
        public double MinTunnelLength { get; set; }

        /// <summary>
        /// Determines the minimum length of a pore.
        /// </summary>
        public double MinPoreLength { get; set; }

        /// <summary>
        /// Remove boundary layers from tunnels?
        /// </summary>
        public bool FilterTunnelBoundaryLayers { get; set; }

        double _maxTunnelSimilarity;
        /// <summary>
        /// The maximum percentage tunnel similarity until the longer one is removed. 
        /// Default is 0.9.
        /// </summary>
        public double MaxTunnelSimilarity 
        {
            get { return _maxTunnelSimilarity; }
            set
            {
                if (value < 0.5) _maxTunnelSimilarity = 0.5;
                else if (value > 1) _maxTunnelSimilarity = 1;
                else _maxTunnelSimilarity = value;
            }
        }

        public ComplexParameters()
        {
            MinDepth = 8;
            MinDepthLength = 5;
            ProbeRadius = 3;
            InteriorThreshold = 1.25;
            IgnoreHETAtoms = false;
            IgnoreHydrogens = false;

            MinTunnelLength = 0.0;
            MinPoreLength = 0.0;
            SurfaceCoverRadius = 10.0;
            BottleneckRadius = 1.25;
            BottleneckTolerance = 0.0;
            MaxTunnelSimilarity = 0.9;
            OriginRadius = 5.0;
            AutoOriginCoverRadius = 10.0;
            MaxAutoOriginsPerCavity = 5;
            UseCustomExitsOnly = false;
            FilterTunnelBoundaryLayers = false;
            WeightFunction = TunnelWeightFunction.VoronoiScale;
        }

        public XElement ToXml()
        {
            return new XElement("Params",
                new XElement("Cavity",
                    new XAttribute("MinDepth", MinDepth.ToString()),
                    new XAttribute("MinDepthLength", MinDepthLength.ToStringInvariant("0.000")),
                    new XAttribute("ProbeRadius", ProbeRadius.ToStringInvariant("0.000")),
                    new XAttribute("InteriorThreshold", InteriorThreshold.ToStringInvariant("0.000")),
                    new XAttribute("IgnoreHETAtoms", IgnoreHETAtoms.ToString()),
                    new XAttribute("IgnoreHydrogens", IgnoreHydrogens.ToString())),
                new XElement("Tunnel",
                    new XAttribute("MinTunnelLength", MinTunnelLength.ToStringInvariant("0.000")),
                    new XAttribute("MinPoreLength", MinPoreLength.ToStringInvariant("0.000")),
                    new XAttribute("SurfaceCoverRadius", SurfaceCoverRadius.ToStringInvariant("0.000")),
                    new XAttribute("BottleneckRadius", BottleneckRadius.ToStringInvariant("0.000")),
                    new XAttribute("BottleneckTolerance", BottleneckTolerance.ToStringInvariant("0.000")),
                    new XAttribute("MaxTunnelSimilarity", MaxTunnelSimilarity.ToStringInvariant("0.000")),
                    new XAttribute("OriginRadius", OriginRadius.ToStringInvariant("0.000")),
                    new XAttribute("AutoOriginCoverRadius", AutoOriginCoverRadius.ToStringInvariant("0.000")),
                    new XAttribute("MaxAutoOriginsPerCavity", MaxAutoOriginsPerCavity.ToString()),
                    new XAttribute("UseCustomExitsOnly", UseCustomExitsOnly.ToString()),
                    new XAttribute("FilterBoundaryLayers", FilterTunnelBoundaryLayers.ToString()),
                    new XAttribute("WeightFunction", WeightFunction.ToString())));
        }

        public static ComplexParameters FromXml(XElement xml)
        {
            var ret = new ComplexParameters();

            var cavity = xml.Element("Cavity");
            if (cavity != null)
            {
                SetIfPresent<int>(cavity, "MinDepth", GetInteger, x => ret.MinDepth = x);
                SetIfPresent<double>(cavity, "MinDepthLength", GetDouble, x => ret.MinDepthLength = x);
                SetIfPresent<double>(cavity, "ProbeRadius", GetDouble, x => ret.ProbeRadius = x);
                SetIfPresent<double>(cavity, "InteriorThreshold", GetDouble, x => ret.InteriorThreshold = x);
                SetIfPresent<bool>(cavity, "IgnoreHETAtoms", GetBool, x => ret.IgnoreHETAtoms = x);
                SetIfPresent<bool>(cavity, "IgnoreHydrogens", GetBool, x => ret.IgnoreHydrogens = x);
            }

            var tunnel = xml.Element("Tunnel");
            if (tunnel != null)
            {
                SetIfPresent<double>(tunnel, "MinTunnelLength", GetDouble, x => ret.MinTunnelLength = x);
                SetIfPresent<double>(tunnel, "MinPoreLength", GetDouble, x => ret.MinPoreLength = x);
                SetIfPresent<double>(tunnel, "SurfaceCoverRadius", GetDouble, x => ret.SurfaceCoverRadius = x);
                SetIfPresent<double>(tunnel, "BottleneckRadius", GetDouble, x => ret.BottleneckRadius = x);
                SetIfPresent<double>(tunnel, "BottleneckTolerance", GetDouble, x => ret.BottleneckTolerance = x);
                SetIfPresent<double>(tunnel, "MaxTunnelSimilarity", GetDouble, x => ret.MaxTunnelSimilarity = x);
                SetIfPresent<double>(tunnel, "OriginRadius", GetDouble, x => ret.OriginRadius = x);
                SetIfPresent<double>(tunnel, "AutoOriginCoverRadius", GetDouble, x => ret.AutoOriginCoverRadius = x);
                SetIfPresent<int>(tunnel, "MaxAutoOriginsPerCavity", GetInteger, x => ret.MaxAutoOriginsPerCavity = x);
                SetIfPresent<bool>(tunnel, "UseCustomExitsOnly", GetBool, x => ret.UseCustomExitsOnly = x);
                SetIfPresent<bool>(tunnel, "FilterBoundaryLayers", GetBool, x => ret.FilterTunnelBoundaryLayers = x);
                SetIfPresent<TunnelWeightFunction>(tunnel, "WeightFunction", GetEnum<TunnelWeightFunction>, x => ret.WeightFunction = x);
            }
            
            return ret;
        }

        public ComplexParameters Clone()
        {
            var ret = new ComplexParameters();
            foreach (var p in typeof(ComplexParameters).GetProperties())
            {
                p.SetValue(ret, p.GetValue(this, null), null);
            }
            return ret;
        }

        #region Parsers

        static void SetIfPresent<T>(XElement x, string name, Func<string, object> parser, Action<T> setter)
        {
            var attr = x.Attribute(name);
            if (attr == null) return;
            var val = (T)parser(attr.Value);
            setter(val);
        }

        static object GetEnum<T>(string value)
            where T : struct
        {
            T ret;
            if (!Enum.TryParse<T>(value, true, out ret))
            {
                throw new ArgumentException(string.Format("Expected a value from {{{0}}}, got '{1}'.", string.Join(", ", Enum.GetNames(typeof(T)).ToArray()), value));
            }
            return ret;
        }

        static object GetDouble(string value)
        {
            double ret;
            if (!double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ret))
            {
                throw new ArgumentException(string.Format("Expected a real number (i.e. 1.25), got '{0}'.", value));
            }
            return ret;
        }
        
        static object GetInteger(string value)
        {
            int ret;
            if (!int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ret))
            {
                throw new ArgumentException(string.Format("Expected an integer (i.e. 10), got '{0}'.", value));
            }
            return ret;
        }

        static object GetBool(string value)
        {
            if (value.Equals("0", StringComparison.OrdinalIgnoreCase) || value.Equals("False", StringComparison.OrdinalIgnoreCase)) return false;
            if (value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("True", StringComparison.OrdinalIgnoreCase)) return true;

            throw new ArgumentException(string.Format("Expected a boolean (0/1 or True/False), got '{0}'.", value));
        }
        #endregion
    }
}
