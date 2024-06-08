/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Tunnels.Core.Helpers;

namespace WebChemistry.Tunnels.Core.Geometry
{
    public class TunnelScalarField
    {
        public SurfaceScalarField Surface { get; private set; }
        public CubicSplineInterpolation CenterlineValue { get; private set; }
        public double CenterlineMinValue { get; private set; }
        public double CenterlineMaxValue { get; private set; }
        public Tunnel Tunnel { get; private set; }
        public double Total { get; private set; }
        
        public Dictionary<TunnelProfile.Node, double> GetValues(TunnelProfile profile)
        {
            return profile.ToDictionary(n => n, n => CenterlineValue.Interpolate(n.T));
        }
        
        public TunnelScalarField(Tunnel t, SurfaceScalarField surface, CubicSplineInterpolation centerlineValues, double centerlineMin, double centerlineMax)
        {
            this.Tunnel = t;
            this.Surface = surface;
            this.CenterlineValue = centerlineValues;
            this.Total = surface.Values.Length == 0 ? 0.0 : surface.Values.Where(v => v.HasValue).Sum(v => v.Value);
            this.CenterlineMinValue = centerlineMin;
            this.CenterlineMaxValue = centerlineMax;
        }
    }
}
