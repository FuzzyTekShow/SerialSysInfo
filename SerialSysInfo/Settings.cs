using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SerialSysInfo
{
    public static class Settings
    {
        // Store all the settings
        public static string Port { get; private set; }
        public static int Baud { get; private set; }
        public static int UpdateFrequency { get; private set; }
        public static bool UpdateGUI { get; private set; }
        public static bool StartOnBoot { get; private set; }
        public static bool StartMinimized { get; private set; }


        public static void SaveSettings(string port, int baud, int updateFrequency, bool updateGUI, bool startOnBoot, bool startMinimized)
        {
            Port = port;
            Baud = baud;
            UpdateFrequency = updateFrequency;
            UpdateGUI = updateGUI;
            StartOnBoot = startOnBoot;
            StartMinimized = startMinimized;

            // Set and save the settings
            Properties.Settings.Default.port = Port;
            Properties.Settings.Default.baud = Baud;
            Properties.Settings.Default.updateFrequency = UpdateFrequency;
            Properties.Settings.Default.updateGUI = UpdateGUI;
            Properties.Settings.Default.startMinim = StartMinimized;
            Properties.Settings.Default.startOnBoot = StartOnBoot;

            Properties.Settings.Default.Save();
        }


        /// <summary>
        /// Start the software at windows boot
        /// </summary>
        private static void EnableStartOnBoot()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
        }

        /// <summary>
        /// Disables starting on Windows boot
        /// </summary>
        private static void DisableStartOnBoot()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            key.DeleteSubKey(curAssembly.GetName().Name);
        }


        public static void GetSettings()
        {
            Port = Properties.Settings.Default.port;
            Baud = Properties.Settings.Default.baud;
            UpdateFrequency = Properties.Settings.Default.updateFrequency;
            UpdateGUI = Properties.Settings.Default.updateGUI;
            StartMinimized = Properties.Settings.Default.startMinim;
            StartOnBoot = Properties.Settings.Default.startOnBoot;
        }
    }
}
