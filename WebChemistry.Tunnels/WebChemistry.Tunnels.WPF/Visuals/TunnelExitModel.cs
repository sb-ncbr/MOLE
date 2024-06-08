/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using WebChemistry.Tunnels.Core;
using WebChemistry.Util;
using WebChemMath = WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    class TunnelExitModel
    {
        static readonly double radius = 0.5;
        static readonly Geometry3D geometry, smallGeometry;

        static TunnelExitModel()
        {
            var builder = new MeshBuilder(true, false);
            builder.AddArrow(new Point3D(0, 0, 0), new Point3D(4, 0, 0), 2 * radius, 2);
            geometry = builder.ToMesh();
            
            builder = new MeshBuilder(true, false);
            builder.AddArrow(new Point3D(0, 0, 0), new Point3D(2, 0, 0), 0.5, 2);
            smallGeometry = builder.ToMesh();
        }

        public Model3D Model { get; private set; }

        SolidColorBrush brush;

        public void SetColor(Color color)
        {
            brush.Color = color;
        }

        public static Vector3D GetNormal(Facet facet)
        {
            Point3D[] vert = facet.Vertices.Select(p => new Point3D(p.Position.X, p.Position.Y, p.Position.Z)).ToArray();

            int i = 0, j = 1, k = 2;
            Vector3D u = new Vector3D(facet.Vertices[j].Position.X - facet.Vertices[i].Position.X, facet.Vertices[j].Position.Y - facet.Vertices[i].Position.Y, facet.Vertices[j].Position.Z - facet.Vertices[i].Position.Z);
            Vector3D v = new Vector3D(facet.Vertices[k].Position.X - facet.Vertices[i].Position.X, facet.Vertices[k].Position.Y - facet.Vertices[i].Position.Y, facet.Vertices[k].Position.Z - facet.Vertices[i].Position.Z);

            var n = Vector3D.CrossProduct(u, v);
            n.Normalize();
            var d = -(n.X * facet.Vertices[i].Position.X + n.Y * facet.Vertices[i].Position.Y + n.Z * facet.Vertices[i].Position.Z);
            var pivot = facet.Pivot.Atom.Position;
            var t = n.X * pivot.X + n.Y * pivot.Y + n.Z * pivot.Z + d;

            if (t >= 0)
            {
                n.Negate();
            }

            return n;
        }

        public TunnelExitModel(Color color)
        {
            //FromRgb(0xD2, 0x62, 0x22)
            this.brush = new SolidColorBrush(color) { Opacity = 0.65 };
            DiffuseMaterial diffuse = new DiffuseMaterial(brush);
            var material = new MaterialGroup { Children = new MaterialCollection { diffuse, new SpecularMaterial(Brushes.Beige, 6.0) } };
            this.Model = new GeometryModel3D
            {
                Geometry = geometry,
                Material = material
            };

            //this.Model = new Model3DGroup();
        }

        public static Transform3D GetTransform(Facet facet)
        {
            var dir = GetNormal(facet);
            var origin = 0.333333 * facet.Vertices.Aggregate(new Vector3D(), (a, v) => a + new Vector3D(v.Atom.Position.X, v.Atom.Position.Y, v.Atom.Position.Z));// boundaryFaces.SelectMany(f => f.Vertices).Aggregate(new Vector3D(), (a, v) => a + new Vector3D(v.Atom.Position.X, v.Atom.Position.Y, v.Atom.Position.Z)) / (3.0 * boundaryFaces.Count());

            var axis = Vector3D.CrossProduct(dir, new Vector3D(1, 0, 0));
            var angle = WebChemMath.MathHelper.RadiansToDegrees(Math.Acos(dir.X));

            return new Transform3DGroup()
            {
                Children = new Transform3DCollection()
                {
                    new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(axis.X, axis.Y, axis.Z), -angle)),
                    new TranslateTransform3D(origin.X, origin.Y, origin.Z)
                }
            };
        }

        public TunnelExitModel(Facet facet)
        {
            var dir = GetNormal(facet);
            var origin = 0.333333 * facet.Vertices.Aggregate(new Vector3D(), (a, v) => a + new Vector3D(v.Atom.Position.X, v.Atom.Position.Y, v.Atom.Position.Z));// boundaryFaces.SelectMany(f => f.Vertices).Aggregate(new Vector3D(), (a, v) => a + new Vector3D(v.Atom.Position.X, v.Atom.Position.Y, v.Atom.Position.Z)) / (3.0 * boundaryFaces.Count());

            var axis = Vector3D.CrossProduct(dir, new Vector3D(1, 0, 0));
            var angle = WebChemMath.MathHelper.RadiansToDegrees(Math.Acos(dir.X));

            //FromRgb(0xD2, 0x62, 0x22)
            var brush = new SolidColorBrush(Colors.Red) { Opacity = 0.65 };
            DiffuseMaterial diffuse = new DiffuseMaterial(brush);
            var material = new MaterialGroup { Children = new MaterialCollection { diffuse, new SpecularMaterial(Brushes.Beige, 6.0) } };
            this.Model = new GeometryModel3D
            {
                Geometry = smallGeometry,
                Material = material,
                Transform = new Transform3DGroup()
                {
                    Children = new Transform3DCollection()
                    {
                        new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(axis.X, axis.Y, axis.Z), -angle)),
                        new TranslateTransform3D(origin.X, origin.Y, origin.Z)
                    }
                }
            };

            //this.Model = new Model3DGroup();
        }

        public TunnelExitModel(Tetrahedron exit, Cavity cavity)
        {
            var faces = Facet.Boundary(exit, cavity.CavityGraph.AdjacentEdges(exit).Select(e => e.Other(exit)).ToArray());
            var sum = faces.Sum(f => f.GetArea());
            var dir = 1.0 / sum * faces.Select(f => new { N = GetNormal(f), W = f.GetArea() }).Aggregate(new Vector3D(), (a, v) => a + v.W * v.N);
            dir.Normalize();
            var origin = //0.333333 * face.Vertices.Aggregate(new Vector3D(), (a, v) => a + new Vector3D(v.Atom.Position.X, v.Atom.Position.Y, v.Atom.Position.Z));

                exit.Center; // boundaryFaces.SelectMany(f => f.Vertices).Aggregate(new Vector3D(), (a, v) => a + new Vector3D(v.Atom.Position.X, v.Atom.Position.Y, v.Atom.Position.Z)) / (3.0 * boundaryFaces.Count());

            var axis = Vector3D.CrossProduct(dir, new Vector3D(1, 0, 0));
            var angle = WebChemMath.MathHelper.RadiansToDegrees(Math.Acos(dir.X));

            //FromRgb(0xD2, 0x62, 0x22)
            var brush = new SolidColorBrush(Colors.Red) { Opacity = 0.65 };
            DiffuseMaterial diffuse = new DiffuseMaterial(brush);
            var material = new MaterialGroup { Children = new MaterialCollection { diffuse, new SpecularMaterial(Brushes.Beige, 6.0) } };
            this.Model = new GeometryModel3D
            {
                Geometry = smallGeometry,
                Material = material,
                Transform = new Transform3DGroup()
                {
                    Children = new Transform3DCollection()
                    {
                        new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(axis.X, axis.Y, axis.Z), -angle)),
                        new TranslateTransform3D(origin.X, origin.Y, origin.Z)
                    }
                }
            };

            //this.Model = new Model3DGroup();
        }
    }
}