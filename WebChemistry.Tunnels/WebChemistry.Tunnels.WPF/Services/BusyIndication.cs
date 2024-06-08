/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using System.Reactive.Linq;

namespace WebChemistry.Tunnels.WPF.Services
{
    public class BusyIndication : ObservableObject, IBusyIndication
    {
        static BusyIndication self;

        public static BusyIndication Instance
        {
            get 
            {
                self = self ?? new BusyIndication();
                return self;
            }
        }

        public static void SetBusy(bool busy)
        {
            Instance.IsBusy = busy;
        }

        public static void SetStatusText(string status)
        {
            Instance.StatusText = status;
        }

        IDisposable timeTracker;
        DateTime timeStarted;
        DateTime currentStarted;

        void StartTimeTrack()
        {
            TimeElapsed = "0.0s";
            timeStarted = DateTime.Now;
            timeTracker = Observable
                .Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(100))
                .Subscribe(_ => TimeElapsed = 
                    string.Format("{0}s",
                        //((DateTime.Now - currentStarted).TotalMilliseconds / 1000).ToStringInvariant("0.0"),
                        ((DateTime.Now - timeStarted).TotalMilliseconds / 1000).ToStringInvariant("0.0")));
        }

        void StopTimeTrack()
        {
            if (timeTracker != null) timeTracker.Dispose();
        }

        bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    if (isBusy) StartTimeTrack();
                    else StopTimeTrack();
                    NotifyPropertyChanged("IsBusy");
                }
            }
        }

        string status;
        public string StatusText
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    currentStarted = DateTime.Now;
                    status = value;
                    NotifyPropertyChanged("StatusText");
                }
            }
        }

        private string _timeElapsed = "n/a";
        public string TimeElapsed
        {
            get
            {
                return _timeElapsed;
            }
            set
            {
                if (_timeElapsed == value) return;

                _timeElapsed = value;
                NotifyPropertyChanged("TimeElapsed");
            }
        }

        private BusyIndication()
        {

        }
    }
}
