using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Kinect;
using System.IO;


namespace _150306
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {

        private KinectSensor sensor;

        private byte[] colorPixels;

        private ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution640x480Fps30;
        
        private DepthImagePixel[] depthPixels;
        private DepthImagePoint[] depthCoordinates;
        
        private WriteableBitmap colorBitmap;
        
        
        private int x;
        private int y;
        
        private int colorWidth;
        private int colorHight;
        
        private int depthWith;
        private int depthHight;
        


        

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           //sensor = KinectSensor.KinectSensors[0];//最簡單得寫法 但不易偵錯
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorFormat);
                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorWidth = this.sensor.ColorStream.FrameWidth;
                this.colorHight = this.sensor.ColorStream.FrameHeight;
                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(colorWidth, colorHight, 96.0, 96.0, PixelFormats.Bgr32, null);
                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;
                // Turn on the depth stream to receive depth frames
                this.sensor.DepthStream.Enable(DepthFormat);
                this.sensor.DepthStream.Range = DepthRange.Default;
                this.depthWith = this.sensor.DepthStream.FrameWidth;
                this.depthHight = this.sensor.DepthStream.FrameHeight;
                // Allocate space to put the depth pixels we'll receive
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.depthCoordinates = new DepthImagePoint[this.colorWidth * this.colorHight];
                // Add an event handler to be called whenever there is new color frame data
                this.sensor.AllFramesReady += this.sensor_AllFramesReady; ;

                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
            else
            {
                //Lable1.Content = "沒有連接";
                Lable1.Content = Properties.Resources.NoKinectReady;
            }
        }

        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
            bool depthReceived = false;
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)// 防止讀取到其他的數值
                {
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                    depthReceived = true;
                }
                if (depthReceived)
                {
                    //to get the correspong coordinate in depth image for each color pixel
                    this.sensor.CoordinateMapper.MapColorFrameToDepthFrame(ColorFormat,
                                                                           DepthFormat,
                                                                           depthPixels,
                                                                           depthCoordinates);
                    //CXCY 去抓depthCoordinates 記憶體的位置  就可以知道Dx,Dy該位置的深度
                    if ((x >= 0 && x <= colorWidth) && (y >= 0 && y <= colorHight))
                    {      //防止滑鼠讀取超出範圍的數值
                        int colorIndex = x + y * colorWidth;
                        int depthIndex = depthCoordinates[colorIndex].X + depthCoordinates[colorIndex].Y * depthWith;

                        if (depthIndex >= 0 && depthIndex < depthWith * depthHight)
                        {
                            short depth = depthPixels[colorIndex].Depth;
                            Lable2.Content = "(" + x.ToString() + "," + y.ToString() + ")" + depth.ToString();
                        }
                    }
                }
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.sensor != null)
                this.sensor.Stop();
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(Image);
            x = (int)(p.X + 0.5);
            y = (int)(p.Y + 0.5);
            
            //short d = this.depthPixels[idx].Depth;
           // Lable2.Content = "(" + x.ToString() + "," + y.ToString() + ")";// +d.ToString();
        }
    }
}
