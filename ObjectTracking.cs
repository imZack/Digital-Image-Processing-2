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
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace ImageProcess
{
    class ObjectTracking
    {
        public Image<Hsv, Byte> hsv;
        public Image<Gray, Byte> hue;
        public Image<Gray, Byte> mask;
        public Image<Gray, Byte> backproject;
        public DenseHistogram hist;
        private Rectangle trackingWindow;
        private MCvConnectedComp trackcomp;
        private MCvBox2D trackbox;

        public ObjectTracking(Image<Bgr, Byte> image, Rectangle ROI)
        {
            // Initialize parameters
            trackbox = new MCvBox2D();
            trackcomp = new MCvConnectedComp();
            hue = new Image<Gray, byte>(image.Width, image.Height);
            hue._EqualizeHist();
            mask = new Image<Gray, byte>(image.Width, image.Height);
            hist = new DenseHistogram(30, new RangeF(0, 180));
            backproject = new Image<Gray, byte>(image.Width, image.Height);

            // Assign Object's ROI from source image.
            trackingWindow = ROI;

            // Producing Object's hist
            CalObjectHist(image);
        }

        public Rectangle Tracking(Image<Bgr, Byte> image)
        {
            UpdateHue(image);

            // Calucate BackProject
            backproject = hist.BackProject(new Image<Gray, Byte>[] { hue });

            // Apply mask
            backproject._And(mask);

            // Tracking windows empty means camshift lost bounding-box last time
            // here we give camshift a new start window from 0,0 (you could change it)
            if (trackingWindow.IsEmpty || trackingWindow.Width==0 || trackingWindow.Height==0)
            {
                trackingWindow = new Rectangle(0, 0, 100, 100);
            }
            CvInvoke.cvCamShift(backproject, trackingWindow,
                new MCvTermCriteria(10, 1), out trackcomp, out trackbox);

            // update tracking window
            trackingWindow = trackcomp.rect;

            return trackingWindow;
        }

        private void CalObjectHist(Image<Bgr, Byte> image)
        {
            UpdateHue(image);

            // Set tracking object's ROI
            hue.ROI = trackingWindow;
            mask.ROI = trackingWindow;
            hist.Calculate(new Image<Gray, Byte>[] { hue }, false, mask);

            // Scale Historgram
            float max=0, min=0, scale=0;
            int[] minLocations, maxLocations;
            hist.MinMax(out min, out max, out minLocations, out maxLocations);
            if (max != 0)
            {
                scale = 255 / max;
            }
            CvInvoke.cvConvertScale(hist.MCvHistogram.bins, hist.MCvHistogram.bins, scale, 0);

            // Clear ROI
            hue.ROI = System.Drawing.Rectangle.Empty;
            mask.ROI = System.Drawing.Rectangle.Empty;

            // Now we have Object's Histogram, called hist.
        }

        private void UpdateHue(Image<Bgr, Byte> image)
        {
            // release previous image memory
            if (hsv != null)    hsv.Dispose();
            hsv = image.Convert<Hsv, Byte>();

            // Drop low saturation pixels
            mask = hsv.Split()[1].ThresholdBinary(new Gray(60), new Gray(255));
            CvInvoke.cvInRangeS(hsv, new MCvScalar(0, 30, Math.Min(10, 255), 0),
                new MCvScalar(180, 256, Math.Max(10, 255), 0), mask);

            // Get Hue
            hue = hsv.Split()[0];
        }


    }
}
