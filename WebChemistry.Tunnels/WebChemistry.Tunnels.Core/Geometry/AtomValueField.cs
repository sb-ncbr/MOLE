/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;

    public enum AtomValueFieldInterpolationMethod
    {
        // Atoms
        NearestValue,
        RadiusSum,
        RadiusSumDividedByDistance,
        RadiusMultiplicativeScale,
        RadiusAdditiveScale,
        KNearestSum,
        KNearestSumDividedByDistance,
        Lining,
        WholeStructure,

        // Residues
        RadiusResidueSum,
        RadiusResidueSumDividedByDistance,
        KNearestResidueSum,
        KNearestResidueSumDividedByDistance,
        AllResidues
    }
    
    public class AtomValueField : FieldBase
    {
        public static bool NeedsAtomPivots(AtomValueFieldInterpolationMethod method)
        {
            return !NeedsResiduePivots(method);
        }

        public static bool NeedsResiduePivots(AtomValueFieldInterpolationMethod method)
        {
            return (
                method == AtomValueFieldInterpolationMethod.RadiusResidueSum ||
                method == AtomValueFieldInterpolationMethod.RadiusResidueSumDividedByDistance ||
                method == AtomValueFieldInterpolationMethod.KNearestResidueSum ||
                method == AtomValueFieldInterpolationMethod.KNearestResidueSumDividedByDistance ||
                method == AtomValueFieldInterpolationMethod.AllResidues);
        }

        AtomValueFieldInterpolationMethod Method;
        IStructure Structure;
        Dictionary<IAtom, double> Values;
        KDAtomTree AtomPivots;
        K3DTree<PdbResidue> ResiduePivots;
        bool IgnoreHydrogens;
        double Radius;
        int K;
        
        Func<Vector3D, double?> InterpolationFuncCenter;
        Func<TunnelProfile.Node, TunnelLayer, double?> InterpolationFuncNode;

        double? GetResidueCharge(PdbResidue residue)
        {
            var atoms = residue.Atoms;
            var len = atoms.Count;
            double charge = 0;
            bool present = false;
            for (int i = 0; i < len; i++)
            {
                double value;
                var a = atoms[i];
                if (IgnoreHydrogens && a.ElementSymbol == ElementSymbols.H) continue;
                if (Values.TryGetValue(atoms[i], out value))
                {
                    present = true;
                    charge += value;
                }
            }
            return present ? (double?)charge : null;
        }

        #region Atoms
        double? InterpolateNearest(Vector3D position)
        {
            var atom = AtomPivots.Nearest(position);
            double value;
            if (Values.TryGetValue(atom.Value, out value)) return value;
            return null;
        }

        double? InterpolateRadiusSum(Vector3D position)
        {
            var atoms = AtomPivots.NearestRadius(position, Radius);
            if (atoms.Count == 0) return null;
                        
            double value = 0.0;
            bool present = false;
            int len = atoms.Count;
            for (int i = 0; i < len; i++)
            {
                double t;
                if (Values.TryGetValue(atoms[i].Value, out t)) 
                {
                    present = true;
                    value += t;
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateRadiusSumDividedByDistance(Vector3D position)
        {
            var atoms = AtomPivots.NearestRadius(position, Radius);
            if (atoms.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = atoms.Count;
            for (int i = 0; i < len; i++)
            {
                double t;
                var a = atoms[i];
                if (Values.TryGetValue(a.Value, out t))
                {
                    present = true;
                    value += t / Math.Sqrt(a.Priority);
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateRadiusMultiplicative(TunnelProfile.Node node, TunnelLayer layer)
        {
            var position = node.Center;
            var atoms = AtomPivots.NearestRadius(position, Radius * node.Radius);
            if (atoms.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = atoms.Count;
            for (int i = 0; i < len; i++)
            {
                double t;
                var a = atoms[i];
                if (Values.TryGetValue(a.Value, out t))
                {
                    present = true;
                    value += t / Math.Sqrt(a.Priority);
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateRadiusAdditive(TunnelProfile.Node node, TunnelLayer layer)
        {
            var position = node.Center;
            var atoms = AtomPivots.NearestRadius(position, Radius + node.Radius);
            if (atoms.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = atoms.Count;
            for (int i = 0; i < len; i++)
            {
                double t;
                var a = atoms[i];
                if (Values.TryGetValue(a.Value, out t))
                {
                    present = true;
                    value += t / Math.Sqrt(a.Priority);
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateKNearestSum(Vector3D position)
        {
            var atoms = AtomPivots.NearestCount(position, K);
            if (atoms.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = atoms.Count;
            for (int i = 0; i < len; i++)
            {
                double t;
                if (Values.TryGetValue(atoms[i].Value, out t))
                {
                    present = true;
                    value += t;
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateKNearestSumDividedByDistance(Vector3D position)
        {
            var atoms = AtomPivots.NearestCount(position, K);
            if (atoms.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = atoms.Count;
            for (int i = 0; i < len; i++)
            {
                double t;
                var a = atoms[i];
                if (Values.TryGetValue(a.Value, out t))
                {
                    present = true;
                    value += t / Math.Sqrt(a.Priority);
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateWholeStructure(Vector3D position)
        {
            double ret = 0;
            foreach (var a in Structure.Atoms)
            {
                double t;
                if (Values.TryGetValue(a, out t))
                {
                    ret += t / a.Position.DistanceTo(position);
                }
            }
            return ret;
        }

        double? InterpolateLining(TunnelProfile.Node node, TunnelLayer layer)
        {
            bool present = false;
            var position = node.Center;
            double ret = 0;
            foreach (var r in layer.Lining)
            {
                var atoms = r.Atoms;
                for (int i = 0; i < atoms.Count; i++)
                {
                    double t;
                    var a = atoms[i];
                    if (Values.TryGetValue(a, out t))
                    {
                        present = true;
                        ret += t / a.Position.DistanceTo(position);
                    }    
                }
            }
            if (present) return ret;
            return null;
        }
        #endregion

        #region Residues
        double? InterpolateRadiusResidueSum(Vector3D position)
        {
            var residues = ResiduePivots.NearestRadius(position, Radius);
            if (residues.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = residues.Count;
            for (int i = 0; i < len; i++)
            {
                var charge = GetResidueCharge(residues[i].Value);
                if (charge != null)
                {
                    present = true;
                    value += charge.Value;
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateRadiusResidueSumDividedByDistance(Vector3D position)
        {
            var residues = ResiduePivots.NearestRadius(position, Radius);
            if (residues.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = residues.Count;
            for (int i = 0; i < len; i++)
            {
                var r = residues[i];
                var charge = GetResidueCharge(r.Value);
                if (charge.HasValue)
                {
                    present = true;
                    value += charge.Value / Math.Sqrt(r.Priority);
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateKNearestResidueSum(Vector3D position)
        {
            var residues = ResiduePivots.NearestCount(position, K);
            if (residues.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = residues.Count;
            for (int i = 0; i < len; i++)
            {
                var charge = GetResidueCharge(residues[i].Value);
                if (charge.HasValue)
                {
                    present = true;
                    value += charge.Value;
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateKNearestResidueSumDividedByDistance(Vector3D position)
        {
            var residues = ResiduePivots.NearestCount(position, K);
            if (residues.Count == 0) return null;

            double value = 0.0;
            bool present = false;
            int len = residues.Count;
            for (int i = 0; i < len; i++)
            {
                var r = residues[i];
                var charge = GetResidueCharge(r.Value);
                if (charge.HasValue)
                {
                    present = true;
                    value += charge.Value / Math.Sqrt(r.Priority);
                }
            }
            return present ? (double?)value : null;
        }

        double? InterpolateAllResidues(Vector3D position)
        {
            double ret = 0;
            foreach (var r in Structure.PdbResidues())
            {
                var charge = GetResidueCharge(r);
                if (charge.HasValue)
                {
                    ret += charge.Value / r.Atoms.GeometricalCenter().DistanceTo(position);
                }
            }
            return ret;
        }
        #endregion

        double? WrapNodeToCenter(TunnelProfile.Node node, TunnelLayer layer)
        {
            return Interpolate(node.Center);
        }

        public override double? Interpolate(Vector3D position)
        {
            return InterpolationFuncCenter(position);
        }

        public override double? Interpolate(TunnelProfile.Node node, TunnelLayer layer)
        {
            return InterpolationFuncNode(node, layer);
        }

        static Dictionary<IAtom, double> ReadValues(string filename, IStructure structure)
        {
            var fi = new FileInfo(filename);

            if (fi.Extension.EndsWith("wprop", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new StreamReader(filename))
                {
                    var props = AtomPropertiesBase.Read(reader);
                    if (!props.ParentId.EqualOrdinalIgnoreCase(structure.Id))
                    {
                        throw new ArgumentException(string.Format("Property parent id does not match in '{0}'. Got '{1}', expected '{2}'.", filename, props.ParentId, structure.Id));
                    }
                    if (!(props is RealAtomProperties))
                    {
                        throw new ArgumentException(string.Format("Property type does not match in '{0}'. Got '{1}', expected 'RealAtomProperties'.", filename, props.GetType().Name));
                    }
                    Dictionary<IAtom, double> ret = new Dictionary<IAtom, double>(structure.Atoms.Count);
                    foreach (var a in structure.Atoms)
                    {
                        object value = props.TryGetValue(a);
                        if (value != null) ret[a] = (double)value;
                    }
                    return ret;
                }
            }
            else if (fi.Extension.EndsWith("chrg", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new StringReader(filename))
                {
                    reader.ReadLine();
                    int count = int.Parse(reader.ReadLine());

                    if (count != structure.Atoms.Count)
                    {
                        throw new ArgumentException(string.Format("Atom counts do not match in '{0}'. Got '{1}', expected '{2}'.", filename, count, structure.Atoms.Count));
                    }

                    Dictionary<IAtom, double> values = new Dictionary<IAtom, double>(count);

                    List<Tuple<int, ElementSymbol, double>> records = new List<Tuple<int, ElementSymbol, double>>();

                    var sep = " ".ToCharArray();
                    for (int i = 0; i < count; i++)
                    {
                        var line = reader.ReadLine();
                        var fields = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length == 3) records.Add(Tuple.Create(int.Parse(fields[0]), ElementSymbol.Create(fields[1]), fields[2].ToDouble().GetValue()));
                    }

                    for (int i = 0; i < count; i++)
                    {
                        var entry = records[i];

                        if (structure.Atoms[i].ElementSymbol != entry.Item2)
                        {
                            throw new ArgumentException(string.Format("Element symbols do not match in '{0}' on line {1}. Got '{2}', expected '{3}'.", filename, i + 3, entry.Item2, structure.Atoms[i].ElementSymbol));
                        }

                        values[structure.Atoms[i]] = entry.Item3;
                    }

                    return values;
                }
            }

            throw new ArgumentException(string.Format("Invalid file format in '{0}'. Expected '.*chrg' or '.wprop'.", filename));
        }

        public static AtomValueField FromFile(string name, string filename, IStructure structure, bool ignoreHydrogens, KDAtomTree atomPivots, K3DTree<PdbResidue> residuePivots, AtomValueFieldInterpolationMethod method, double radius = 5.0, int K = 5)
        {
            return FromValues(name, structure, ignoreHydrogens, atomPivots, residuePivots, ReadValues(filename, structure), method, radius, K);
        }

        public static AtomValueField FromValues(string name, IStructure structure, bool ignoreHydrogens, KDAtomTree atomPivots, K3DTree<PdbResidue> residuePivots, Dictionary<IAtom, double> values, AtomValueFieldInterpolationMethod method, double radius = 5.0, int K = 5)
        { 
            var ret = new AtomValueField(name)
            {
                IgnoreHydrogens = ignoreHydrogens,
                AtomPivots = atomPivots,
                ResiduePivots = residuePivots,
                Method = method,
                Radius = radius,
                Values = values,
                Structure = structure,
                K = K
            };

            switch (method)
            {
                case AtomValueFieldInterpolationMethod.NearestValue: ret.InterpolationFuncCenter = ret.InterpolateNearest; break;
                case AtomValueFieldInterpolationMethod.RadiusSum: ret.InterpolationFuncCenter = ret.InterpolateRadiusSum; break;
                case AtomValueFieldInterpolationMethod.RadiusSumDividedByDistance: ret.InterpolationFuncCenter = ret.InterpolateRadiusSumDividedByDistance; break;
                case AtomValueFieldInterpolationMethod.RadiusMultiplicativeScale: ret.InterpolationFuncNode = ret.InterpolateRadiusMultiplicative; break;
                case AtomValueFieldInterpolationMethod.RadiusAdditiveScale: ret.InterpolationFuncNode = ret.InterpolateRadiusAdditive; break;
                case AtomValueFieldInterpolationMethod.KNearestSum: ret.InterpolationFuncCenter = ret.InterpolateKNearestSum; break;
                case AtomValueFieldInterpolationMethod.KNearestSumDividedByDistance: ret.InterpolationFuncCenter = ret.InterpolateKNearestSumDividedByDistance; break;
                case AtomValueFieldInterpolationMethod.WholeStructure: ret.InterpolationFuncCenter = ret.InterpolateWholeStructure; break;
                case AtomValueFieldInterpolationMethod.Lining: ret.InterpolationFuncNode = ret.InterpolateLining; break;
                case AtomValueFieldInterpolationMethod.KNearestResidueSum: ret.InterpolationFuncCenter = ret.InterpolateKNearestResidueSum; break;
                case AtomValueFieldInterpolationMethod.KNearestResidueSumDividedByDistance: ret.InterpolationFuncCenter = ret.InterpolateKNearestResidueSumDividedByDistance; break;
                case AtomValueFieldInterpolationMethod.RadiusResidueSum: ret.InterpolationFuncCenter = ret.InterpolateRadiusResidueSum; break;
                case AtomValueFieldInterpolationMethod.RadiusResidueSumDividedByDistance: ret.InterpolationFuncCenter = ret.InterpolateRadiusResidueSumDividedByDistance; break;
                case AtomValueFieldInterpolationMethod.AllResidues: ret.InterpolationFuncCenter = ret.InterpolateAllResidues; break;
            }

            if (ret.InterpolationFuncNode == null) ret.InterpolationFuncNode = ret.WrapNodeToCenter;
            if (ret.InterpolationFuncCenter == null) ret.InterpolationFuncCenter = _ => null;

            return ret;
        }
        
        protected AtomValueField(string name)
            : base(name)
        {

        }
    }
}
