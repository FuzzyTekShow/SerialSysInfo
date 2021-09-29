using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SerialSysInfo
{
    public class MetricData
    {
        private readonly Computer computer;
        private List<string> serialData;

        public float CPUTemp { get; private set; }
        public float GPUTemp { get; private set; }
        public float CPUUsage { get; private set; }
        public float GPUUsage { get; private set; }
        public float CPUFreq { get; private set; }
        public float GPUCoreFreq { get; private set; }
        public float GPUMemFreq { get; private set; }
        public float RAMUsed { get; private set; }
        public float RAMTotal { get; private set; }
        public float RAMFreePercentage { get; private set; }


        public MetricData()
        {
            serialData = new List<string>();

            computer = new Computer
            {
                CPUEnabled = true,
                GPUEnabled = true,
                RAMEnabled = true
            };
        }



        /// <summary>
        /// Gets the system information
        /// </summary>
        public void GetMetricData()
        {
            computer.Open();

            foreach (IHardware hardware in computer.Hardware)
            {
                // CPU
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    hardware.Update();

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        // Temperature
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("CPU Package"))
                        {
                            CPUTemp = sensor.Value.GetValueOrDefault();
                        }
                        // Frequency
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("CPU Core #1"))
                        {
                            CPUFreq = sensor.Value.GetValueOrDefault();
                        }
                        // Usage
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU Total"))
                        {
                            CPUUsage = sensor.Value.GetValueOrDefault();
                        }
                    }
                }

                // GPU
                if (hardware.HardwareType == HardwareType.GpuAti ||
                    hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        // Temperature
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core"))
                        {
                            GPUTemp = sensor.Value.GetValueOrDefault();
                        }
                        // Core Frequency
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Core"))
                        {
                            GPUCoreFreq = sensor.Value.GetValueOrDefault();
                        }
                        // Memory Frequency
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Memory"))
                        {
                            GPUMemFreq = sensor.Value.GetValueOrDefault();
                        }
                        // Usage
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Memory"))
                        {
                            GPUUsage = sensor.Value.GetValueOrDefault();
                        }
                    }
                }

                // RAM
                if (hardware.HardwareType == HardwareType.RAM)
                {
                    hardware.Update();

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        // Used
                        if (sensor.SensorType == SensorType.Data && sensor.Name.Contains("Used Memory"))
                        {
                            RAMUsed = sensor.Value.GetValueOrDefault();
                        }
                        // Total
                        else if (sensor.SensorType == SensorType.Data && sensor.Name.Contains("Available Memory"))
                        {
                            RAMTotal = sensor.Value.GetValueOrDefault() + RAMUsed;
                            RAMFreePercentage = RAMUsed / RAMTotal * 100;
                        }
                    }
                }
            }
            computer.Close();
        }


        public List<string> GetSerialData()
        {
            // 0 = CPU Temp
            // 1 = GPU Temp
            // 2 = CPU Freq
            // 3 = GPU Core
            // 4 = GPU Mem Freq
            // 5 = CPU Load Percentage
            // 6 = GPU Load Percentage
            // 7 = Used RAM
            // 8 = Total RAM
            // 9 = RAM Percentage
            //10 = Time (00:00)
            //11 = Day&Date (MON27)

            // Clear the list
            serialData.Clear();

            serialData.Add(CPUTemp.ToString(new CultureInfo("en-US")));
            serialData.Add(GPUTemp.ToString(new CultureInfo("en-US")));
            serialData.Add(CPUFreq.ToString(new CultureInfo("en-US")));
            serialData.Add(GPUCoreFreq.ToString(new CultureInfo("en-US")));
            serialData.Add(GPUMemFreq.ToString(new CultureInfo("en-US")));
            serialData.Add(CPUUsage.ToString(new CultureInfo("en-US")));
            serialData.Add(GPUUsage.ToString(new CultureInfo("en-US")));
            serialData.Add(RAMUsed.ToString(new CultureInfo("en-US")));
            serialData.Add(RAMTotal.ToString(new CultureInfo("en-US")));
            serialData.Add(RAMFreePercentage.ToString(new CultureInfo("en-US")));
            serialData.Add(DateTime.Now.ToString("t"));
            serialData.Add($"{DateTime.Now.Date.ToString("ddd").ToUpper()} {DateTime.Now.Date:dd} {DateTime.Now.Date.ToString("MMM").ToUpper()}");

            return serialData;
        }
    }
}
