using System;

namespace System.Windows.Forms
{
    internal static class Application
    {
        internal static string StartupPath
        {
            get { return AppContext.BaseDirectory; }
        }
    }
}
