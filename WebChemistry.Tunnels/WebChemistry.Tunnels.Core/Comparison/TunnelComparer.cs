/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Comparison
{
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Geometry;

    public class StructureComparisonInfo
    {
        public IStructure Structure { get; set; }
        public TunnelCollection Tunnels { get; set; }
        public IEnumerable<PdbResidue> Anchors { get; set; }
    }

    public class CompareResult
    {
        public Matrix3D[] Rotations { get; set; }
        public Vector3D[] Offsets { get; set; }
    }

    public class TunnelComparer
    {
        static double TunnelDistance(Vector3D[] xs, K3DTree<Vector3D> tx, Vector3D[] ys, K3DTree<Vector3D> ty)
        {
            double d1 = 0, d2 = 0;
            foreach (var x in xs)
            {
                var y = ty.Nearest(x);
                d1 += y.Priority;// .DistanceToSquared(y);
            }
            d1 = Math.Sqrt(d1 / (double)xs.Length);

            foreach (var y in ys)
            {
                var x = tx.Nearest(y);
                d2 += x.Priority;// .DistanceToSquared(y);
            }
            d2 = Math.Sqrt(d2 / (double)ys.Length);
            
            return (d1 + d2) / 2.0;
        }

        static K3DTree<Vector3D> CreateTree(Vector3D[] points)
        {
            return K3DTree.Create(points, p => p);

            //var tree = new K3DTreeOld<Vector3D>();

            //foreach (var p in points)
            //{
            //    try
            //    {
            //        tree.Insert(p, p);
            //    }
            //    catch
            //    {
            //    }
            //}

            //return tree;
        }

        public static void Compare(IEnumerable<StructureComparisonInfo> structures, out double distance, out Matrix3D[] rotations, out Vector3D[] offsets)
        {
            //ISuperimposer si = new SiteBinderSuperimposer();
            //var ss = structures.Select(s => s.Anchors.Select(a => a.Atoms.GeometricalCenter()));
            
            //si.GetTransforms(ss.ToList(), out rotations, out offsets);

            //Func<TunnelCollection, Vector3D[]> getPoints = tns => tns.SelectMany(t => t.GetProfile(4).Select(n => n.Center)).ToArray();

            //Vector3D[] offsets_ = offsets;

            //var data = Enumerable.Zip(Enumerable.Zip(rotations, offsets, (r, o) => new { R = r, O = o }), structures,
            //    (t, s) =>
            //    {
            //        var points = getPoints(s.Tunnels).Select(p => t.R.Transform(p - t.O) + offsets_[0]).ToArray();
            //        return new
            //        {
            //            Transform = t,
            //            Structure = s,
            //            Points = points,
            //            KDTree = CreateTree(points)
            //        };
            //    })
            //        .ToArray();

            //var pivot = data.First();
            //var averageDistance = data.Skip(1).Average(d => TunnelDistance(pivot.Points, pivot.KDTree, d.Points, d.KDTree));

            //distance = averageDistance;
            distance = 0;
            rotations = null;
            offsets = null;
        }
    }
}
