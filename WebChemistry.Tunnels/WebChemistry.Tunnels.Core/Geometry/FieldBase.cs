/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core.Geometry
{
    using WebChemistry.Framework.Math;

    public abstract class FieldBase
    {
        public string Name { get; private set; }

        public abstract double? Interpolate(Vector3D position);
        public abstract double? Interpolate(TunnelProfile.Node node, TunnelLayer layer);

        protected FieldBase(string name)
        {
            this.Name = name;
        }
    }
}
