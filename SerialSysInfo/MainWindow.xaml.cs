using Hardcodet.Wpf.TaskbarNotification;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SerialSysInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TaskbarIcon tbi;
        MetricData md;

        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            InitSettings();
            InitTrayIcon();
            InitSerialPorts();

            md = new MetricData();
            StartMetrics();
        }


        /// <summary>
        /// Gets any user saved settings on run
        /// </summary>
        private void InitSettings()
        {
            Settings.GetSettings();

            cbPortSelection.SelectedItem = Settings.Port;
            tbBaudSelection.Text = Settings.Baud.ToString();
            tbUpdateFrequency.Text = Settings.UpdateFrequency.ToString();
            cbGUIUpdateData.IsChecked = Settings.UpdateGUI;
            cbStartMinim.IsChecked = Settings.StartMinimized;
            cbStartWithWindows.IsChecked = Settings.StartOnBoot;

            if (Settings.StartMinimized)
                this.WindowState = WindowState.Minimized;

        }


        /// <summary>
        /// Begin retrieving metric information every second
        /// </summary>
        private async void StartMetrics()
        {
            await SetMetrics();
        }


        /// <summary>
        /// Set the metric information from system
        /// </summary>
        /// <returns></returns>
        private async Task SetMetrics()
        {

            string ramUsedSuffix = string.Empty;
            string ramTotalSuffix = string.Empty;

            // Repeat every second
            while (true)
            {
                Task delayTask = Task.Delay(Settings.UpdateFrequency * 1000);

                md.GetMetricData();

                // Update data if update checkbox is checked
                if (Settings.UpdateGUI)
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

                // If serial is connected send the data
                if (isConnected)
                {
                    SendSerialData();
                }

                await delayTask;
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
                conToggle.Header = "Restore window";
                this.Hide();
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.Show();
                conToggle.Header = "Minimize to tray";
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
            if (!isConnected)
            {
                string connResult = SerialSender.Connect(cbPortSelection.Text, int.Parse(tbBaudSelection.Text));

                if (connResult == "True")
                {
                    btnStartStop.Content = "Disconnect";
                    isConnected = true;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show($"An error occured. {connResult}",
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


        private void SendSerialData()
        {
            SerialSender.SendData(md.GetSerialData());
        }


        /// <summary>
        /// Called when the exit button is clicked on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            // Exit the application
            //MessageBoxResult result = MessageBox.Show("Are you sure you want to exit?", "Exit?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            this.Close();
        }


        #endregion

        #region Context Menu


        private void conExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void conToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindow();
        }

        private void ToggleWindow()
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Minimized;
            }
            else if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
                this.Show();
            }
        }


        #endregion


        private void cbPortSelection_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            btnStartStop.IsEnabled = cbPortSelection.SelectedItem != null;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Make sure the serial port is closed
            SerialSender.Disconnect();
        }


        private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
        {

            // Save the settings
            Settings.SaveSettings(cbPortSelection.Text,
                int.Parse(tbBaudSelection.Text), int.Parse(tbUpdateFrequency.Text),
                (bool)cbGUIUpdateData.IsChecked, (bool)cbStartWithWindows.IsChecked,
                (bool)cbStartMinim.IsChecked);

            // Set the GUI data to blank if not to be shown
            if (!Settings.UpdateGUI)
            {
                tbCPUTemp.Text = tbGPUTemp.Text = tbCPUFreq.Text =
                    tbGPUFreq.Text = tbGPUMemFreq.Text = tbRAMUsage.Text = "----";
            }

            
        }
    }
}
