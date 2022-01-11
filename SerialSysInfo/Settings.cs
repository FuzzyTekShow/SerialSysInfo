using Microsoft.Win32.TaskScheduler;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

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
        public static bool StartSerialOnLoad { get; private set; }
        public static bool StopOnSleep { get; private set; }

        // Shortcut stuff
        private static readonly string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string app = Assembly.GetExecutingAssembly().Location;


        /// <summary>
        /// Save the settings
        /// </summary>
        /// <param name="port"></param>
        /// <param name="baud"></param>
        /// <param name="updateFrequency"></param>
        /// <param name="updateGUI"></param>
        /// <param name="startOnBoot"></param>
        /// <param name="startMinimized"></param>
        /// <param name="startSerialOnLoad"></param>
        public static void SaveSettings(string port, int baud, int updateFrequency, bool updateGUI, bool startOnBoot, bool startMinimized, bool startSerialOnLoad, bool stopOnSleep)
        {
            Port = port;
            Baud = baud;
            UpdateFrequency = updateFrequency;
            UpdateGUI = updateGUI;
            StartOnBoot = startOnBoot;
            StartMinimized = startMinimized;
            StartSerialOnLoad = startSerialOnLoad;
            StopOnSleep = stopOnSleep;

            // Set and save the settings
            Properties.Settings.Default.port = Port;
            Properties.Settings.Default.baud = Baud;
            Properties.Settings.Default.updateFrequency = UpdateFrequency;
            Properties.Settings.Default.updateGUI = UpdateGUI;
            Properties.Settings.Default.startMinim = StartMinimized;
            Properties.Settings.Default.startOnBoot = StartOnBoot;
            Properties.Settings.Default.startSerialOnLoad = StartSerialOnLoad;
            Properties.Settings.Default.stopOnSleep = StopOnSleep;

            Properties.Settings.Default.Save();
        }
        

        /// <summary>
        /// Start the software at windows boot
        /// </summary>
        public static void EnableStartOnBoot()
        {
            Task task = TaskService.Instance.GetTask("SerialSysInfo");

            if (task == null)
            {
                // Task doesn't exist so create it
                TaskService ts = new TaskService();
                TaskDefinition td = ts.NewTask();
                // Get the EXE path
                ExecAction exe = new ExecAction(Assembly.GetExecutingAssembly().Location);
                td.Actions.Add(exe);
                // Run as an admin
                td.Principal.RunLevel = TaskRunLevel.Highest;
                // Task description
                td.RegistrationInfo.Description = "Launches SerialSysInfo at logon";
                // Start at logon of user
                td.Triggers.AddNew(TaskTriggerType.Logon);
                // Create the task and enable it
                task = ts.RootFolder.RegisterTaskDefinition("SerialSysInfo", td);
                task.Enabled = true;
            }
            else { task.Enabled = true; }
        }


        /// <summary>
        /// Disables starting on Windows boot
        /// </summary>
        public static void DisableStartOnBoot()
        {
            Task task = TaskService.Instance.GetTask("SerialSysInfo");

            // Check the task exists and delete it if so
            if (task != null)
            {
                TaskService.Instance.RootFolder.DeleteTask(task.Name);
            }
            
        }


        public static void GetSettings()
        {
            Port = Properties.Settings.Default.port;
            Baud = Properties.Settings.Default.baud;
            UpdateFrequency = Properties.Settings.Default.updateFrequency;
            UpdateGUI = Properties.Settings.Default.updateGUI;
            StartMinimized = Properties.Settings.Default.startMinim;
            StartOnBoot = Properties.Settings.Default.startOnBoot;
            StartSerialOnLoad = Properties.Settings.Default.startSerialOnLoad;
            StopOnSleep = Properties.Settings.Default.stopOnSleep;
        }


        #region Shortcut COM Stuff

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        #endregion

    }
}
