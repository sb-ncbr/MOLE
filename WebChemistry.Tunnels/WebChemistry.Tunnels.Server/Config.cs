/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Server
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Tunnels.Core;
    using WebChemistry.Tunnels.Core.Export;
    using WebChemistry.Tunnels.Core.Geometry;

    class ResidueHandle
    {
        public string Chain { get; set; }
        public char InsertionCode { get; set; }
        public int SequenceNumber { get; set; }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}", string.IsNullOrWhiteSpace(Chain) ? "" : Chain.ToString() + " ", SequenceNumber, InsertionCode == ' ' ? "" : " " + InsertionCode.ToString());
        }

        public static ResidueHandle FromXml(XElement xml)
        {
            var ret = new ResidueHandle();
            bool hasSN = false;

            foreach (var attr in xml.Attributes())
            {
                switch (attr.Name.LocalName)
                {
                    case "Chain": 
                        {
                            if (string.IsNullOrEmpty(attr.Value)) ret.Chain = "";
                            else ret.Chain = attr.Value;
                        }
                        break;
                    case "SequenceNumber":
                        try
                        {
                            ret.SequenceNumber = (int)ConfigBase.GetInteger(attr.Value);
                            hasSN = true;
                        }
                        catch (Exception e)
                        {
                             throw new ArgumentException(ConfigBase.AttributeParseError("SequenceNumber", xml.Name.LocalName, e.Message));
                        }
                        break;
                    case "InsertionCode":
                        {
                            if (string.IsNullOrEmpty(attr.Value)) ret.InsertionCode = ' ';
                            else if (attr.Value.Length == 1) ret.InsertionCode = attr.Value[0];
                            else throw new ArgumentException(string.Format("Invalid insertion code '{0}'.", attr.Value));
                        }
                        break;
                    case "Name":
                        break;
                    default:
                        throw new ArgumentException(ConfigBase.UnknownAttributeError(attr.Name.LocalName, xml.Name.LocalName));
                }
            }

            if (!hasSN)
            {
                throw new ArgumentException(string.Format("Missing SequenceNumber attribute in <{0}>.", xml.Name.LocalName));
            }

            return ret;
        }

        public ResidueHandle()
        {
            Chain = "";
            InsertionCode = ' ';
        }
    }

    class QueryHandle
    {
        public string Text { get; set; }
        public QueryMotive Query { get; set; }

        public override string ToString()
        {
            return Text;
        }

        public static QueryHandle FromXml(XElement xml)
        {            
            var t = xml.Value;
            var q = PatternQueryHelper.CompileMotive(t);

            return new QueryHandle { Text = t, Query = q };
        }
    }

    class Point3DHandle
    {
        public Vector3D Point { get; set; }

        public override string ToString()
        {
            return Point.ToString();
        }

        public static Point3DHandle FromXml(XElement xml)
        {
            double? x = null, y = null, z = null;

            foreach (var attr in xml.Attributes())
            {
                switch (attr.Name.LocalName)
                {
                    case "X":
                        try
                        {
                            x = (double)ConfigBase.GetDouble(attr.Value);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException(ConfigBase.AttributeParseError("X", xml.Name.LocalName, e.Message));
                        }
                        break;
                    case "Y":
                        try
                        {
                            y = (double)ConfigBase.GetDouble(attr.Value);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException(ConfigBase.AttributeParseError("Y", xml.Name.LocalName, e.Message));
                        }
                        break;
                    case "Z":
                        try
                        {
                            z = (double)ConfigBase.GetDouble(attr.Value);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException(ConfigBase.AttributeParseError("Z", xml.Name.LocalName, e.Message));
                        }
                        break;
                }
            }

            if (x == null || y == null || z == null)
            {
                throw new ArgumentException(string.Format("Missing X, Y or Z coordinate in <{0}>.", xml.Name.LocalName));
            }

            return new Point3DHandle { Point = new Vector3D(x.Value, y.Value, z.Value) };
        }

        public Point3DHandle()
        {
            
        }
    }

    class ConfigElement<TConfig>
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
        public object Value { get; set; }
        public Type Type { get; set; }
        public Action<TConfig, object> Apply { get; set; }
    }
    
    abstract class ConfigBase
    {
        public static string[] MakeLines(string str, int maxLen)
        {
            if (str.Length <= maxLen) return new[] { str };
            var index = str.LastIndexOf(' ', maxLen);
            if (index < 0) return new[] { str };
            var line = str.Substring(0, index);
            return new[] { line }.Concat(MakeLines(str.Substring(index + 1), maxLen)).ToArray();
        }

        static string GetTypeName(Type type)
        {
            if (type == typeof(double) || type == typeof(double?)) return "real";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            return "string";
            //if (type == typeof(string)) return "string";
            //if (type == typeof(TunnelWeightFunction)) return GetEnum<TunnelWeightFunction>;
            //if (type == typeof(ShowTunnelType)) return GetEnum<ShowTunnelType>;
        }
        
        public static string MakeAttributeHelp<TConfig>(ConfigElement<TConfig> attribute)
        {
            StringBuilder sb = new StringBuilder();
            string defaultText;
            if ((attribute.DefaultValue is string && string.IsNullOrEmpty(attribute.DefaultValue as string)) || attribute.DefaultValue == null)
            {
                defaultText = "";
            }
            else
            {
                defaultText = string.Format(", Default value = {0}", attribute.DefaultValue);
            }
            sb.AppendFormat("  {0}\n    Type [{1}]{2} \n", attribute.Name, GetTypeName(attribute.Type), defaultText);
            foreach (var line in MakeLines(attribute.Description, 70))
            {
                sb.AppendFormat("      {0}\n", line);
            }
            return sb.ToString();
        }

        public static string MakeAttributeWikiEntry<TConfig>(ConfigElement<TConfig> attribute)
        {
            string defaultText;
            if ((attribute.DefaultValue is string && string.IsNullOrEmpty(attribute.DefaultValue as string)) || attribute.DefaultValue == null)
            {
                defaultText = "''none''";
            }
            else
            {
                defaultText = attribute.DefaultValue.ToString();
                if (string.IsNullOrEmpty(defaultText)) defaultText = "\"\"";
            }

            return string.Join(Environment.NewLine, new[]
            {
                string.Format("* '''{0}''' [{1}], Default value = {2}", attribute.Name, GetTypeName(attribute.Type), defaultText),
                //string.Format(": Type = [{0}], Default value = {1}", GetTypeName(attribute.Type), defaultText),
                string.Format(": ''{0}''", attribute.Description)
            });
        }

        public static string MakeCategoryAttributeHelp<TConfig>(ConfigElement<TConfig> attribute)
        {
            StringBuilder sb = new StringBuilder();
            string defaultText;
            if ((attribute.DefaultValue is string && string.IsNullOrEmpty(attribute.DefaultValue as string)) || attribute.DefaultValue == null)
            {
                defaultText = "";
            }
            else
            {
                defaultText = string.Format(", Default value = {0}", attribute.DefaultValue);
            }
            sb.AppendFormat("    {0}\n      Type [{1}]{2} \n", attribute.Name, GetTypeName(attribute.Type), defaultText);
            foreach (var line in MakeLines(attribute.Description, 60))
            {
                sb.AppendFormat("        {0}\n", line);
            }
            return sb.ToString();
        }

        public static object GetEnum<T>(string value)
            where T : struct
        {
            T ret;
            if (!Enum.TryParse<T>(value, true, out ret))
            {
                throw new ArgumentException(string.Format("Expected a value from {{{0}}}, got '{1}'.", string.Join(", ", Enum.GetNames(typeof(T)).ToArray()), value));
            }
            return ret;
        }
        
        public static object GetDouble(string value)
        {
            double ret;
            if (!double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ret))
            {
                throw new ArgumentException(string.Format("Expected a real number (i.e. 1.25), got '{0}'.", value));
            }
            return ret;
        }

        public static object GetNullableDouble(string value)
        {
            double ret;
            if (!double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ret))
            {
                return new Nullable<double>();
            }
            return new Nullable<double>(ret);
        }

        public static object GetInteger(string value)
        {
            int ret;
            if (!int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out ret))
            {
                throw new ArgumentException(string.Format("Expected an integer (i.e. 10), got '{0}'.", value));
            }
            return ret;
        }

        public static object GetBool(string value)
        {
            if (value.Equals("0", StringComparison.OrdinalIgnoreCase) || value.Equals("False", StringComparison.OrdinalIgnoreCase)) return false;
            if (value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("True", StringComparison.OrdinalIgnoreCase)) return true;

            throw new ArgumentException(string.Format("Expected a boolean (0/1 or True/False), got '{0}'.", value));
        }

        public static object GetString(string value)
        {
            return value;
        }

        public static Func<string, object> GetParser(Type type)
        {
            if (type == typeof(double)) return GetDouble;
            if (type == typeof(double?)) return GetNullableDouble;
            if (type == typeof(int)) return GetInteger;
            if (type == typeof(bool)) return GetBool;
            if (type == typeof(string)) return GetString;
            if (type == typeof(TunnelWeightFunction)) return GetEnum<TunnelWeightFunction>;
            if (type == typeof(SurfaceType)) return GetEnum<SurfaceType>;
            if (type == typeof(PyMolSpectrumPalette)) return GetEnum<PyMolSpectrumPalette>;
            if (type == typeof(AtomValueFieldInterpolationMethod)) return GetEnum<AtomValueFieldInterpolationMethod>;

            throw new ArgumentException(string.Format("No parser available for type '{0}'.", type.Name.ToString()));
        }

        public static string UnknownAttributeError(string attr, string e)
        {
            return string.Format("Unknown attribute '{0}' in element <{1}>.", attr, e);
        }

        public static string UnknownElementError(string name, string e)
        {
            return string.Format("Unknown element '{0}' in element <{1}>.", name, e);
        }

        public static string AttributeParseError(string attr, string e, string error)
        {
            return string.Format("Error parsing attribute '{0}' in element <{1}>: {2}", attr, e, error);
        }

        public static IList<string> ParseConfig<TConfig>(XElement xml, TConfig config, ConfigElement<TConfig>[] elements)
        {
            var map = elements.GroupBy(e => e.Category).ToDictionary(
                c => c.Key,
                c => c.ToDictionary(e => e.Name, e => e, StringComparer.Ordinal), 
                StringComparer.Ordinal);
            List<string> errors = new List<string>();

            foreach (var elem in xml.Elements())
            {
                Dictionary<string, ConfigElement<TConfig>> attrMap;
                if (!map.TryGetValue(elem.Name.LocalName, out attrMap))
                {
                    errors.Add(UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                    continue;
                }

                foreach (var attr in elem.Attributes())
                {
                    ConfigElement<TConfig> element;
                    if (!attrMap.TryGetValue(attr.Name.LocalName, out element))
                    {
                        errors.Add(UnknownAttributeError(attr.Name.LocalName, xml.Name.LocalName));
                        continue;
                    }

                    try
                    {
                        var parser = GetParser(element.Type);
                        element.Value = parser(attr.Value);
                    }
                    catch (Exception e)
                    {
                        errors.Add(AttributeParseError(attr.Name.LocalName, xml.Name.LocalName, e.Message));
                    }
                }
            }

            foreach (var element in elements)
            {
                element.Value = element.Value ?? element.DefaultValue;
                element.Apply(config, element.Value);
            }

            return errors.ToArray();
        }

        protected static void Print<TConfig>(IEnumerable<ConfigElement<TConfig>> elements, string offset = "")
        {
            foreach (var e in elements.OrderBy(e => e.Name))
            {
                Console.WriteLine("{0}{1} = {2} {3}", offset, e.Name.PadRight(25), e.Value, object.Equals(e.Value, e.DefaultValue) ? "[Default]" : "");
            }
        }
    }

    class ParamsConfig : ConfigBase
    {
        static readonly ComplexParameters DefaultParams = new ComplexParameters();

        public ComplexParameters Parameters { get; set; }

        public string QueryFilterString { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public LambdaQuery QueryFilter { get; set; }
        
        [Newtonsoft.Json.JsonIgnore]
        public readonly ConfigElement<ParamsConfig>[] Elements = new ConfigElement<ParamsConfig>[]
        {
            new ConfigElement<ParamsConfig> { Name = "MinDepth", Category = "Cavity", Description = "Minimum cavity depth in the number of tetrahedrons.", DefaultValue = DefaultParams.MinDepth, Type = typeof(int), Apply = (c, v) => c.Parameters.MinDepth = (int)v },
            new ConfigElement<ParamsConfig> { Name = "MinDepthLength", Category = "Cavity", Description = "Minimum depth of a cavity in angstroms.", DefaultValue = DefaultParams.MinDepthLength, Type = typeof(double), Apply = (c, v) => c.Parameters.MinDepthLength = (double)v },
            new ConfigElement<ParamsConfig> { Name = "ProbeRadius", Category = "Cavity", Description = "Regulates level of detail of the molecular surface. Higher Probe Radius produces less detail.", DefaultValue = DefaultParams.ProbeRadius, Type = typeof(double), Apply = (c, v) => c.Parameters.ProbeRadius = (double)v },
            new ConfigElement<ParamsConfig> { Name = "InteriorThreshold", Category = "Cavity", Description = "Minimum radius of void inside the protein structure, so that the void would be considered a cavity.", DefaultValue = DefaultParams.InteriorThreshold, Type = typeof(double), Apply = (c, v) => c.Parameters.InteriorThreshold = (double)v },            
            new ConfigElement<ParamsConfig> { Name = "IgnoreHETAtoms", Category = "Cavity", Description = "Allows to exclude the HET atoms from the calculation.", DefaultValue = DefaultParams.IgnoreHETAtoms, Type = typeof(bool), Apply = (c, v) => c.Parameters.IgnoreHETAtoms = (bool)v },
            new ConfigElement<ParamsConfig> { Name = "IgnoreHydrogens", Category = "Cavity", Description = "Allows to exclude the hydrogen atoms from the calculation.", DefaultValue = DefaultParams.IgnoreHydrogens, Type = typeof(bool), Apply = (c, v) => c.Parameters.IgnoreHydrogens = (bool)v },
            
            new ConfigElement<ParamsConfig> { Name = "MinTunnelLength", Category = "Tunnel", Description = "Determines the minimal length (in ang) of a tunnel.", DefaultValue = DefaultParams.MinTunnelLength, Type = typeof(double), Apply = (c, v) => c.Parameters.MinTunnelLength = (double)v },
            new ConfigElement<ParamsConfig> { Name = "MinPoreLength", Category = "Tunnel", Description = "Determines the minimal length (in ang) of a pore.", DefaultValue = DefaultParams.MinPoreLength, Type = typeof(double), Apply = (c, v) => c.Parameters.MinPoreLength = (double)v },
            new ConfigElement<ParamsConfig> { Name = "SurfaceCoverRadius", Category = "Tunnel", Description = "Regulates the density of exit points tested at each outer boundary. Higher Surface Cover radius produces a lower density of exit points.", DefaultValue = DefaultParams.SurfaceCoverRadius, Type = typeof(double), Apply = (c, v) => c.Parameters.SurfaceCoverRadius = (double)v },
            new ConfigElement<ParamsConfig> { Name = "BottleneckRadius", Category = "Tunnel", Description = "Minimal radius of a valid tunnel if BottleneckLength is 0.", DefaultValue = DefaultParams.BottleneckRadius, Type = typeof(double), Apply = (c, v) => c.Parameters.BottleneckRadius = (double)v },
            new ConfigElement<ParamsConfig> { Name = "BottleneckTolerance", Category = "Tunnel", Description = "Maximum length of a valid tunnel for which the radius is less than Bottleneck Radius.", DefaultValue = DefaultParams.BottleneckTolerance, Type = typeof(double), Apply = (c, v) => c.Parameters.BottleneckTolerance = (double)v },
            new ConfigElement<ParamsConfig> { Name = "MaxTunnelSimilarity", Category = "Tunnel", Description = "Maximum degree of similarity between two tunnels before one tunnel is discarded.", DefaultValue = DefaultParams.MaxTunnelSimilarity, Type = typeof(double), Apply = (c, v) => c.Parameters.MaxTunnelSimilarity = (double)v },
            new ConfigElement<ParamsConfig> { Name = "OriginRadius", Category = "Tunnel", Description = "If the user defined a tunnel start point, expand the search for tunnel start points to a sphere of radius.", DefaultValue = DefaultParams.OriginRadius, Type = typeof(double), Apply = (c, v) => c.Parameters.OriginRadius = (double)v },
            new ConfigElement<ParamsConfig> { Name = "AutoOriginCoverRadius", Category = "Tunnel", Description = "The minimal distance between two auto origins in a given cavity.", DefaultValue = DefaultParams.AutoOriginCoverRadius, Type = typeof(double), Apply = (c, v) => c.Parameters.AutoOriginCoverRadius = (double)v },
            new ConfigElement<ParamsConfig> { Name = "MaxAutoOriginsPerCavity", Category = "Tunnel", Description = "The maximum number of automatically computed origins per cavity.", DefaultValue = DefaultParams.MaxAutoOriginsPerCavity, Type = typeof(int), Apply = (c, v) => c.Parameters.MaxAutoOriginsPerCavity = (int)v },
            new ConfigElement<ParamsConfig> { Name = "UseCustomExitsOnly", Category = "Tunnel", Description = "Only user defined exits are used for tunnel computation.", DefaultValue = DefaultParams.UseCustomExitsOnly, Type = typeof(bool), Apply = (c, v) => c.Parameters.UseCustomExitsOnly = (bool)v },
            new ConfigElement<ParamsConfig> { Name = "FilterBoundaryLayers", Category = "Tunnel", Description = "Determines whether to remove layers with boundary residues from the tunnel.", DefaultValue = DefaultParams.FilterTunnelBoundaryLayers, Type = typeof(bool), Apply = (c, v) => c.Parameters.FilterTunnelBoundaryLayers = (bool)v },
            new ConfigElement<ParamsConfig> { Name = "WeightFunction", Category = "Tunnel", Description = "Determines the weight function used to compute channels [" + string.Join(", ", Enum.GetNames(typeof(TunnelWeightFunction)).ToArray()) + "].", DefaultValue = DefaultParams.WeightFunction, Type = typeof(TunnelWeightFunction), Apply = (c, v) => c.Parameters.WeightFunction = (TunnelWeightFunction)v }
        }.OrderBy(c => c.Name).ToArray();

        public void Print()
        {
            Console.WriteLine("<Params>");
            foreach (var g in Elements.GroupBy(e => e.Category).OrderBy(g => g.Key))
            {
                Console.WriteLine("  <{0}>", g.Key);
                Print(g, "    ");
            }
            if (!string.IsNullOrEmpty(QueryFilterString))
            {
                Console.WriteLine("  <Filter>");
                Console.WriteLine("    {0}", QueryFilterString);
            }
            Console.WriteLine();
        }

        public static ParamsConfig FromXml(XElement xml, List<string> errors)
        {
            var config = new ParamsConfig();
            var errs = ParseConfig(new XElement(xml.Name, xml.Elements().Where(e => e.Name.LocalName != "Filter").ToArray()) , config, config.Elements).ToList();

            var filter = xml.Element("Filter");
            if (filter != null)
            {
                try
                {
                    var q = PatternQueryHelper.Compile(string.Format("Function({0})", filter.Value)) as LambdaQuery;
                    config.QueryFilterString = filter.Value;
                    config.QueryFilter = q;
                }
                catch (Exception e)
                {
                    errs.Add(string.Format("Error parsing <Filter> query: {0}", e.Message));
                }
            }

            errors.AddRange(errs);
            if (errs.Count > 0) return null;
            return config;
        }

        public ParamsConfig()
        {
            Parameters = new ComplexParameters();
        }
    }

    class ExportConfig : ConfigBase
    {
        public ExportParameters Parameters { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public readonly ConfigElement<ExportConfig>[] Elements = new ConfigElement<ExportConfig>[]
        {
            new ConfigElement<ExportConfig> { Name = "Mesh", Category = "Formats", Description = "Controls storing information about the mesh of detected tunnels, for subsequent visualization in PyMol or Jmol.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatMesh = (bool)v },            
            new ConfigElement<ExportConfig> { Name = "PyMol", Category = "Formats", Description = "Controls whether a PyMol script will be generated, for subsequent visualization in PyMol.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatPyMol = (bool)v },
            new ConfigElement<ExportConfig> { Name = "Chimera", Category = "Formats", Description = "Controls whether a Chimera script will be generated, for subsequent visualization in Chimera", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatChimera = (bool)v },
            new ConfigElement<ExportConfig> { Name = "VMD", Category = "Formats", Description = "Controls whether a VMD script will be generated, for subsequent visualization in VMD.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatVMD = (bool)v },
            new ConfigElement<ExportConfig> { Name = "PDBProfile", Category = "Formats", Description = "Controls if channel profiles are exported in PDB format.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatPdbProfile = (bool)v },
            new ConfigElement<ExportConfig> { Name = "PDBStructure", Category = "Formats", Description = "Controls if channel residues (surrounding atoms) are exported in PDB format.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatPdbStructure = (bool)v },
            new ConfigElement<ExportConfig> { Name = "CSV", Category = "Formats", Description = "Controls if CSV export of channel profiles created.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatCSV = (bool)v },
            new ConfigElement<ExportConfig> { Name = "JSON", Category = "Formats", Description = "Controls if JSON output is created.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatJSON = (bool)v },
            new ConfigElement<ExportConfig> { Name = "ChargeSurface", Category = "Formats", Description = "Controls if XML representation of surface with charges is created.", DefaultValue = true, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatChargeSurface = (bool)v },
            
            new ConfigElement<ExportConfig> { Name = "Cavities", Category = "Types", Description = "Controls whether cavities will be exported.", DefaultValue = true, Type = typeof(bool), Apply = (c, v) => c.Parameters.ExportCavities = (bool)v },
            new ConfigElement<ExportConfig> { Name = "Tunnels", Category = "Types", Description = "Controls whether tunnels will be computed.", DefaultValue = true, Type = typeof(bool), Apply = (c, v) => c.Parameters.ExportTunnels = (bool)v },            
            new ConfigElement<ExportConfig> { Name = "PoresMerged", Category = "Types", Description = "Controls whether merged pores will be computed.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.ExportMergedPores = (bool)v },
            new ConfigElement<ExportConfig> { Name = "PoresUser", Category = "Types", Description = "Controls whether user exit pores will be computed.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.ExportUserPores = (bool)v },
            new ConfigElement<ExportConfig> { Name = "PoresAuto", Category = "Types", Description = "Controls whether 'auto' pores will be computed.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.ExportAutoPores = (bool)v },
            
            new ConfigElement<ExportConfig> { Name = "Density", Category = "Mesh", Description = "Level of detail of mesh surface. The higher the Mesh Density, the lower the level of detail in the visualization.", DefaultValue = 1.33, Type = typeof(double), Apply = (c, v) => c.Parameters.MeshDensity = (double)v },
            new ConfigElement<ExportConfig> { Name = "Compress", Category = "Mesh", Description = "Controls storing of mesh information in a GZip file archive.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.Parameters.FormatMeshGz = (bool)v },

            new ConfigElement<ExportConfig> { Name = "PDBId", Category = "VMD", Description = "If this value is present, the option for downloading the structure is incorporated in the VMD visualization script.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Parameters.VMDPDBId = (string)v },
            new ConfigElement<ExportConfig> { Name = "SurfaceType", Category = "VMD", Description = "Controls, if channels are displayed in VMD as isosurface or as a set of spheres [" + string.Join(", ", Enum.GetNames(typeof(SurfaceType)).ToArray()) + "].", DefaultValue = SurfaceType.Spheres, Type = typeof(SurfaceType), Apply = (c, v) => c.Parameters.VMDSurfaceType = (SurfaceType)v },

            new ConfigElement<ExportConfig> { Name = "PDBId", Category = "Chimera", Description = "If this value is present, the option for downloading the structure is incorporated in the Chimera visualization script.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Parameters.ChimeraPDBId = (string)v },
            new ConfigElement<ExportConfig> { Name = "SurfaceType", Category = "Chimera", Description = "Controls, if channels are displayed in Chimera as isosurface or as a set of spheres [" + string.Join(", ", Enum.GetNames(typeof(SurfaceType)).ToArray()) + "].", DefaultValue = SurfaceType.Spheres, Type = typeof(SurfaceType), Apply = (c, v) => c.Parameters.ChimeraSurfaceType = (SurfaceType)v },

            new ConfigElement<ExportConfig> { Name = "SurfaceType", Category = "PyMol", Description = "Controls, if channels are displayed in PyMOL as isosurface or as a set of spheres [" + string.Join(", ", Enum.GetNames(typeof(SurfaceType)).ToArray()) + "].", DefaultValue = SurfaceType.Surface, Type = typeof(SurfaceType), Apply = (c, v) => c.Parameters.PyMolSurfaceType = (SurfaceType)v },
            new ConfigElement<ExportConfig> { Name = "PDBId", Category = "PyMol", Description = "If this value is present, the option for downloading the structure is incorporated in the PyMOL visualization script.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Parameters.PyMolPDBId = (string)v },
            new ConfigElement<ExportConfig> { Name = "ChargePalette", Category = "PyMol", Description = "Determines the palette used for charge coloring [" + string.Join(", ", Enum.GetNames(typeof(PyMolSpectrumPalette)).ToArray()) + "]." , DefaultValue = PyMolSpectrumPalette.RedWhiteBlue, Type = typeof(PyMolSpectrumPalette), Apply = (c, v) => c.Parameters.PyMolChargePalette = (PyMolSpectrumPalette)v }
        }.OrderBy(c => c.Name).ToArray();

        public void Print()
        {
            Console.WriteLine("<Export>");
            foreach (var g in Elements.GroupBy(e => e.Category).OrderBy(g => g.Key))
            {
                Console.WriteLine("  <{0}>", g.Key);
                Print(g, "    ");
            }
            Console.WriteLine();
        }

        public static ExportConfig FromXml(XElement xml, List<string> errors)
        {
            var config = new ExportConfig();
            var errs = ParseConfig(xml, config, config.Elements);

            errors.AddRange(errs);
            if (errs.Count > 0) return null;
            return config;
        }

        public ExportConfig()
        {
            Parameters = new ExportParameters();
        }
    }
    
    class InputConfig
    {
        [Newtonsoft.Json.JsonIgnore]
        public string Filename { get; set; }
        public string SpecificChains { get; set; }
        public bool ReadAllModels { get; set; }

        public static InputConfig FromXml(XElement xml, List<string> errors)
        {
            var config = new InputConfig();

            config.Filename = xml.Value;

            foreach (var attr in xml.Attributes())
            {
                switch (attr.Name.LocalName)
                {
                    case "SpecificChains": config.SpecificChains = attr.Value; break;
                    case "ReadAllModels":
                        try
                        {
                            config.ReadAllModels = (bool)ConfigBase.GetBool(attr.Value);
                        }
                        catch (Exception e)
                        {
                            errors.Add(ConfigBase.AttributeParseError("ReadAllModels", xml.Name.LocalName, e.Message));
                        }
                        break;
                    default:
                        errors.Add(ConfigBase.UnknownAttributeError(attr.Name.LocalName, xml.Name.LocalName));
                        break;
                }
            }

            return config;
        }

        public void Print()
        {
            Console.WriteLine("<Input>");
            Console.WriteLine("  Filename = {0}", Filename);
            Console.WriteLine("  Chains = {0}", string.IsNullOrEmpty(SpecificChains) ? "all chains" : "'" + SpecificChains + "'");
            Console.WriteLine("  ReadAllModels = {0}", ReadAllModels);
            Console.WriteLine();
        }
    }

    enum ChargeSourceElementType
    {
        RandomGrid,
        ScalarGrid,
        AtomValues
    }

    class ChargeSourceElement : ConfigBase
    {
        public static readonly Func<ConfigElement<ChargeSourceElement>[]> MakeRandomElements = () => new ConfigElement<ChargeSourceElement>[]
        {
            new ConfigElement<ChargeSourceElement> { Name = "Name", Category = "RandomGrid", Description = "Name of the field.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Name = (string)v },
            
            new ConfigElement<ChargeSourceElement> { Name = "MinValue", Category = "RandomGrid", Description = "Minimum value to generate.", DefaultValue = -1.0, Type = typeof(double), Apply = (c, v) => c.MinValue = (double)v },
            new ConfigElement<ChargeSourceElement> { Name = "MaxValue", Category = "RandomGrid", Description = "Maximum value to generate.", DefaultValue = 1.0, Type = typeof(double), Apply = (c, v) => c.MaxValue = (double)v },
        };

        public static readonly Func<ConfigElement<ChargeSourceElement>[]> MakeGridElements = () => new ConfigElement<ChargeSourceElement>[]
        {
            new ConfigElement<ChargeSourceElement> { Name = "Name", Category = "ScalarGrid", Description = "Name of the field.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Name = (string)v },
            new ConfigElement<ChargeSourceElement> { Name = "Source", Category = "ScalarGrid", Description = "Filename of the source. Supported formats: OpenDX.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Source = (string)v },
        };

        public static readonly Func<ConfigElement<ChargeSourceElement>[]> MakeAtomValuesElements = () => new ConfigElement<ChargeSourceElement>[]
        {
            new ConfigElement<ChargeSourceElement> { Name = "Name", Category = "AtomValues", Description = "Name of the field.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Name = (string)v },
            new ConfigElement<ChargeSourceElement> { Name = "Source", Category = "AtomValues", Description = "Filename of the source. Supported formats: OpenDX.", DefaultValue = "", Type = typeof(string), Apply = (c, v) => c.Source = (string)v },

            new ConfigElement<ChargeSourceElement> { Name = "Method", Category = "AtomValues", Description = "Method used for charge computation [" + string.Join(", ", Enum.GetNames(typeof(AtomValueFieldInterpolationMethod)).ToArray()) + "].", DefaultValue = AtomValueFieldInterpolationMethod.NearestValue, Type = typeof(AtomValueFieldInterpolationMethod), Apply = (c, v) => c.ValuesMethod = (AtomValueFieldInterpolationMethod)v },
            new ConfigElement<ChargeSourceElement> { Name = "Radius", Category = "AtomValues", Description = "Radius used for the Radius* methods.", DefaultValue = 5.0, Type = typeof(double), Apply = (c, v) => c.Radius = (double)v },
            new ConfigElement<ChargeSourceElement> { Name = "K", Category = "AtomValues", Description = "Number of neighbors used used for the KNearest* methods.", DefaultValue = 5, Type = typeof(int), Apply = (c, v) => c.K = (int)v },
            new ConfigElement<ChargeSourceElement> { Name = "IgnoreHydrogens", Category = "AtomValues", Description = "Determines if to consider hydrogens for value assignment.", DefaultValue = false, Type = typeof(bool), Apply = (c, v) => c.IgnoreHydrogens = (bool)v },
        };

        [Newtonsoft.Json.JsonIgnore]
        ConfigElement<ChargeSourceElement>[] Values;

        public ChargeSourceElementType Type { get; set; }

        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        public string Name { get; set; }
        public string Source { get; set; }

        public bool IgnoreHydrogens { get; set; }
        public double Radius { get; set; }
        public int K { get; set; }
        public AtomValueFieldInterpolationMethod ValuesMethod { get; set; }

        public static ChargeSourceElement FromXml(XElement xml, List<string> errors)
        {
            var ret = new ChargeSourceElement();
            switch (xml.Name.LocalName)
            {
                case "ScalarGrid":
                    ret.Type = ChargeSourceElementType.ScalarGrid;
                    ret.Values = MakeGridElements();
                    break;
                case "AtomValues":
                    ret.Type = ChargeSourceElementType.AtomValues;
                    ret.Values = MakeAtomValuesElements();
                    break;
                case "RandomGrid":
                    ret.Type = ChargeSourceElementType.RandomGrid;
                    ret.Values = MakeRandomElements();
                    break;
            }

            var err = ConfigBase.ParseConfig<ChargeSourceElement>(new XElement("ChargeSources", xml), ret, ret.Values);
            errors.AddRange(err);
            return err.Count == 0 ? ret : null;
        }

        public void Print()
        {
            Console.WriteLine("  <{0}>", Type.ToString());
            Print(Values, "    ");
        }
    }

    class ChargeSourcesConfig
    {
        public ChargeSourceElement[] Sources { get; set; }

        public static ChargeSourcesConfig FromXml(XElement xml, List<string> errors)
        {
            var fields = new List<ChargeSourceElement>();

            foreach (var elem in xml.Elements())
            {
                switch (elem.Name.LocalName)
                {
                    case "ScalarGrid":
                    case "AtomValues":
                    case "RandomGrid":
                        try
                        {
                            fields.Add(ChargeSourceElement.FromXml(elem, errors));
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error parsing <{0}>: {1}", elem.Name.LocalName, e.Message));
                        }
                        break;
                    default:
                        errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                        break;
                }
            }

            return new ChargeSourcesConfig { Sources = fields.ToArray() };
        }

        public void Print()
        {
            if (Sources.Length > 0)
            Console.WriteLine("<ChargeSources>");
            foreach (var s in Sources) s.Print();
            Console.WriteLine();
        }
    }

    class CustomVdwConfig
    {
        public Dictionary<ElementSymbol, double> Values { get; set; }

        public static CustomVdwConfig FromXml(XElement xml, List<string> errors)
        {
            Dictionary<ElementSymbol, double> values = new Dictionary<ElementSymbol,double>();

            bool hasError = false;

            foreach (var elem in xml.Elements())
            {
                switch (elem.Name.LocalName)
                {
                    case "Radius":
                        try
                        {
                            var eA = elem.Attribute("Element");
                            var rA = elem.Attribute("Value");

                            if (eA == null) throw new ArgumentException("Missing Element attribute.");
                            if (rA == null) throw new ArgumentException("Missing Value attribute.");

                            var unknownAttributes = xml.Attributes().Where(a => a.Name.LocalName != "Element" && a.Name.LocalName != "Radius").ToArray();
                            if (unknownAttributes.Length > 0)
                            {
                                throw new ArgumentException(string.Format("Unknown attributes: {0}.", string.Join(", ", unknownAttributes.Select(a => a.Name.LocalName).ToArray())));
                            }

                            var r = (double)ConfigBase.GetDouble(rA.Value);
                            values.Add(ElementSymbol.Create(eA.Value), r);
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    default:
                        errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                        hasError = true;
                        break;
                }
            }

            if (hasError)
            {
                throw new InvalidOperationException(string.Format("Errors in non active residue specification <{0}>.", xml.Name.LocalName));
            }

            return new CustomVdwConfig
            {
                Values = values
            };
        }

        public void Print()
        {
            if (Values.Count == 0) return;

            Console.WriteLine("<CustomVdw>");
            foreach (var radius in Values.OrderBy(e => e.Key))
            {
                Console.WriteLine("  {0} = {1}", radius.Key, radius.Value);
            }
            Console.WriteLine();
        }

        public CustomVdwConfig()
        {
            Values = new Dictionary<ElementSymbol, double>();
        }
    }
    
    class NonActivePartsConfig
    {
        public ResidueHandle[] Residues { get; set; }
        public QueryHandle[] Queries { get; set; }

        public static NonActivePartsConfig FromXml(XElement xml, List<string> errors)
        {
            List<ResidueHandle> residues = new List<ResidueHandle>();
            List<QueryHandle> queries = new List<QueryHandle>();

            bool hasError = false;

            foreach (var elem in xml.Elements())
            {
                switch (elem.Name.LocalName)
                {
                    case "Residue":
                        try
                        {
                            residues.Add(ResidueHandle.FromXml(elem));
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    case "Query":
                        try
                        {
                            queries.Add(QueryHandle.FromXml(elem));
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    default:
                        errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                        hasError = true;
                        break;
                }
            }

            if (hasError)
            {
                throw new InvalidOperationException(string.Format("Errors in non active residue specification <{0}>.", xml.Name.LocalName));
            }

            return new NonActivePartsConfig
            {
                Residues = residues.ToArray(),
                Queries = queries.ToArray()
            };
        }

        public void Print()
        {
            if (Residues.Length == 0 && Queries.Length == 0) return;

            Console.WriteLine("<NonActiveParts>");
            if (Residues.Length > 0) Console.WriteLine("  {0} residue(s): {1}.", Residues.Length, string.Join(", ", Residues.Select(r => r.ToString()).ToArray()));
            if (Queries.Length > 0) 
            {
                Console.WriteLine("  {0} {1}:", Queries.Length, Queries.Length == 1 ? "query" : "queries");
                foreach (var q in Queries) Console.WriteLine("    {0}", q.Text);
            }
            Console.WriteLine();
        }
    }

    class TriangulationPoint
    {
        public ResidueHandle[] Residues { get; set; }
        public Point3DHandle[] ResidueSnaps { get; set; }
        public Point3DHandle[] Points { get; set; }
        public QueryHandle[] Queries { get; set; }
        
        public Vector3D? ToPoint(IStructure structure)
        {
            var rs = structure.PdbResidues();

            List<Vector3D> atoms = new List<Vector3D>();
            var added = new HashSet<PdbResidue>();

            foreach (var rh in Residues)
            {
                var r = rs.FromIdentifier(PdbResidueIdentifier.Create(rh.SequenceNumber, rh.Chain, rh.InsertionCode));
                if (r == null)
                {
                    Console.WriteLine("Warning: Residue '{0}' not found.");
                    continue;
                }
                if (added.Add(r))
                {
                    atoms.AddRange(r.Atoms.Select(a => a.Position));
                }
            }

            foreach (var p in ResidueSnaps)
            {
                IAtom closest = null;
                double closestDistance = double.MaxValue;
                foreach (var a in structure.Atoms)
                {
                    var d = a.Position.DistanceTo(p.Point);
                    if (d < closestDistance)
                    {
                        closestDistance = d;
                        closest = a;
                    }
                }
                var r = rs.FromAtom(closest);
                if (added.Add(r))
                {
                    atoms.AddRange(r.Atoms.Select(a => a.Position));
                }
            }

            atoms.AddRange(Points.Select(p => p.Point));

            if (atoms.Count > 0) return atoms.GetCenter();
            return null;
        }

        public Vector3D[] ToPoints(IStructure structure, Func<ExecutionContext> ctx)
        {
            var rs = structure.PdbResidues();

            List<Vector3D> atoms = new List<Vector3D>();
            var added = new HashSet<PdbResidue>();

            foreach (var rh in Residues)
            {
                var r = rs.FromIdentifier(PdbResidueIdentifier.Create(rh.SequenceNumber, rh.Chain, rh.InsertionCode));
                if (r == null)
                {
                    Console.WriteLine("Warning: Residue '{0}' not found.");
                    continue;
                }
                if (added.Add(r))
                {
                    atoms.AddRange(r.Atoms.Select(a => a.Position));
                }
            }

            foreach (var p in ResidueSnaps)
            {
                IAtom closest = null;
                double closestDistance = double.MaxValue;
                foreach (var a in structure.Atoms)
                {
                    var d = a.Position.DistanceTo(p.Point);
                    if (d < closestDistance)
                    {
                        closestDistance = d;
                        closest = a;
                    }
                }
                var r = rs.FromAtom(closest);
                if (added.Add(r))
                {
                    atoms.AddRange(r.Atoms.Select(a => a.Position));
                }
            }

            atoms.AddRange(Points.Select(p => p.Point));

            if (atoms.Count == 0 && Queries.Length == 0) return new Vector3D[0];
            if (Queries.Length == 0) return new[] { atoms.GetCenter() };

            var motives = Queries.SelectMany(q => q.Query.Matches(ctx())).ToHashSet();

            return motives.Select(m => atoms.Concat(m.Atoms.Select(a => a.Position)).GetCenter()).ToArray();
        }

        public static TriangulationPoint FromXml(XElement xml, List<string> errors)
        {
            List<ResidueHandle> residues = new List<ResidueHandle>();
            List<Point3DHandle> snaps = new List<Point3DHandle>();
            List<Point3DHandle> points = new List<Point3DHandle>();
            List<QueryHandle> queries = new List<QueryHandle>();
            bool hasError = false;
            foreach (var elem in xml.Elements())
            {
                switch (elem.Name.LocalName)
                {
                    case "Residue":
                        try 
                        {
                            residues.Add(ResidueHandle.FromXml(elem));
                        }
                        catch (Exception e) 
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    case "ResidueFromPoint":
                        try
                        {
                            snaps.Add(Point3DHandle.FromXml(elem));
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    case "Point":
                        try
                        {
                            points.Add(Point3DHandle.FromXml(elem));
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    case "Query":
                        try
                        {
                            queries.Add(QueryHandle.FromXml(elem));
                        }
                        catch (Exception e)
                        {
                            errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            hasError = true;
                        }
                        break;
                    default:
                        errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                        hasError = true;
                        break;
                }
            }

            if (hasError)
            {
                throw new InvalidOperationException(string.Format("Errors in point specification <{0}>.", xml.Name.LocalName));
            }

            return new TriangulationPoint
            {
                Residues = residues.ToArray(),
                ResidueSnaps = snaps.ToArray(),
                Points = points.ToArray(),
                Queries = queries.ToArray()
            };
        }

        public void Print(string header)
        {
            List<string> elems = new List<string>();
            if (Residues.Length > 0) elems.Add(string.Join(", ", Residues.Select(r => r.ToString()).ToArray()));
            if (ResidueSnaps.Length > 0) elems.Add("Residues at " + string.Join(", ", ResidueSnaps.Select(r => r.ToString()).ToArray()));
            if (Points.Length > 0) elems.Add(string.Join(", ", Points.Select(r => r.ToString()).ToArray()));
            if (Queries.Length > 0) elems.Add(string.Join(", ", Queries.Select(q => q.ToString()).ToArray()));

            if (elems.Count == 0) return;

            Console.WriteLine("{0}: {1}", header, string.Join("; ", elems));
        }

        public override string ToString()
        {
            List<string> elems = new List<string>();
            if (Residues.Length > 0) elems.Add(string.Join(", ", Residues.Select(r => r.ToString()).ToArray()));
            if (ResidueSnaps.Length > 0) elems.Add("Residues at " + string.Join(", ", ResidueSnaps.Select(r => r.ToString()).ToArray()));
            if (Points.Length > 0) elems.Add(string.Join(", ", Points.Select(r => r.ToString()).ToArray()));
            if (Queries.Length > 0) elems.Add(string.Join(", ", Queries.Select(q => q.ToString()).ToArray()));
            return string.Join("; ", elems);
        }

        private TriangulationPoint()
        {
        }
    }

    class TriangulationPoints
    {
        public TriangulationPoint[] Points { get; set; }

        public static TriangulationPoints FromXml(XElement xml, string elementName, List<string> errors)
        {
            List<TriangulationPoint> points = new List<TriangulationPoint>();

            bool hasError = false;

            foreach (var elem in xml.Elements())
            {
                if (elem.Name.LocalName == elementName)
                {
                    try
                    {
                        points.Add(TriangulationPoint.FromXml(elem, errors));
                    }
                    catch (Exception e)
                    {
                        errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                        hasError = true;
                    }
                }
                else
                {
                    errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                }
            }

            if (hasError)
            {
                throw new InvalidOperationException(string.Format("Errors in point specification <{0}>.", xml.Name.LocalName));
            }

            return new TriangulationPoints { Points = points.ToArray() };
        }

        public void Print(string pointHeader)
        {
            if (Points.Length == 0) return;
            foreach (var p in Points)
            {
                p.Print(pointHeader);
            }
        }
    }

    class CustomExitsConfig
    {
        public TriangulationPoints Points { get; set; }

        public static CustomExitsConfig FromXml(XElement xml, List<string> errors)
        {
            try
            {
                var points = TriangulationPoints.FromXml(xml, "Exit", errors);
                var unknownAttributes = xml.Attributes().ToArray();
                if (unknownAttributes.Length > 0)
                {
                    throw new ArgumentException(string.Format("Unknown attributes: {0}.", string.Join(", ", unknownAttributes.Select(a => a.Name.LocalName).ToArray())));
                }
                return new CustomExitsConfig { Points = points };
            }
            catch (Exception e)
            {
                errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                throw new InvalidOperationException(string.Format("Errors in custom exit specification <{0}>.", xml.Name.LocalName));
            }
        }

        public void Print()
        {
            if (Points.Points.Length == 0) return;
            Console.WriteLine("<CustomExits>");
            Points.Print("  Exit");
            Console.WriteLine();
        }
    }

    class OriginsConfig
    {
        public bool UseAutoPoints { get; set; }
        public TriangulationPoints Points { get; set; }

        public static OriginsConfig FromXml(XElement xml, List<string> errors)
        {
            try
            {
                var points = TriangulationPoints.FromXml(xml, "Origin", errors);
                var auto = xml.Attribute("Auto");
                bool useAuto = false;
                if (auto != null)
                {
                    useAuto = (bool)ConfigBase.GetBool(auto.Value);
                }
                var unknownAttributes = xml.Attributes().Where(a => a.Name.LocalName != "Auto").ToArray();
                if (unknownAttributes.Length > 0)
                {
                    throw new ArgumentException(string.Format("Unknown attributes: {0}.", string.Join(", ", unknownAttributes.Select(a => a.Name.LocalName).ToArray())));
                }
                return new OriginsConfig { Points = points, UseAutoPoints = useAuto };
            }
            catch (Exception e)
            {
                errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                throw new InvalidOperationException(string.Format("Errors in origins specification <{0}>.", xml.Name.LocalName));
            }
        }

        public void Print()
        {
            Console.WriteLine("<Origins>");
            Console.WriteLine("  Use computed origins = {0}", UseAutoPoints);
            Points.Print("  Origin");
            Console.WriteLine();
        }
    }
    
    class PathConfig
    {
        public TriangulationPoint Start { get; set; }
        public TriangulationPoint End { get; set; }

        public static PathConfig FromXml(XElement xml, List<string> errors)
        {
            TriangulationPoint start = null, end = null;
            bool hasError = false;

            foreach (var elem in xml.Elements())
            {
                if (elem.Name.LocalName == "Start")
                {
                    try
                    {
                        start = TriangulationPoint.FromXml(elem, errors);
                    }
                    catch (Exception e)
                    {
                        errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                        hasError = true;
                    }
                }
                else if (elem.Name.LocalName == "End")
                {
                    try
                    {
                        end = TriangulationPoint.FromXml(elem, errors);
                    }
                    catch (Exception e)
                    {
                        errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                        hasError = true;
                    }
                }
                else
                {
                    errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                }
            }

            if (start == null)
            {
                hasError = true;
                errors.Add(string.Format("Missing <Start> point in <Path> specification."));
            }

            if (end == null)
            {
                hasError = true;
                errors.Add(string.Format("Missing <End> point in <Path> specification."));
            }

            if (hasError)
            {
                throw new InvalidOperationException(string.Format("Errors in path specification <{0}>.", xml.Name.LocalName));
            }

            return new PathConfig { Start = start, End = end };
        }

        public void Print()
        {
            Console.WriteLine("  <Path>");
            Start.Print(      "    Start: ");
            End.Print(        "    End:   ");
        }
    }

    class PathsConfig
    {
        public PathConfig[] Paths { get; set; }

        public static PathsConfig FromXml(XElement xml, List<string> errors)
        {
            List<PathConfig> paths = new List<PathConfig>();

            bool hasError = false;

            foreach (var elem in xml.Elements())
            {
                if (elem.Name.LocalName == "Path")
                {
                    try
                    {
                        paths.Add(PathConfig.FromXml(elem, errors));
                    }
                    catch (Exception e)
                    {
                        errors.Add(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                        hasError = true;
                    }
                }
                else
                {
                    errors.Add(ConfigBase.UnknownElementError(elem.Name.LocalName, xml.Name.LocalName));
                }
            }

            if (hasError)
            {
                throw new InvalidOperationException(string.Format("Errors in paths specification <{0}>.", xml.Name.LocalName));
            }

            return new PathsConfig { Paths = paths.ToArray() };
        }

        public void Print()
        {
            if (Paths.Length == 0) return;
            Console.WriteLine("<Paths>");
            foreach (var p in Paths)
            {
                p.Print();
            }
        }
    }

    class TunnelsConfig
    {
        public InputConfig Input { get; set; }
        public ChargeSourcesConfig Charges { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public string WorkingDirectory { get; set; }
        public ParamsConfig Params { get; set; }
        public ExportConfig Export { get; set; }
        public CustomVdwConfig CustomVdw { get; set; }
        public NonActivePartsConfig NonActiveParts { get; set; }
        public CustomExitsConfig CustomExits { get; set; }
        public OriginsConfig Origins { get; set; }
        public PathsConfig Paths { get; set; }

        public void Print()
        {
            Input.Print();
            Charges.Print();

            Console.WriteLine("<WorkingDirectory>");
            Console.WriteLine("  {0}", WorkingDirectory);
            Console.WriteLine();

            Params.Print();
            NonActiveParts.Print();
            Export.Print();
            CustomVdw.Print();
            CustomExits.Print();
            Origins.Print();
            Paths.Print();
        }

        public static Tuple<TunnelsConfig, string[]> FromXml(XElement xml)
        {
            List<string> errors = new List<string>();
            var ret = new TunnelsConfig();

            try
            {
                var elem = xml.Element("Input");
                if (elem == null)
                {
                    errors.Add("Missing <Input> element.");
                }
                else 
                {
                    ret.Input = InputConfig.FromXml(elem, errors);
                }
            }
            catch (Exception)
            {
                errors.Add("Errors in <Input>.");
            }

            try
            {
                var elem = xml.Element("ChargeSources") ?? new XElement("ChargeSources");
                ret.Charges = ChargeSourcesConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <ChargeSources> element.");
            }

            try
            {
                var elem = xml.Element("WorkingDirectory");
                if (elem == null)
                {
                    errors.Add("Missing <WorkingDirectory> element.");
                }
                else
                {
                    var wd = elem.Value;

                    if (!wd.EndsWith("/") && !wd.EndsWith("\\")) wd = wd + "/";
                    string workingDirectory;
                    if (wd.StartsWith("./"))
                    {
                        wd = wd.Substring(2);
                        workingDirectory = Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), wd));
                    }
                    else
                    {
                        workingDirectory = Path.GetDirectoryName(wd);
                    }

                    ret.WorkingDirectory = workingDirectory;
                }
            }
            catch (Exception)
            {
                errors.Add("Errors in <WorkingDirectory> element.");
            }

            try
            {
                var elem = xml.Element("Params") ?? new XElement("Params");
                ret.Params = ParamsConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <Params> element.");
            }

            try
            {
                var elem = xml.Element("Export") ?? new XElement("Export");
                ret.Export = ExportConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <Export> element.");
            }
            
            try
            {
                var elem = xml.Element("CustomVdw") ?? new XElement("CustomVdw");
                ret.CustomVdw = CustomVdwConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <CustomVdw> element.");
            }

            try
            {
                var elem = xml.Element("NonActiveParts") ?? new XElement("NonActiveParts");
                ret.NonActiveParts = NonActivePartsConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <NonActiveParts> element.");
            }

            try
            {
                var elem = xml.Element("CustomExits") ?? new XElement("CustomExits");
                ret.CustomExits = CustomExitsConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <CustomExits> element.");
            }

            try
            {
                var elem = xml.Element("Origins") ?? new XElement("Origins");
                ret.Origins = OriginsConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <Origins> element.");
            }

            try
            {
                var elem = xml.Element("Paths") ?? new XElement("Paths");
                ret.Paths = PathsConfig.FromXml(elem, errors);
            }
            catch (Exception)
            {
                errors.Add("Errors in <Paths> element.");
            }
            
            return Tuple.Create(ret, errors.ToArray());
        }

        public static string CreateHelp()
        {
            var shape = new XElement("Tunnels",
                new XElement("Input", new XAttribute("Attributes", "..."), "filename"),
                new XElement("ChargeSources",
                    new XElement("ScalarGrid", new XAttribute("Attributes", "...")),
                    new XElement("AtomValues", new XAttribute("Attributes", "...")),
                    new XElement("RandomGrid", new XAttribute("Attributes", "...")),
                    new XComment("More optional Field elements")),
                new XElement("WorkingDirectory", "path"),
                new XElement("Params",
                    new XElement("Cavity", new XAttribute("Attributes", "...")),
                    new XElement("Tunnel", new XAttribute("Attributes", "...")),
                    new XElement("Filter", "PatternQuery expression (see http://webchem.ncbr.muni.cz/Wiki for help)")),
                new XElement("Export",
                    new XElement("Formats", new XAttribute("Attributes", "...")),
                    new XElement("Types", new XAttribute("Attributes", "...")),
                    new XElement("Mesh", new XAttribute("Attributes", "...")),
                    new XElement("PyMol", new XAttribute("Attributes", "...")),
                    new XElement("Chimera", new XAttribute("Attributes", "...")),
                    new XElement("VMD", new XAttribute("Attributes", "..."))),
                new XElement("CustomVdw",
                    new XElement("Radius", new XAttribute("Element", "C"), new XAttribute("Value", "1.75")),
                    new XComment("More optional Radius elements")),
                new XElement("NonActiveParts",
                    new XElement("Residue", new XAttribute("SequenceNumber", "123"), new XAttribute("Chain", "A")),
                    new XElement("Query", "PatternQuery expression (see http://webchem.ncbr.muni.cz/Wiki for help)"),
                    new XComment("More optional Residue or Query elements")),
                new XElement("CustomExits",
                    new XElement("Exit", "Point specification"),
                    new XComment("More optional Exit elements")),
                new XElement("Origins",
                    new XAttribute("Auto", "0 or 1"),
                    new XElement("Origin", "Point specification"),
                    new XComment("More optional Origin elements")),
                new XElement("Paths",
                    new XComment("Optional"),
                    new XElement("Path",
                        new XElement("Start", "Point specification"),
                        new XElement("End", "Point specification")),
                    new XComment("More optional Path elements."))
            );

            var types =
                "Parameter types:\n" +
                "  [string] - a sequence of characters\n" +
                "  [real]   - a floating point number (1.23)\n" +
                "  [int]    - an integer (123)\n" +
                "  [bool]   - 0/1 or True/False";
            
            var pointSpec =
                "Points can be specified in 4 ways: \n" +
                "- One or more residue elements\n" +
                "    " + new XElement("Residue", new XAttribute("SequenceNumber", "123"), new XAttribute("Chain", "A"), new XAttribute("InsertionCode", " ")).ToString() + "\n" +
                "- One or more 3D points that 'snaps' to the closest residue"  + Environment.NewLine +
                "    " + new XElement("ResidueFromPoint", new XAttribute("X", "1.0"), new XAttribute("Y", "2.0"), new XAttribute("Z", "3.0")).ToString() + "\n" +
                "- One or more 3D points\n" +
                "    " + new XElement("Point", new XAttribute("X", "1.0"), new XAttribute("Y", "2.0"), new XAttribute("Z", "3.0")).ToString() + "\n" +
                "- PatternQuery expression (see http://webchem.ncbr.muni.cz/Wiki for help)\n" +
                "    " + new XElement("Query", "expression").ToString() + "\n" +
                "    Creates a point from each motif returned by the query.\n" +
                "The final start point is defined as the centroid of atomic centers\n" +
                "or defined points.";

            var inputAttributes =
                "<Input> attributes: \n" +
                ConfigBase.MakeAttributeHelp(new ConfigElement<object>
                {
                    Name = "SpecificChains",
                    Type = typeof(string),
                    DefaultValue = "",
                    Description = "One or more characters that specify which chains should participate in the computation. If empty or non present, all chains are loaded."
                }) 
                +
                ConfigBase.MakeAttributeHelp(new ConfigElement<object>
                {
                    Name = "ReadAllModels",
                    Type = typeof(bool),
                    DefaultValue = false,
                    Description = "Determines whether to read all models from the PDB file. All models are read automatically for PDB assemblies (.pdb0 extension)."
                });

            var originAttributes =
                "<Origin> attributes: \n" +
                ConfigBase.MakeAttributeHelp(new ConfigElement<object>
                {
                    Name = "Auto",
                    Type = typeof(bool),
                    DefaultValue = false,
                    Description = "Determines whether to (also) use automatically computed tunnel start points."
                });

            var chargeSources = "<ChargeSources> elements: \n" +
                string.Concat(ChargeSourceElement.MakeGridElements().Concat(ChargeSourceElement.MakeAtomValuesElements()).Concat(ChargeSourceElement.MakeRandomElements())
                    .GroupBy(e => e.Category).OrderBy(c => c.Key)
                    .Select(c => string.Format("  <{0}> attributes:\n", c.Key) +
                                 string.Concat(c.Select(ConfigBase.MakeCategoryAttributeHelp).ToArray())));

            var paramsAttributes = "<Params> elements: \n" +
                string.Concat(new ParamsConfig().Elements
                    .GroupBy(e => e.Category).OrderBy(c => c.Key)
                    .Select(c => string.Format("  <{0}> attributes:\n", c.Key) + 
                                 string.Concat(c.Select(ConfigBase.MakeCategoryAttributeHelp).ToArray()))) +
                "  <Filter>\n"  +
                "    A PatternQuery lambda expression that returns True to keep\n" +
                "    the given channel or False to discard it.";

            var exportAttributes = "<Export> elements: \n" +
                string.Concat(new ExportConfig().Elements
                    .GroupBy(e => e.Category).OrderBy(c => c.Key)
                    .Select(c => string.Format("  <{0}> attributes:\n", c.Key) +
                                 string.Concat(c.Select(ConfigBase.MakeCategoryAttributeHelp).ToArray())));

            var help = new[]
                {
                    "General XML input shape:",
                    "",
                    shape.ToString(),
                    "",
                    pointSpec,
                    "",
                    types,
                    "",
                    inputAttributes,
                    "",
                    chargeSources,
                    "",
                    paramsAttributes,
                    "",
                    exportAttributes,
                    "",
                    originAttributes
                };

            return string.Join("\n", help);
        }

        static string[] MakeAttributeWiki<T>(IEnumerable<ConfigElement<T>> xs)
        {
            return xs
                .GroupBy(e => e.Category).OrderBy(c => c.Key)
                .Select(c => string.Format("==== &lt;{0}&gt; attributes ====" + Environment.NewLine, c.Key) +
                             string.Join(Environment.NewLine, c.Select(ConfigBase.MakeAttributeWikiEntry).ToArray()))
                .ToArray();
        }

        public static string CreateWikiEntry()
        {
            var shape = new XElement("Tunnels",
                new XElement("Input", new XAttribute("Attributes", "..."), "filename"),
                new XElement("ChargeSources",
                    new XElement("ScalarGrid", new XAttribute("Attributes", "...")),
                    new XElement("AtomValues", new XAttribute("Attributes", "...")),
                    new XElement("RandomGrid", new XAttribute("Attributes", "...")),
                    new XComment("More optional Field elements")),
                new XElement("WorkingDirectory", "path"),
                new XElement("Params",
                    new XElement("Cavity", new XAttribute("Attributes", "...")),
                    new XElement("Tunnel", new XAttribute("Attributes", "...")),
                    new XElement("Filter", "PatternQuery expression (see http://webchem.ncbr.muni.cz/Wiki for help)")),
                new XElement("Export",
                    new XElement("Formats", new XAttribute("Attributes", "...")),
                    new XElement("Types", new XAttribute("Attributes", "...")),
                    new XElement("Mesh", new XAttribute("Attributes", "...")),
                    new XElement("PyMol", new XAttribute("Attributes", "...")),
                    new XElement("Chimera", new XAttribute("Attributes", "...")),
                    new XElement("VMD", new XAttribute("Attributes", "..."))),
                new XElement("CustomVdw",
                    new XElement("Radius", new XAttribute("Element", "C"), new XAttribute("Value", "1.75")),
                    new XComment("More optional Radius elements")),
                new XElement("NonActiveParts",
                    new XElement("Residue", new XAttribute("SequenceNumber", "123"), new XAttribute("Chain", "A")),
                    new XElement("Query", "PatternQuery expression (see http://webchem.ncbr.muni.cz/Wiki for help)"),
                    new XComment("More optional Residue or Query elements")),
                new XElement("CustomExits",
                    new XElement("Exit", "Point specification"),
                    new XComment("More optional Exit elements")),
                new XElement("Origins",
                    new XAttribute("Auto", "0 or 1"),
                    new XElement("Origin", "Point specification"),
                    new XComment("More optional Origin elements")),
                new XElement("Paths",
                    new XComment("Optional"),
                    new XElement("Path",
                        new XElement("Start", "Point specification"),
                        new XElement("End", "Point specification")),
                    new XComment("More optional Path elements."))
            );

            var types = new[]
                {
                    ": [string] - a sequence of characters",
                    ": [real] - a floating point number (1.23)",
                    ": [int] - an integer (123)",
                    ": [bool] - 0/1 or True/False"
                };

            var pointSpec = new[]
            {
                "Points can be specified in 4 ways:",
                "; One or more residue elements",
                ": <code>" + new XElement("Residue", new XAttribute("SequenceNumber", "123"), new XAttribute("Chain", "A"), new XAttribute("InsertionCode", " ")).ToString() + "</code>",
                "; One or more 3D points that 'snaps' to the closest residue",
                ": <code>" + new XElement("ResidueFromPoint", new XAttribute("X", "1.0"), new XAttribute("Y", "2.0"), new XAttribute("Z", "3.0")).ToString() + "</code>",
                "; One or more 3D points",
                ": <code>" + new XElement("Point", new XAttribute("X", "1.0"), new XAttribute("Y", "2.0"), new XAttribute("Z", "3.0")).ToString() + "</code>",
                "; PatternQuery expression (see http://webchem.ncbr.muni.cz/Wiki for help)",
                ": <code>" + new XElement("Query", "expression").ToString() + "</code>",
                ": Creates a point from each motif returned by the query.",
                "<br/>",
                "The final start point is defined as the centroid of atomic centers or defined points."
            };

            var inputAttributes = new[]
            {
                "=== &lt;Input&gt; attributes ===",
                ConfigBase.MakeAttributeWikiEntry(new ConfigElement<object>
                {
                    Name = "SpecificChains",
                    Type = typeof(string),
                    DefaultValue = "",
                    Description = "Chain identifiers separated by a comma (,) that specify which chains should participate in the computation. If empty or non present, all chains are loaded."
                }),
                ConfigBase.MakeAttributeWikiEntry(new ConfigElement<object>
                {
                    Name = "ReadAllModels",
                    Type = typeof(bool),
                    DefaultValue = false,
                    Description = "Determines whether to read all models from the PDB file. All models are read automatically for PDB assemblies (.pdb0 extension)."
                })
            };

            var originAttributes = new[]
            {
                "=== &lt;Origin&gt; attributes ===",
                ConfigBase.MakeAttributeWikiEntry(new ConfigElement<object>
                {
                    Name = "Auto",
                    Type = typeof(bool),
                    DefaultValue = false,
                    Description = "Determines whether to (also) use automatically computed tunnel start points."
                })
            };
            
            var help = new[]
            {
                new [] 
                { 
                    "== General XML input shape ==",
                    "<pre>",
                    shape.ToString(),
                    "</pre>"
                },
                new [] { "== Point specification ==" },
                pointSpec,
                new [] 
                { 
                    "== Parameter Descriptions ==",
                    "=== Types ===" 
                },
                types,
                new [] { "-----------------------" },
                inputAttributes,
                new [] 
                {
                    "-----------------------",
                    "=== &lt;ChargeSources&gt; elements ==="
                },
                MakeAttributeWiki(ChargeSourceElement.MakeGridElements().Concat(ChargeSourceElement.MakeAtomValuesElements()).Concat(ChargeSourceElement.MakeRandomElements())),
                new [] 
                {
                    "-----------------------",
                    "=== &lt;Params&gt; elements ==="
                },
                MakeAttributeWiki(new ParamsConfig().Elements),
                new [] 
                {
                    "==== &lt;Filter&gt; element ====",
                    "''A PatternQuery lambda expression that returns True to keep the given channel or False to discard it.''"
                },     
                new [] 
                {
                    "-----------------------",
                    "=== &lt;Export&gt; elements ==="
                },
                MakeAttributeWiki(new ExportConfig().Elements),       
                new [] { "-----------------------" },
                originAttributes
            };

            return string.Join(Environment.NewLine, help.SelectMany(h => h));
        }
    }
}
