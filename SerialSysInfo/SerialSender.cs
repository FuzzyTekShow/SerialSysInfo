using System;
using System.Collections.Generic;
using System.IO.Ports;

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
                BaudRate = baud
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
        public static void SendData(List<string> data)
        {
            string dataToSend = string.Empty;

            foreach (string metric in data)
            {
                if (data[data.Count - 1] == metric)
                {
                    dataToSend += metric;
                }
                else
                {
                    dataToSend += metric + "|";
                }
            }

            // Send the data
            serialPort.Write($"0x00{dataToSend}0x01");
        }
    }
}
