using System;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace IPLiveTile
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            var MainPageViewModel = new IPListViewModel();

            MainPageViewModel.IPListMenuItems.Add(new SplitViewMenu.SplitViewMenuItem
            {
                Label = "Home",
                DestinationPage = typeof(IPListViewMain),
                Symbol = Symbol.Home,
                FrameHeaderLabel = "IP List"
            });
            MainPageViewModel.IPListMenuItems.Add(new SplitViewMenu.SplitViewMenuItem
            {
                Label = "Settings",
                DestinationPage = typeof(IPListViewSettings),
                Symbol = Symbol.Setting,
                FrameHeaderLabel = "Settings"
            });

            DataContext = MainPageViewModel;
        }
    }
}