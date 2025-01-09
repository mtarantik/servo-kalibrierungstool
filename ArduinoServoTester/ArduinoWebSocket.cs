using System;
using System.IO;
using System.IO.Ports;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ArduinoServoTester
{
    public class ArduinoWebSocket : WebSocketBehavior
    {
        private SerialPort serialPort;
        private static readonly string logFilePath = "log.txt";

        public ArduinoWebSocket(SerialPort port)
        {
            serialPort = port;
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string data = sp.ReadLine(); // Annahme: Die Daten kommen zeilenweise an

                if (Sessions.Count > 0)
                {
                    Sessions.Broadcast(data);
                }

                LogToFile($"Received from SerialPort: {data}");
            }
            catch (Exception ex)
            {
                LogToFile($"Error: {ex.Message}");
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                string receivedData = e.Data;
                serialPort.WriteLine(receivedData);
                Sessions.Broadcast(receivedData);

                LogToFile($"Received from WebSocket: {receivedData}");
            }
            catch (Exception ex)
            {
                LogToFile($"Error: {ex.Message}");
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging Error: {ex.Message}");
            }
        }
    }
}
