using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Windows.Forms;
using WebSocketSharp.Server;

namespace ArduinoServoTester
{
    public partial class ServoTesterGui : Form
    {
        private SerialPort sp;
        private HttpListener _listener;
        private WebSocketServer webSocketServer;
        private static readonly string logFilePath = "log.txt";

        public ServoTesterGui()
        {
            InitializeComponent();
            StartWebServer();
            InitializeWebSocket();
        }

        private void StartWebServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://192.168.178.78:8080/");
            _listener.Start();
            _listener.BeginGetContext(OnRequestReceived, null);
        }

        private void OnRequestReceived(IAsyncResult result)
        {
            var context = _listener.EndGetContext(result);
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET")
            {
                // Serve the HTML file
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html");
                var responseString = File.ReadAllText(filePath);
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }

            _listener.BeginGetContext(OnRequestReceived, null);
        }

        private void InitializeWebSocket()
        {
            webSocketServer = new WebSocketServer(IPAddress.Parse("192.168.178.78"), 8081);
            webSocketServer.AddWebSocketService("/", () => new ArduinoWebSocket(sp));
            webSocketServer.Start();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            label1.Text = trackBar1.Value.ToString();
            SendServoCommand(textBox1.Text, trackBar1.Value);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendServoPowerCommand(textBox1.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            sp = new SerialPort(textBox2.Text, 115200);
            sp.ReadBufferSize = 8000;
            sp.WriteBufferSize = 4096;
            sp.Open();
        }

        private void SendServoCommand(string servoNumber, int value)
        {
            if (sp != null && sp.IsOpen)
            {
                try
                {
                    sp.WriteLine($"setServo({servoNumber}, {value})");
                    LogToFile($"Sent to SerialPort: setServo({servoNumber}, {value})");
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }
        }

        private void SendServoPowerCommand(string servoNumber)
        {
            if (sp != null && sp.IsOpen)
            {
                try
                {
                    sp.WriteLine($"setServoPower({servoNumber})");
                    LogToFile($"Sent to SerialPort: setServoPower({servoNumber})");
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }
        }

        private void LogError(Exception ex)
        {
            LogToFile($"Error: {ex.Message}");
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (webSocketServer != null && webSocketServer.IsListening)
            {
                webSocketServer.Stop();
            }
            if (sp != null && sp.IsOpen)
            {
                sp.Close();
            }
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
            }
            base.OnFormClosed(e);
        }
    }
}
