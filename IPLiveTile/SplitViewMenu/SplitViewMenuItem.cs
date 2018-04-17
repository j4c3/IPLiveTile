using System;
using Windows.UI.Xaml.Controls;

namespace SplitViewMenu
{
    public interface ISplitViewMenuItem
    {
        Type DestinationPage { get; }
        object Arguments { get; }
        string Label { get; }
        string FrameHeaderLabel { get; }
    }

    public sealed class SplitViewMenuItem : ISplitViewMenuItem
    {
        public string Label { get; set; }
        public string FrameHeaderLabel { get; set; }
        public Symbol Symbol { get; set; }
        public char SymbolAsChar => (char)Symbol;
        public object Arguments { get; set; }
        public Type DestinationPage { get; set; }
    }
}
