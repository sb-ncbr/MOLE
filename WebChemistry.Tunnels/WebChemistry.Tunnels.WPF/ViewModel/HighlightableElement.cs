/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    public abstract class HighlightableElement : Animatable
    {
        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
            set { SetValue(IsHighlightedProperty, value); }
        }

        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(HighlightableElement), new PropertyMetadata(false, OnIsHighlightedChanged));

        private static void OnIsHighlightedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as HighlightableElement).OnIsHighlightedChanged();
        }

        protected virtual void OnIsHighlightedChanged()
        {
        }
        
        protected override Freezable CreateInstanceCore()
        {
            return this;
        }
    }
}
