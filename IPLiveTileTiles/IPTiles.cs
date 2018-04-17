using IPLiveTileList;
using NotificationsExtensions;
using NotificationsExtensions.Tiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace IPLiveTileTiles
{
    public class Tile
    {
        public static void GenerateTiles()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            bool domainOnlyValue = (bool)localSettings.Values["DomainOnly"];
            List<IPList> tileIPList = IPList.getIPList(domainOnlyValue);
            TileUpdater tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            if (tileIPList.Count > 1) { tileUpdater.EnableNotificationQueue(true); }
            tileUpdater.Clear();
            int loop = 0;
            foreach (IPList ipListItem in tileIPList)
            {
                loop++;
                XmlDocument tileXML = GenerateTileContent(domainOnlyValue, ipListItem, tileIPList.Count, loop).GetXml();
                TileNotification tileNotification = new TileNotification(tileXML);
                tileNotification.Tag = ipListItem.IPAddress;
                tileUpdater.Update(tileNotification);
            }
        }

        public static TileContent GenerateTileContent(bool domainOnlyValue, IPList ipListItem, int ipListCount, int ipListPosition)
        {
            string TileTextTitle = "IP Address"; if (ipListItem.DomainStatus == "Authenticated") { TileTextTitle = "Domain IP"; }
            string displayName = "IPLiveTile";
            TileBranding branding = TileBranding.NameAndLogo;
            if (ipListCount > 1)
            {
                branding = TileBranding.Name;
                displayName = displayName + " (" + ipListPosition + "/" + ipListCount + ")";
            }
            TileBindingContentAdaptive bindingContent = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = "Assets/2x2_Transparent.png"
                },
                Children =
                {
                    new AdaptiveText() { Text = TileTextTitle, HintStyle = AdaptiveTextStyle.CaptionSubtle },
                    new AdaptiveText() { Text = ipListItem.IPAddress, HintStyle = AdaptiveTextStyle.Base }
                }
            };

            TileBinding binding = new TileBinding()
            {
                Branding = branding,
                DisplayName = displayName,
                Content = bindingContent
            };

            TileBinding bindingWL = new TileBinding()
            {
                Branding = TileBranding.NameAndLogo,
                DisplayName = displayName,
                Content = bindingContent
            };

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = binding,
                    TileWide = bindingWL,
                    TileLarge = bindingWL
                }
            };
            return content;
        }
    }
}