using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;

namespace SerialSysInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string version = $"v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Revision}";
        private readonly string websiteLink = "https://github.com/fuzzytekshow/serialsysinfo";
        private TaskbarIcon tbi;
        private MetricData md;
        private BackgroundWorker serialWorker;

        private bool isConnected = false;


        public MainWindow()
        {
            InitializeComponent();
            InitSettings();
            InitTrayIcon();
            InitSerialPorts();
            InitMetrics();
        }


        /// <summary>
        /// Gets any user saved settings on run
        /// </summary>
        private void InitSettings()
        {
            Settings.GetSettings();

            cbPortSelection.SelectedItem = Settings.Port;
            cbPortSelection.Text = Settings.Port;
            tbBaudSelection.Text = Settings.Baud.ToString();
            tbUpdateFrequency.Text = Settings.UpdateFrequency.ToString();
            cbGUIUpdateData.IsChecked = Settings.UpdateGUI;
            cbStartMinim.IsChecked = Settings.StartMinimized;
            cbStartWithWindows.IsChecked = Settings.StartOnBoot;
            cbStartSerial.IsChecked = Settings.StartSerialOnLoad;

            if (Settings.StartMinimized)
            {
                Hide();
            }

            if (Settings.StartSerialOnLoad)
            {
                StartSerialConnection();
            }
        }


        /// <summary>
        /// Initialise the metrics
        /// </summary>
        private void InitMetrics()
        {
            md = new MetricData();
            serialWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            serialWorker.DoWork += SerialWorker_DoWork;
        }


        /// <summary>
        /// Begin retrieving metric information every second
        /// </summary>
        private void StartMetrics()
        {
            if (serialWorker.IsBusy)
            {
                serialWorker.CancelAsync();
                while (serialWorker.IsBusy) { }
            }
            serialWorker.RunWorkerAsync();
        }


        /// <summary>
        /// Set the metric information from system
        /// </summary>
        /// <returns></returns>
        private void SerialWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            while (!worker.CancellationPending)
            {
                md.GetMetricData();
                    
                // Update the GUI if set to do it
                if (Settings.UpdateGUI)
                {
                    Dispatcher.Invoke(UpdateGUI);
                }
                    
                // If serial is connected send the data
                bool dataSent = false;
                if (isConnected && !dataSent)
                {
                    SendSerialData();
                    dataSent = true;
                }

                // If not about to cancel, wait for update frequency
                if (!worker.CancellationPending)
                {
                    System.Threading.Thread.Sleep(Settings.UpdateFrequency * 1000);
                }
            }

            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
        }


        /// <summary>
        /// When the worker stops
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ClearGUIData();
            // Reset the connection button text
            btnStartStop.Content = "Start sending serial data";
        }


        private void UpdateGUI()
        {
            //Temperature
            tbCPUTemp.Text = $"{md.CPUTemp:0.0}°c";
            tbGPUTemp.Text = $"{md.GPUTemp:0.0}°c";

            // CPU Load Percentage
            tbCPUUsage.Text = $"{md.CPUUsage:0}%";
            // GPU Load Percentage
            tbGPUUsage.Text = $"{md.GPUUsage:0}%";

            // Frequency

            // CPU
            tbCPUFreq.Text = md.CPUFreq / 1000 > 0 ? $"{md.CPUFreq / 1000:0.00} Ghz" : $"{md.CPUFreq:0.00} Mhz";
            // GPU Core
            tbGPUFreq.Text = md.GPUCoreFreq / 1000 > 0 ? $"{md.GPUCoreFreq / 1000:0.00} Ghz" : $"{md.GPUCoreFreq:0.00} Mhz";
            // GPU Mem
            tbGPUMemFreq.Text = md.GPUMemFreq / 1000 > 0 ? $"{md.GPUMemFreq / 1000:0.00} Ghz" : $"{md.GPUMemFreq:0.00} Mhz";

            // RAM
            string ramUsedSuffix = md.RAMUsed / 1000 > 0 ? "GB" : "MB";
            string ramTotalSuffix = md.RAMTotal / 1000 > 0 ? "GB" : "MB";

            tbRAMUsage.Text = $"{md.RAMUsed:0.00} {ramUsedSuffix} / {md.RAMTotal:0.00} {ramTotalSuffix} ({md.RAMFreePercentage:#}%)";
        }


        #region Serial Ports

        /// <summary>
        /// Setup serial port stuff
        /// </summary>
        private void InitSerialPorts()
        {
            ListSerialPorts();
        }


        /// <summary>
        /// Get a list of all the serial ports
        /// </summary>
        private void ListSerialPorts()
        {
            List<string> ports = new List<string>();
            ports.Clear();

            foreach (string port in SerialPort.GetPortNames())
            {
                // Only add valid ports
                if (port.StartsWith("COM"))
                {
                    ports.Add(port);
                }
            }

            // Add the ports to the dropdown
            cbPortSelection.Items.Clear();
            cbPortSelection.ItemsSource = ports;
        }

        /// <summary>
        /// Make sure the entered text is an integer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


        #endregion

        #region Tray Icon Stuff

        private void InitTrayIcon()
        {
            tbi = trayIcon;
            tbi.ToolTipText = Title;
        }


        /// <summary>
        /// Called when the window state is minimised or restored
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            // If window was minimised
            if (WindowState == WindowState.Minimized)
            {
                // Show balloon message
                tbi.ShowBalloonTip(Title, "SerialSysInfo is now running in the background.", BalloonIcon.Info);
                Hide();
            }
            else if (WindowState == WindowState.Normal)
            {
                Show();
            }
        }

        #endregion

        #region Buttons


        /// <summary>
        /// Called when the user clicks the start/stop button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            StartSerialConnection();
        }


        /// <summary>
        /// Called when Save + apply button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnApplyChanges_Click(object sender, RoutedEventArgs e)
        {

            // Save the settings
            Settings.SaveSettings(cbPortSelection.Text,
                int.Parse(tbBaudSelection.Text), int.Parse(tbUpdateFrequency.Text),
                (bool)cbGUIUpdateData.IsChecked, (bool)cbStartWithWindows.IsChecked,
                (bool)cbStartMinim.IsChecked, (bool)cbStartSerial.IsChecked);

            // Set the GUI data to blank if not to be shown
            if (!Settings.UpdateGUI)
            {
                ClearGUIData();
            }

            // Enable or disable on startup
            if (Settings.StartOnBoot)
            {
                Settings.EnableStartOnBoot();
            }
            else
            {
                Settings.DisableStartOnBoot();
            }

            // Show settings saved message
            _ = MessageBox.Show($"Settings saved.",
                        "Settings", MessageBoxButton.OK, MessageBoxImage.Information);


        }


        /// <summary>
        /// Minimise the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        #endregion

        #region Context Menu


        private void conExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        /// <summary>
        /// Return the window to normal after being minimized. Called when tray icon is double clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void conNormalWindow_Click(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Hidden)
            {
                Visibility = Visibility.Visible;
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate ()
                    {
                        WindowState = WindowState.Normal;
                        _ = Activate();
                    })
                );
            }
        }


        #endregion

        #region Misc Methods


        /// <summary>
        /// Sends serial data
        /// </summary>
        private void SendSerialData()
        {
            string sender = SerialSender.SendData(md.GetSerialData());

            if (sender != "ok")
            {
                _ = MessageBox.Show($"An error occured. {sender}",
                        "Serial Error", MessageBoxButton.OK, MessageBoxImage.Error);
                serialWorker.CancelAsync();
                SerialSender.Disconnect();
                isConnected = false;
                return;
            }
        }



        /// <summary>
        /// Clears the GUI data area
        /// </summary>
        private void ClearGUIData()
        {
            tbCPUTemp.Text = tbGPUTemp.Text = tbCPUFreq.Text =
                    tbGPUFreq.Text = tbGPUMemFreq.Text = tbRAMUsage.Text =
                    tbGPUUsage.Text = tbCPUUsage.Text = "----";
        }


        /// <summary>
        /// Detect window keypresses
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // CHM Help file
            if (e.Key == Key.F1)
            {
                ShowHelp();
            }
        }


        /// <summary>
        /// Shows the help file
        /// </summary>
        private void ShowHelp()
        {
            Process.Start(@"help.chm");
        }


        /// <summary>
        /// Starts the actual serial connection
        /// </summary>
        private void StartSerialConnection()
        {
            if (!isConnected)
            {
                string connResult = SerialSender.Connect(cbPortSelection.Text, int.Parse(tbBaudSelection.Text));

                if (connResult == "True")
                {
                    StartMetrics();
                    btnStartStop.Content = "Stop sending serial data";
                    isConnected = true;
                }
                else
                {
                    _ = MessageBox.Show($"An error occured. {connResult}",
                        "Serial Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                if (serialWorker.IsBusy)
                {
                    serialWorker.CancelAsync();
                }

                btnStartStop.Content = "Connect to serial device";
                isConnected = false;
                SerialSender.Disconnect();
            }
        }


        /// <summary>
        /// Called when the window is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If normal window, ask if sure
            if (WindowState == WindowState.Normal)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to exit?", "Exit Application", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }

            // Make sure the serial port is closed
            SerialSender.Disconnect();
        }

        #endregion


        /// <summary>
        /// Called when the "About" menu item is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Show();
        }


        /// <summary>
        /// Called when the HELP menu item is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuGuide_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
