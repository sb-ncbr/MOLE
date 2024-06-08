/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */


namespace WebChemistry.Tunnels.Core.Export
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Core;
    using WebChemistry.Tunnels.Core.Geometry;

    public enum SurfaceType
    {
        Surface = 0,
        Spheres
    }

    public enum PyMolSpectrumPalette
    {
        RedWhiteBlue = 0,
        BlueWhiteRed
    }

    public class PyMolExporter
    {
        static CultureInfo Culture = CultureInfo.InvariantCulture;
        static string[] Colors = new string[] 
        {
            "red", "green", "blue", "yellow", "violet", "cyan", "salmon", "lime",
            "pink", "slate", "magenta", "orange", "marine", "olive", "purple", "teal",
            "forest", "firebrick", "chocolate", "wheat", "white", "grey"
        };

        int ColorIndex;
        TextWriter Writer;
        Vector3D Offset;

        public PyMolExporter AddTunnel(string commandName, Tunnel t, SurfaceType surfaceType = SurfaceType.Surface)
        {
            Writer.WriteLine("def {0}():", commandName);
            Writer.WriteLine("  model = chempy.models.Indexed()", commandName);
            var ctp = t.Profile;
            int index = 0;
            foreach (var n in ctp)
            {
                Writer.WriteLine(string.Format(Culture, "  addAtom(model,'{0}',{1:0.000},{2:0.000},{3:0.000},{4:0.000})", index, n.Radius, n.Center.X + Offset.X, n.Center.Y + Offset.Y, n.Center.Z + Offset.Z));
                index++;
            }
            string nameString = "'" + commandName + "'";
            var color = "'" + Colors[(ColorIndex++) % Colors.Length] + "'";
            new[] 
            {
                "for a in range(len(model.atom)-1):",
                "  b = chempy.Bond()",
                "  b.index = [a,a+1]",
                "  model.bond.append(b)",
                "cmd.set('surface_mode', 1)",
                "cmd.set('sphere_mode', 5)",
                "cmd.set('mesh_mode', 1)",
                string.Format("cmd.load_model(model, {0})", nameString),
                string.Format("cmd.hide('everything', {0})", nameString),
                surfaceType == SurfaceType.Surface ? string.Format("cmd.set('surface_color', {0}, {1})", color, nameString) : string.Format("cmd.set('sphere_color', {0}, {1})", color, nameString),
                surfaceType == SurfaceType.Surface ? string.Format("cmd.show('surface', {0})", nameString) : string.Format("cmd.show('spheres', {0})", nameString),
            }.ForEach(l => Writer.WriteLine("  " + l));
            Writer.WriteLine("{0}()", commandName);
            Writer.WriteLine("cmd.group('{0}s', [{1}], 'add')", GetCommandName(t), commandName);
            Writer.WriteLine("");
            
            return this;
        }

        public PyMolExporter AddCenterlineChargeTunnel(string commandName, string groupName, Tunnel t, string name, double? customMin, double? customMax, PyMolSpectrumPalette spectrum, SurfaceType surfaceType = SurfaceType.Surface)
        {
            Writer.WriteLine("def {0}():", commandName);
            Writer.WriteLine("  model = chempy.models.Indexed()", commandName);
            var values = t.ScalarFields[name].GetValues(t.Profile);
            int index = 0;
            foreach (var n in t.Profile)
            {
                var v = values[n];
                Writer.WriteLine(string.Format(Culture, "  addAtom(model,'{0}',{1:0.000},{2:0.000},{3:0.000},{4:0.000},{5:0.000})", 
                    index, n.Radius, 
                    n.Center.X + Offset.X, n.Center.Y + Offset.Y, n.Center.Z + Offset.Z,
                    v));
                index++;
            }
            string nameString = "'" + commandName + "'";
            var color = "'" + Colors[(ColorIndex++) % Colors.Length] + "'";
            customMin = customMin ?? t.ScalarFields[name].CenterlineMinValue;
            customMax = customMax ?? t.ScalarFields[name].CenterlineMaxValue;
            new[] 
            {
                "for a in range(len(model.atom)-1):",
                "  b = chempy.Bond()",
                "  b.index = [a,a+1]",
                "  model.bond.append(b)",
                "cmd.set('surface_mode', 1)",
                "cmd.set('sphere_mode', 5)",
                "cmd.set('mesh_mode', 1)",
                string.Format("cmd.load_model(model, {0})", nameString),
                string.Format("cmd.hide('everything', {0})", nameString),
                string.Format("cmd.spectrum('pc', '{0}', '{1}', minimum = {2:0.000}, maximum = {3:0.000})", spectrum == PyMolSpectrumPalette.RedWhiteBlue ? "red_white_blue" : "blue_white_red", commandName, customMin.Value, customMax.Value),
                surfaceType == SurfaceType.Surface ? string.Format("cmd.show('surface', {0})", nameString) : string.Format("cmd.show('spheres', {0})", nameString),
            }.ForEach(l => Writer.WriteLine("  " + l));
            Writer.WriteLine("{0}()", commandName);
            Writer.WriteLine("cmd.group('{0}', [{1}], 'add')", groupName, commandName);
            Writer.WriteLine("");

            return this;
        }
        
        void WriteVertex(string offset, Vector3D position)
        {
            Writer.WriteLine(string.Format(Culture, "{0}VERTEX,{1:0.000},{2:0.000},{3:0.000},", offset, position.X + Offset.X, position.Y + Offset.Y, position.Z + Offset.Z));
        }

        void WriteNormal(string offset, Vector3D normal)
        {
            Writer.WriteLine(string.Format(Culture, "{0}NORMAL,{1:0.000},{2:0.000},{3:0.000},", offset, normal.X, normal.Y, normal.Z));
        }

        void WriteColor(string offset, Vector3D color)
        {
            Writer.WriteLine(string.Format(Culture, "{0}COLOR,{1:0.000},{2:0.000},{3:0.000},", offset, color.X, color.Y, color.Z));
        }

        void WriteSurface(TriangulatedSurface surface, double alpha, Vector3D color, string offset = "    ")
        {
            Writer.WriteLine("{0}BEGIN,TRIANGLES,", offset);
            Writer.WriteLine("{0}ALPHA,{1},", offset, alpha.ToStringInvariant("0.00"));
            Writer.WriteLine("{0}COLOR,{1},{2},{3},", offset, color.X.ToStringInvariant("0.00"), color.Y.ToStringInvariant("0.00"), color.Z.ToStringInvariant("0.00"));
            
            foreach (var t in surface.Triangles)
            {
                WriteVertex(offset, t.A.Position);
                WriteVertex(offset, t.B.Position);
                WriteVertex(offset, t.C.Position);
            }

            Writer.WriteLine("{0}END",offset);
        }

        void WriteVectorFieldSurface(SurfaceVectorField field, double? customMin, double? customMax, Func<double?, double, double, Vector3D> colorFunc, string offset = "    ")
        {
            Writer.WriteLine("{0}BEGIN,TRIANGLES,", offset);
            Writer.WriteLine("{0}ALPHA,1.0,", offset);

            double min = customMin ?? field.MinMagnitude, max = customMax ?? field.MaxMagnitude;
            foreach (var t in field.Surface.Triangles)
            {
                WriteColor(offset, colorFunc(field.Values[t.A.Id], min, max));
                WriteNormal(offset, t.A.Normal);
                WriteVertex(offset, t.A.Position);
                WriteColor(offset, colorFunc(field.Values[t.B.Id], min, max));
                WriteNormal(offset, t.B.Normal);
                WriteVertex(offset, t.B.Position);
                WriteColor(offset, colorFunc(field.Values[t.C.Id], min, max));
                WriteNormal(offset, t.C.Normal);
                WriteVertex(offset, t.C.Position);
            }

            Writer.WriteLine("{0}END,", offset);
            Writer.WriteLine("{0}LINEWIDTH,2,", offset);
            Writer.WriteLine("{0}BEGIN,LINES,", offset);
            foreach (var v in field.Surface.Vertices)
            {
                WriteColor(offset, colorFunc(field.Values[v.Id], field.MinMagnitude, field.MaxMagnitude));
                WriteVertex(offset, v.Position);
                WriteVertex(offset, v.Position + 3 * field.Field[v.Id]);
            }
            Writer.WriteLine("{0}END", offset);
        }

        void WriteScalarFieldSurface(SurfaceScalarField field, double? customMin, double? customMax, Func<double?, double, double, Vector3D> colorFunc, string offset = "    ")
        {
            Writer.WriteLine("{0}BEGIN,TRIANGLES,", offset);
            Writer.WriteLine("{0}ALPHA,1.0,", offset);

            double min = customMin ?? field.MinMagnitude, max = customMax ?? field.MaxMagnitude;
            foreach (var t in field.Surface.Triangles)
            {
                WriteColor(offset, colorFunc(field.Values[t.A.Id], min, max));
                WriteNormal(offset, t.A.Normal);
                WriteVertex(offset, t.A.Position);
                WriteColor(offset, colorFunc(field.Values[t.B.Id], min, max));
                WriteNormal(offset, t.B.Normal);
                WriteVertex(offset, t.B.Position);
                WriteColor(offset, colorFunc(field.Values[t.C.Id], min, max));
                WriteNormal(offset, t.C.Normal);
                WriteVertex(offset, t.C.Position);
            }

            Writer.WriteLine("{0}END", offset);
        }

        public PyMolExporter AddCavity(string commandName, Cavity cavity)
        {
            TriangulatedSurface inner, boundary;
            TriangulatedSurface.FromCavity(cavity, out inner, out boundary);
            
            Writer.WriteLine("def {0}():", commandName);
            Writer.WriteLine("  data = [");
            WriteSurface(inner, 0.3, new Vector3D(0.65, 0.65, 0.65));
            Writer.WriteLine("    ,");
            WriteSurface(boundary, 0.3, new Vector3D(0, 1.0, 0.0));
            Writer.WriteLine("  ]");
            Writer.WriteLine("  cmd.load_cgo(data,'{0}')", commandName);
            Writer.WriteLine("  cmd.hide('everything', '{0}')", commandName);
            Writer.WriteLine("{0}()", commandName);
            Writer.WriteLine("cmd.group('Interior', [{0}], 'add')", commandName);
            Writer.WriteLine("");

            return this;
        }

        public PyMolExporter AddSurfaceField(string commandName, string groupName, SurfaceScalarField field, double? customMin, double? customMax, PyMolSpectrumPalette spectrum)
        {
            Func<double?, double, double, Vector3D> colorFunc;
            if (spectrum == PyMolSpectrumPalette.RedWhiteBlue) colorFunc = ExportColoringUtils.GetFieldColorRedWhiteBlue;
            else colorFunc = ExportColoringUtils.GetFieldColorBlueWhiteRed;

            Writer.WriteLine("def {0}():", commandName);
            Writer.WriteLine("  data = [");
            WriteScalarFieldSurface(field, customMin, customMax, colorFunc);
            Writer.WriteLine("  ]");
            Writer.WriteLine("  cmd.load_cgo(data,'{0}')", commandName);
            Writer.WriteLine("  cmd.hide('everything', '{0}')", commandName);
            Writer.WriteLine("{0}()", commandName);
            Writer.WriteLine("cmd.group('{0}', [{1}], 'add')", groupName, commandName);
            Writer.WriteLine("cmd.show('cgo', '{0}')", commandName);
            Writer.WriteLine("");
            return this;
        }

        public PyMolExporter AddFetch(string id)
        {
            Writer.WriteLine("cmd.fetch('{0}')", id.ToUpperInvariant());
            return this;
        }

        public PyMolExporter AddStructure(string fileName) {
            Writer.WriteLine(string.Format("if os.path.isfile('{0}'):", fileName));
            Writer.WriteLine(string.Format("  cmd.load('{0}')", fileName));
            return this;
        }

        public string GetCommandName(Tunnel t)
        {
            if (t.Type == TunnelType.Pore) return t.IsMergedPore ? "MergedPore" : "Pore";
            return t.Type.ToString();
        }


        public PyMolExporter AddTunnels(IEnumerable<Tunnel> tunnels, SurfaceType surfaceType = SurfaceType.Surface)
        {
            tunnels.ForEach((t, i) => AddTunnel(GetCommandName(t) + (i + 1).ToString(), t, surfaceType));
            return this;
        }

        public PyMolExporter AddFields(IEnumerable<Tunnel> tunnels, string fieldName, double? customMin, double? customMax, PyMolSpectrumPalette spectrum)
        {
            tunnels.ForEach((t, i) => AddSurfaceField(GetCommandName(t) + (i + 1).ToString() + "_" + fieldName + "_potential", GetCommandName(t) + "s_" + fieldName + "_potential", 
                t.ScalarFields[fieldName].Surface, customMin, customMax, spectrum));
            return this;
        }

        public PyMolExporter AddCenterlineChargeTunnels(IEnumerable<Tunnel> tunnels, string fieldName, double? customMin, double? customMax, PyMolSpectrumPalette spectrum, SurfaceType surfaceType = SurfaceType.Surface)
        {
            tunnels.ForEach((t, i) => AddCenterlineChargeTunnel(GetCommandName(t) + (i + 1).ToString() + "_" + fieldName + "_centerline", GetCommandName(t) + "s_" + fieldName + "_centerline", 
                t, fieldName, customMin, customMax, spectrum, surfaceType));
            return this;
        }

        public PyMolExporter AddCavities(string prefix, IEnumerable<Cavity> cavities)
        {
            cavities.ForEach((c, i) => AddCavity(prefix + (i + 1).ToString(), c));
            return this;
        }

        public PyMolExporter AddCenterlineSpectrum(ExportSpectrumInfo info)
        {
            // TODO
            // use Writer to write the code
            return this;
        }

        public PyMolExporter AddSurfaceSpectrum(ExportSpectrumInfo info)
        {
            // TODO
            // use Writer to write the code
            return this;
        }

        public PyMolExporter(TextWriter writer, Vector3D? offset = null, string[] header = null)
        {
            this.Writer = writer;
            this.Offset = offset.HasValue ? offset.Value : new Vector3D();
            foreach (var l in header ?? new string[0])
            {
                writer.WriteLine("# " + l);
            }
            writer.WriteLine("");
            writer.WriteLine("from pymol import cmd");
            writer.WriteLine("from pymol.cgo import *");
            writer.WriteLine("import chempy");
            writer.WriteLine("import os");

            new[] 
            {
                "def addAtom(model, name, vdw, x, y, z, partialCharge = 0.0):",
                "  a = chempy.Atom()",
                "  a.name = name",
                "  a.vdw = vdw",
                "  a.coord = [x, y, z]",
                "  a.partial_charge = partialCharge",
                "  model.atom.append(a)"
            }.ForEach(l => writer.WriteLine(l));
        }
    }
}
