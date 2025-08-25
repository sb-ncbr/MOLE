/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Store VDW radii;
    /// </summary>
    public static class TunnelVdwRadii
    {
        static readonly Dictionary<ElementSymbol, double> radii;

        static TunnelVdwRadii()
        {
            radii = new Dictionary<ElementSymbol, double>
            {
                { ElementSymbols.H, 1.0 },
                { ElementSymbols.O, 1.45 },
                { ElementSymbols.S, 1.77 },
                { ElementSymbols.N, 1.55 },
                { ElementSymbols.C, 1.61 },
                { ElementSymbols.Fe, 1.7 },
                { ElementSymbols.P, 1.7 },
                { ElementSymbols.Si, 1.8 },
                { ElementSymbols.Al, 1.84 },
                { ElementSymbols.Li, 1.8 },
                { ElementSymbols.Na, 1.8 },
                { ElementSymbols.Cl, 1.75 }
            };
        }

        /// <summary>
        /// Returns a custom VDW radius for tunnel computation.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public static double GetTunnelSpecificVdwRadius(this IAtom atom)
        {
            return GetRadius(atom.ElementSymbol);
        }

        /// <summary>
        /// Set a custom radius.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="radius"></param>
        public static void SetRadius(ElementSymbol symbol, double radius)
        {
            radii[symbol] = radius;
        }

        /// <summary>
        /// Get current vdw radius.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static double GetRadius(ElementSymbol symbol)
        {
            double radius;
            if (radii.TryGetValue(symbol, out radius)) return radius;
            return ElementAndBondInfo.GetElementInfo(symbol).VdwRadius;
        }
    }
}
