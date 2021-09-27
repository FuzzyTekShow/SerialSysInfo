using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;

namespace SerialSysInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TaskbarIcon tbi;
        MetricData md;
        BackgroundWorker serialWorker;

        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            InitSettings();
            InitTrayIcon();
            InitSerialPorts();

            md = new MetricData();
            serialWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            serialWorker.DoWork += SerialWorker_DoWork;
            serialWorker.ProgressChanged += SerialWorker_ProgressChanged;
            StartMetrics();
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
                this.WindowState = WindowState.Minimized;
            }

            if (Settings.StartSerialOnLoad)
            {
                StartSerialConnection();
            }
        }


        /// <summary>
        /// Begin retrieving metric information every second
        /// </summary>
        private void StartMetrics()
        {
            if (serialWorker.IsBusy != true)
            {
                serialWorker.RunWorkerAsync();
            }
        }



        /// <summary>
        /// Set the metric information from system
        /// </summary>
        /// <returns></returns>
        private void SerialWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            // Repeat
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                while (!worker.CancellationPending)
                {
                    md.GetMetricData();

                    Random rand = new Random();
                    worker.ReportProgress(rand.Next(0, 100));

                    // If serial is connected send the data
                    bool dataSent = false;
                    if (isConnected && !dataSent)
                    {
                        SendSerialData();
                        dataSent = true;
                    }

                    System.Threading.Thread.Sleep(Settings.UpdateFrequency * 1000);
                    dataSent = false;
                }
            }
        }


        private void SerialWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (Settings.UpdateGUI)
            {
                string ramUsedSuffix = string.Empty;
                string ramTotalSuffix = string.Empty;

                //Temperature
                tbCPUTemp.Text = $"{md.CPUTemp:0.0}°c";
                tbGPUTemp.Text = $"{md.GPUTemp:0.0}°c";

                // CPU Load Percentage
                tbCPUUsage.Text = $"{md.CPUUsage:0}%";
                // GPU Load Percentage
                tbGPUUsage.Text = $"{md.GPUUsage:0}%";

                // Frequency

                // CPU
                if (md.CPUFreq / 1000 > 0)
                    tbCPUFreq.Text = $"{md.CPUFreq / 1000:0.00} Ghz";
                else
                    tbCPUFreq.Text = $"{md.CPUFreq:0.00} Mhz";
                // GPU Core
                if (md.GPUCoreFreq / 1000 > 0)
                    tbGPUFreq.Text = $"{md.GPUCoreFreq / 1000:0.00} Ghz";
                else
                    tbGPUFreq.Text = $"{md.GPUCoreFreq:0.00} Mhz";
                // GPU Mem
                if (md.GPUMemFreq / 1000 > 0)
                    tbGPUMemFreq.Text = $"{md.GPUMemFreq / 1000:0.00} Ghz";
                else
                    tbGPUMemFreq.Text = $"{md.GPUMemFreq:0.00} Mhz";

                // RAM
                if (md.RAMUsed / 1000 > 0)
                    ramUsedSuffix = "GB";
                else
                    ramUsedSuffix = "MB";
                if (md.RAMTotal / 1000 > 0)
                    ramTotalSuffix = "GB";
                else
                    ramTotalSuffix = "MB";

                tbRAMUsage.Text = $"{md.RAMUsed:0.00} {ramUsedSuffix} / {md.RAMTotal:0.00} {ramTotalSuffix} ({md.RAMFreePercentage:#}%)";
            }
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
            tbi.ToolTipText = this.Title;
        }


        /// <summary>
        /// Called when the window state is minimised or restored
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            // If window was minimised
            if (this.WindowState == WindowState.Minimized)
            {
                // Show balloon message
                tbi.ShowBalloonTip(Title, "SerialSysInfo is now running in the background.", BalloonIcon.Info);
                this.Hide();
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.Show();
            }
        }

        #endregion

        #region Buttons


        /// <summary>
        /// Called when the user clicks the start/stop button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            StartSerialConnection();
        }


        /// <summary>
        /// Called when Save + apply button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
        {

            // Save the settings
            Settings.SaveSettings(cbPortSelection.Text,
                int.Parse(tbBaudSelection.Text), int.Parse(tbUpdateFrequency.Text),
                (bool)cbGUIUpdateData.IsChecked, (bool)cbStartWithWindows.IsChecked,
                (bool)cbStartMinim.IsChecked, (bool)cbStartSerial.IsChecked);

            MessageBox.Show($"Settings saved.",
                        "Settings", MessageBoxButton.OK, MessageBoxImage.Information);

            // Set the GUI data to blank if not to be shown
            if (!Settings.UpdateGUI)
            {
                tbCPUTemp.Text = tbGPUTemp.Text = tbCPUFreq.Text =
                    tbGPUFreq.Text = tbGPUMemFreq.Text = tbRAMUsage.Text =
                    tbGPUUsage.Text = tbCPUUsage.Text = "----";
            }


        }


        /// <summary>
        /// Minimise the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        #endregion

        #region Context Menu


        private void conExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Return the window to normal after being minimized. Called when tray icon is double clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void conNormalWindow_Click(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Hidden)
            {
                this.Visibility = Visibility.Visible;
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate ()
                    {
                        this.WindowState = WindowState.Normal;
                        this.Activate();
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
                MessageBox.Show($"An error occured. {sender}",
                        "Serial Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
                    btnStartStop.Content = "Stop sending serial data";
                    isConnected = true;
                }
                else
                {
                    MessageBox.Show($"An error occured. {connResult}",
                        "Serial Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
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
    }
}
