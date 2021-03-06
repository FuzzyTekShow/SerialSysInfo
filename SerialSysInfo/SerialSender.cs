using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;

namespace SerialSysInfo
{
    public static class SerialSender
    {
        private static SerialPort serialPort;


        /// <summary>
        /// Attempt to connect to the serial port
        /// </summary>
        /// <param name="port">The port to connect to</param>
        /// <param name="baud">The baud rate</param>
        /// <returns>True if success, error message if not</returns>
        public static string Connect(string port, int baud)
        {
            serialPort = new SerialPort
            {
                PortName = port,
                BaudRate = baud,
                WriteTimeout = 1000
            };

            try
            {
                serialPort.Open();
                return serialPort.IsOpen.ToString();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>
        /// Disconnect from the serial
        /// </summary>
        public static void Disconnect()
        {
            if (serialPort != null &&
                serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }


        /// <summary>
        /// Send data to the serial device
        /// </summary>
        /// <param name="data">The data to send</param>
        public static string SendData(List<string> data)
        {
            string dataToSend = string.Empty;
            
            foreach (string metric in data)
            {
                dataToSend += metric + "|";
            }

            // Apply blank on sleep data
            if (Properties.Settings.Default.stopOnSleep)
            {
                dataToSend += "1\n";
            }
            else // Computer is not asleep or blank on sleep is disabled
            {
                dataToSend += "0\n";
            }

            // Send the data
            try
            {
                serialPort.Write(dataToSend);
                return "ok";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
