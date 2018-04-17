using IPLiveTileTiles;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace IPLiveTileTasks
{
    public sealed class TileBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            Debug.WriteLine("INFO:" + taskInstance.Task.Name);
            Debug.WriteLine("CALL GEN TILES");
            Tile.GenerateTiles();
            deferral.Complete();
        }
    }

    public sealed class TileTaskReg
    {
        public static async void RegisterTileBackgroundTimerTask()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity || result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                if ((bool)localSettings.Values["UpdateOnTimer"])
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == "TileBackgroundTimerTask")
                        {
                            task.Value.Unregister(true);
                        }
                    }
                    BackgroundTaskBuilder timerBuilder = new BackgroundTaskBuilder();
                    timerBuilder.Name = "TileBackgroundTimerTask";
                    timerBuilder.TaskEntryPoint = "IPLiveTileTasks.TileBackgroundTask";
                    timerBuilder.SetTrigger(new TimeTrigger(Convert.ToUInt32(localSettings.Values["UpdateTimerInterval"]), false));
                    timerBuilder.Register();
                }
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == "TileBackgroundTimerTask")
                        {
                            task.Value.Unregister(true);
                        }
                    }
                }
            }
        }

        public static async void RegisterTileBackgroundEventTask()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity || result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                if ((bool)localSettings.Values["UpdateOnNetworkChange"])
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == "TileBackgroundEventTask")
                        {
                            return;
                        }
                    }
                    BackgroundTaskBuilder eventBuilder = new BackgroundTaskBuilder();
                    eventBuilder.Name = "TileBackgroundEventTask";
                    eventBuilder.TaskEntryPoint = "IPLiveTileTasks.TileBackgroundTask";
                    eventBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.NetworkStateChange, false));
                    eventBuilder.Register();
                }
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == "TileBackgroundEventTask")
                        {
                            task.Value.Unregister(true);
                        }
                    }
                }
            }
        }
    }
}
