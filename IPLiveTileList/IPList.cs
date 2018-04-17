using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Networking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace IPLiveTileList
{
    public class IPList
    {
        public string IPAddress { get; set; }
        public string InterfaceType { get; set; }
        public string InterfaceIcon { get; set; }
        public string DomainStatus { get; set; }
        public string NetworkProfileName { get; set; }
        public string SSID { get; set; }

        public IPList(string IPLIAddress, string IPLIType, string IPLIIcon, string IPLIDomain, string IPLISSID)
        {
            IPAddress = IPLIAddress;
            InterfaceType = IPLIType;
            InterfaceIcon = IPLIIcon;
            DomainStatus = IPLIDomain;
            SSID = IPLISSID;
        }

        public static List<IPList> getIPList(bool DomainOnly)
        {
            List<IPList> IPList = new List<IPList>();
            IReadOnlyList<Windows.Networking.Connectivity.ConnectionProfile> ConnectionProfileList = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            foreach (HostName hostName in Windows.Networking.Connectivity.NetworkInformation.GetHostNames().Where(hn => (hn.Type == HostNameType.Ipv4) && (hn.DisplayName != null)))
            {
                string DomainStatus = "None";
                string SSID = null;

                if (ConnectionProfileList.Any(p => (p.NetworkAdapter.NetworkAdapterId == hostName.IPInformation?.NetworkAdapter.NetworkAdapterId)))
                {
                    Windows.Networking.Connectivity.ConnectionProfile profile = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles().Where(p => p.NetworkAdapter.NetworkAdapterId == hostName.IPInformation.NetworkAdapter.NetworkAdapterId).FirstOrDefault();
                    DomainStatus = profile.GetDomainConnectivityLevel().ToString();
                    Windows.Networking.Connectivity.WlanConnectionProfileDetails wlanDetails = profile.IsWlanConnectionProfile ? profile.WlanConnectionProfileDetails : null;
                    SSID = wlanDetails?.GetConnectedSsid();
                } 

                if (!((DomainOnly) && (DomainStatus == "None")))
                {
                    IPList.Add(new IPList(
                        hostName.DisplayName.ToString(),
                        hostName.IPInformation?.NetworkAdapter.IanaInterfaceType.ToString(),
                        getIconByInterfaceType(hostName.IPInformation?.NetworkAdapter.IanaInterfaceType),
                        DomainStatus,
                        SSID));
                }
            }
            return IPList.OrderBy(IP => IP.DomainStatus).ThenBy(IP => IP.IPAddress).ToList();
        }

        private static string getIconByInterfaceType(uint? InterfaceType)
        {
            switch (InterfaceType)
            {
                case 6: return "\uE839";   // Ethernet (x: eb55, !:eb56)
                case 71: return "\uE704";   // 802.11 Wireless
                default: return "\uE8CE";   // bluetooth:e702, disconnected:ea14
            }
        }
    }
    public sealed class IPListStringVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value?.ToString() == "None") { return Visibility.Collapsed; }
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public sealed class IPListStringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (parameter == null) return value;
            return string.Format((string)parameter, value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
