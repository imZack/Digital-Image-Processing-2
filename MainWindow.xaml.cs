/**
 * National Taipei Univeristy of Technology
 * Multimedia Information and Technology Integrated Lab
 * Date: 2013/05/25
 * Author: YuLun Shih
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using ImageManipulationExtensionMethods;

using System.ComponentModel;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace ImageProcess
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void EventHandler(object sender, ImageSource e);

        private BackgroundWorker updateCam;
        private Capture webcam = null;
        private bool trackStart = false;
        private System.Drawing.Rectangle selectingWindow;
        private ObjectTracking objTracking = null;
        private bool isSelecting = false;
        private Point startPos;
        private Point endPos;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void init_updateCam()
        {
            webcam = new Capture(0);
            updateCam = new BackgroundWorker();
            updateCam.DoWork += updateCamWorker;
            updateCam.RunWorkerAsync();
            updateCam.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                updateCam.RunWorkerAsync();
            };
        }

        private void updateCamWorker(object sender, DoWorkEventArgs e)
        {
            using (Image<Bgr, Byte> frame = webcam.QueryFrame().Flip(Emgu.CV.CvEnum.FLIP.HORIZONTAL))
            {
                if (trackStart)
                {
                    if (objTracking == null)
                    {
                        objTracking = new ObjectTracking(frame, selectingWindow);
                    }
                    else
                    {
                        System.Drawing.Rectangle result = objTracking.Tracking(frame);

                        frame.Draw(result, new Bgr(0, 255, 0), 3);

                        Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(
                                delegate()
                                {
                                    if (objTracking != null)
                                    {
                                        inputImage.Source = frame.ToBitmapSource();
                                        hueImage.Source = objTracking.hue.ToBitmapSource();
                                        backprojectImage.Source = objTracking.backproject.ToBitmapSource();
                                        maskImage.Source = objTracking.mask.ToBitmapSource();
                                    }
                                }
                                ));
                    }
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(
                            delegate()
                            {
                                inputImage.Source = frame.ToBitmapSource();
                            }
                            ));
                }
            }
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void inputImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isSelecting = true;
            startPos = new Point(e.GetPosition(inputImage).X, e.GetPosition(inputImage).Y);
            Console.WriteLine("DOWN!" + startPos);
        }

        private void inputImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            inputImageCanvas.Children.Clear();
            isSelecting = false;
            endPos = new Point(Math.Min(e.GetPosition(inputImage).X, inputImage.Width),
                                Math.Min(e.GetPosition(inputImage).Y, inputImage.Height));
            Console.WriteLine("UP!" + endPos);

            int xRate = (int)(inputImage.Source.Width / inputImage.Width);
            int yRate = (int)(inputImage.Source.Height / inputImage.Height);
            selectingWindow = new System.Drawing.Rectangle(
                (int)startPos.X * xRate, (int)startPos.Y * yRate,
                (int)(endPos.X - startPos.X) * xRate, (int)(endPos.Y - startPos.Y) * yRate);

            trackStart = true;
            objTracking = null;
        }

        private void inputImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                Point movePos = new Point(e.GetPosition(inputImage).X, e.GetPosition(inputImage).Y);
                inputImageCanvas.Children.Clear();
                Rectangle rectangle = new Rectangle();
                rectangle.SetValue(Canvas.LeftProperty, (double)startPos.X);//Math.Min(startPos.X, endPos.X)
                rectangle.SetValue(Canvas.TopProperty, (double)startPos.Y);//Math.Min(startPos.Y, endPos.Y)
                rectangle.Width = Math.Abs(movePos.X - startPos.X);
                rectangle.Height = Math.Abs(movePos.Y - startPos.Y);
                rectangle.Stroke = new SolidColorBrush() { Color = Colors.Red, Opacity = 0.75f };
                rectangle.StrokeThickness = 3;
                inputImageCanvas.Children.Add(rectangle);
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            init_updateCam();
            startBtn.IsEnabled = false;
        }
    }
}
