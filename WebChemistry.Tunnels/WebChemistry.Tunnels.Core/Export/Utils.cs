/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Export
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Math;

    public class ExportSpectrumInfo
    {
        public double? DisplayedMin { get; set; }
        public double? DisplayedMax { get; set; }
        public double? ActualMin { get; set; }
        public double? ActualMax { get; set; }
    }
    
    public static class ExportColoringUtils
    {
        static double GetTwoSigmaValue(double[] data)
        {
            if (data.Length == 0) return 0;
            var avg = data.Average();
            var sigma = Math.Sqrt(data.Sum(d => (d - avg) * (d - avg)) / (double)data.Length);
            return avg + 2 * sigma;
        }

        /// <summary>
        /// Returns [negative average - 2 sigma, positive average + 2sigma]
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Tuple<double, double> GetTwoSigmaRange(IEnumerable<double> data)
        {
            var negative = data.Where(d => d < 0).Select(d => Math.Abs(d)).ToArray();
            var positive = data.Where(d => d >= 0).ToArray();
            return Tuple.Create(-GetTwoSigmaValue(negative), GetTwoSigmaValue(positive));
        }
        
        public static Vector3D GetFieldColorRedWhiteBlue(double? val, double min, double max)
        {   
            if (!val.HasValue) return new Vector3D();            
            var value = val.Value;
            if (Math.Abs(value) < 0.00001) return new Vector3D(1, 1, 1);
            var t = Math.Max(0.0, 1 - Math.Abs(value / min));
            if (value > 0) return new Vector3D(t, t, 1);
            return new Vector3D(1, t, t);
        }

        public static Vector3D GetFieldColorBlueWhiteRed(double? val, double min, double max)
        {
            if (!val.HasValue) return new Vector3D();
            var value = val.Value;
            if (Math.Abs(value) < 0.00001) return new Vector3D(1, 1, 1);
            var t = Math.Max(0.0, 1 - Math.Abs(value / min));
            if (value < 0) return new Vector3D(t, t, 1);
            return new Vector3D(1, t, t);
        }        
    }
}
