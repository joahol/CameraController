using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.ComponentModel;

namespace CameraController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const string serverAddress = "192.168.1.53";
        private const string serverPort = "80";
        private int TILT = 90;
        private int PAN = 90;
        private BackgroundWorker backgroundWorker;
        private VideoCapture vidCapture;
        

        static ClientWebSocket webSock = null;
        public MainWindow()
        {
            InitializeComponent();
            vidCapture = new VideoCapture();
            backgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            backgroundWorker.DoWork += backgroundVideoProcess;
            RunPanTiltClient();
            Loaded += onLoaded;
            Closing += onClose;
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            backgroundWorker.CancelAsync();
            vidCapture.Dispose();
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            vidCapture.Open(0, VideoCaptureAPIs.ANY);
            if (!vidCapture.IsOpened()) {
                Close();
                return;
            }
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundVideoProcess(object sender, DoWorkEventArgs e)

        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending) {
                using (var frameMat = vidCapture.RetrieveMat())
                {
                    Dispatcher.Invoke(() =>
                    {
                        ImgVid.Source = frameMat.ToWriteableBitmap();
                    }); 
                }
                Thread.Sleep(30);
            }
        }

        public static async Task RunPanTiltClient()
        {
            webSock = new ClientWebSocket();
            string url = "ws://" + serverAddress + ":" + serverPort;
            await webSock.ConnectAsync(new Uri(url), System.Threading.CancellationToken.None);
        }


        private async void Tilt()
        {
            if (webSock != null)
            {
                try
                {
                    byte[] temp = Encoding.ASCII.GetBytes("T");
                    byte[] temp2 = BitConverter.GetBytes(TILT);
                    byte[] tra = new byte[2];
                    tra[0] = temp[0];
                    tra[1] = temp2[0];
                    await webSock.SendAsync(new ArraySegment<byte>(tra), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (System.IO.IOException ioe) { 
                    Console.WriteLine(ioe.Message); }
            }
        }

        private async void Pan()
        {
            if (webSock != null)
            {
                try
                {
                    byte[] temp = Encoding.ASCII.GetBytes("P");
                    byte[] temp2 = BitConverter.GetBytes(PAN);
                    byte[] tra = new byte[2];
                    tra[0] = temp[0];
                    tra[1] = temp2[0];
                    await webSock.SendAsync(new ArraySegment<byte>(tra), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (System.IO.IOException ie) { 
                    Console.WriteLine(ie.Message); }
            }
            }

        private void sliderTILT_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TILT = (int)sliderTILT.Value;
            lblTilt.Content="Tilt:" + TILT;
            Tilt();
        }

        private void sliderPAN_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PAN = (int)sliderPAN.Value;
            lblPan.Content = "Pan:" + PAN;
            Pan();
        }
    }
}
