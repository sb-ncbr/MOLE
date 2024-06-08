/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.WPF.Services
{
    public interface IBusyIndication
    {
        bool IsBusy { get; set; }
        string StatusText { get; set; }
    }
}
