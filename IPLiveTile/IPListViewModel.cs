using IPLiveTileTasks;
using IPLiveTileTiles;
using SplitViewMenu;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;

namespace IPLiveTile
{
    public class IPListViewModel : INotifyPropertyChanged
    {
        public static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        DelayTimerCall timerCall = new DelayTimerCall(2);

        public IPListViewModel()
        {
            IPListMenuItems = new ObservableCollection<SplitViewMenuItem>();
            InitialPage = typeof(IPListViewMain);
            HeaderLabel = HeaderLabel;
            DomainOnly = DomainOnly;
        }
        public ObservableCollection<SplitViewMenuItem> IPListMenuItems { get; }
        public Type InitialPage { get; }
        public string HeaderLabel { get; set; }
        public bool DomainOnly
        {
            get { return (bool)localSettings.Values["DomainOnly"]; }
            set
            {
                localSettings.Values["DomainOnly"] = value;
                Tile.GenerateTiles();
            }
        }

        public class DelayTimerCall
        {
            int _delay;
            DispatcherTimer _timer = new DispatcherTimer();

            public DelayTimerCall(int delay) { _delay = delay; }
            public void CallMethod(Action action)
            {
                if (!_timer.IsEnabled)
                {
                    _timer.Interval = TimeSpan.FromSeconds(_delay);
                    _timer.Tick += (s, e) => { action(); };
                    _timer.Start();
                }
                else
                {
                    _timer.Start();
                }
            }
        }

        public bool UpdateOnNetworkChange
        {
            get { return (bool)localSettings.Values["UpdateOnNetworkchange"]; }
            set
            {
                localSettings.Values["UpdateOnNetworkChange"] = value;
                TileTaskReg.RegisterTileBackgroundEventTask();
            }
        }
        public bool UpdateOnTimer
        {
            get { return (bool)localSettings.Values["UpdateOnTimer"]; }
            set
            {
                localSettings.Values["UpdateOnTimer"] = value;
                OnPropertyChanged("UpdateOnTimer");
                TileTaskReg.RegisterTileBackgroundTimerTask();
            }
        }
        public string PackageVersion
        {
            get
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }
        public int UpdateTimerInterval
        {
            get { return (int)localSettings.Values["UpdateTimerInterval"]; }
            set
            {
                localSettings.Values["UpdateTimerInterval"] = value;
                timerCall.CallMethod(() => TileTaskReg.RegisterTileBackgroundTimerTask());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
