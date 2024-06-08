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
    using System.Xml.Linq;
    using System.Globalization;
    using System.Windows;
    using WebChemistry.Tunnels.Core;

    static class CustomVDW
    {
        public static bool UsingDefault = true;

        public static void Init()
        {
            if (!File.Exists("vdwradii.xml")) return;

            try
            {
                var xml = XElement.Load("vdwradii.xml");
                var values = new Dictionary<ElementSymbol, double>();
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

                                double val;
                                if (!double.TryParse(rA.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out val))
                                {
                                    throw new ArgumentException(string.Format("Expected a real number (i.e. 1.25), got '{0}'.", rA.Value));
                                }
                                values.Add(ElementSymbol.Create(eA.Value), val);
                            }
                            catch (Exception e)
                            {
                                throw new Exception(string.Format("Error in <{0}>: {1}", xml.Name.LocalName, e.Message));
                            }
                            break;
                        default:
                            throw new Exception(string.Format("Error in: unexpected element {0}.", elem.Name.LocalName));

                    }
                }

                foreach (var v in values)
                {
                    if (TunnelVdwRadii.GetRadius(v.Key) != v.Value) { UsingDefault = false; }
                    TunnelVdwRadii.SetRadius(v.Key, v.Value);
                }                
            } 
            catch (Exception e)
            {
                MessageBox.Show("Error loading 'vdwradii.xml':\n\n " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
