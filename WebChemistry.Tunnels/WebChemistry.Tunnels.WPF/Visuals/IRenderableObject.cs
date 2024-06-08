/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System.Windows.Media.Media3D;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    public interface IRenderableObject
    {
        bool IsTransparent { get; }
        Visual3D Visual { get; }
    }
}
