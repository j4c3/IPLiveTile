using IPLiveTileList;
using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace IPLiveTile
{
    public sealed partial class IPListViewMain : Page
    {
        public IPListViewMain()
        {
            InitializeComponent();
            //ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            IPListView.ItemsSource = IPList.getIPList(false);   //(bool)localSettings.Values["DomainOnly"]
        }
    }
}
