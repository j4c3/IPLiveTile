using Expander;
using IPLiveTileTiles;
using IPLiveTileTasks;
using NotificationsExtensions;
using SplitViewMenu;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.Foundation;
using System.Reflection;

namespace IPLiveTile
{
    sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (!(localSettings.Values.ContainsKey("DomainOnly"))) { localSettings.Values["DomainOnly"] = true; }
            if (!(localSettings.Values.ContainsKey("UpdateOnNetworkChange"))) { localSettings.Values["UpdateOnNetworkChange"] = true; }
            if (!(localSettings.Values.ContainsKey("UpdateOnTimer"))) { localSettings.Values["UpdateOnTimer"] = true; }
            if (!(localSettings.Values.ContainsKey("UpdateTimerInterval"))) { localSettings.Values["UpdateTimerInterval"] = 240; }

            TileTaskReg.RegisterTileBackgroundEventTask();
            TileTaskReg.RegisterTileBackgroundTimerTask();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            ApplicationView.PreferredLaunchViewSize = new Size(272, 412);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(272, 200));
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                TileTaskReg.RegisterTileBackgroundEventTask();
                TileTaskReg.RegisterTileBackgroundTimerTask();
                Tile.GenerateTiles();
                this.Exit();
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (e.PrelaunchActivated)
            {
                Tile.GenerateTiles();
                TileTaskReg.RegisterTileBackgroundEventTask();
                TileTaskReg.RegisterTileBackgroundTimerTask();
            }

            ApplicationView.PreferredLaunchViewSize = new Size(272, 412);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(272, 200));
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)                                  // Do not repeat app initialization when the Window already has content, just ensure that the window is active
            {
                rootFrame = new Frame();                            // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;                 // Place the frame in the current Window
            }
            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), e.Arguments);  // When the navigation stack isn't restored navigate to the first page, configuring the new page by passing required information as a navigation parameter
            }
            Window.Current.Activate();                              // Ensure the current window is active
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
