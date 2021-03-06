﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using InteractionUtil.Util;
using KinectServices.Service.Interface;
using KinectServices.Util;
using Microsoft.Kinect;

namespace InteractionUI.BusinessLogic
{
    public class KinectCamera
    {
        private static readonly int INTERVAL = 100;

        private ISensorService sensorService;
        private ICameraService cameraService;
        private ISkeletonService skeletonService;

        private DispatcherTimer cameraTimer;
        private String lastGesture = "";

        public Image ScreenImage { get; set; }


        public KinectCamera(int sensorIdx)
        {
            initialize(sensorIdx);
        }

        public void Start()
        {
            cameraTimer.Start();
        }

        public void Stop()
        {
            cameraTimer.Stop();
        }

        public void AddGestureTextEvent(KinectInteraction interaction)
        {
            interaction.GestureEvent += new EventHandler(interaction_GestureEvent);
        }

        private void initialize(int sensorIdx)
        {
            cameraService = SpringUtil.getService<ICameraService>();
            sensorService = SpringUtil.getService<ISensorService>();
            skeletonService = SpringUtil.getService<ISkeletonService>();

            sensorService.startSensor(sensorIdx);
            cameraService.enableCamera(sensorService.getSensor(sensorIdx));
            skeletonService.enableSkeleton(sensorService.getSensor(sensorIdx));

            cameraTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);
            cameraTimer.Tick += new EventHandler(cameraTimer_Tick);
            cameraTimer.Interval = TimeSpan.FromMilliseconds(INTERVAL);
        }


        private void interaction_GestureEvent(object sender, EventArgs e)
        {
            lastGesture = ((KinectInteractionArg)e).GestureName;
        }

        private void cameraTimer_Tick(object sender, EventArgs e)
        {
            if (null != ScreenImage)
            {
                byte[] byteArrayIn = cameraService.getImage();

                if (null != byteArrayIn)
                {
                    BitmapSource bitmapSource = BitmapSource.Create(
                        cameraService.getWidth(), cameraService.getHeight(), KinectConsts.DPIX, KinectConsts.DPIX,
                        PixelFormats.Bgr32, null, byteArrayIn, cameraService.getWidth() * cameraService.getBytesPerPixel());

                    RenderTargetBitmap bitmap = new RenderTargetBitmap(cameraService.getWidth(),
                        cameraService.getHeight(), KinectConsts.DPIX, KinectConsts.DPIX, PixelFormats.Default);

                    DrawingVisual drawingVisual = new DrawingVisual();

                    // add draw context in here
                    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                    {
                        // draw camera stream
                        drawingContext.DrawImage(bitmapSource, new Rect(0, 0, bitmap.Width, bitmap.Height));
                        // draw hands
                        drawJoints(drawingContext);

                        if (!skeletonService.userInRange())
                        {
                            drawingContext.PushOpacity(0.2);
                            drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, bitmap.Width, bitmap.Height));
                        }
                    }

                    bitmap.Render(drawingVisual);
                    ScreenImage.Source = bitmap;
                }
            }
        }


        private void drawJoints(DrawingContext drawingContext)
        {
            if (skeletonService.hasJoint(JointType.HandLeft))
            {
                ColorImagePoint point = skeletonService.getColorPointJoint(JointType.HandLeft);
                drawingContext.DrawRectangle(Brushes.Green, null, new Rect(point.X - 10, point.Y - 10, 20, 20));
            }
            if (skeletonService.hasJoint(JointType.HandRight))
            {
                ColorImagePoint point = skeletonService.getColorPointJoint(JointType.HandRight);
                drawingContext.DrawRectangle(Brushes.Green, null, new Rect(point.X - 10, point.Y - 10, 20, 20));
            }

            FormattedText formattedText = new FormattedText(lastGesture, CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight, new Typeface("Verdana"), 32, Brushes.Black);

            drawingContext.DrawText(formattedText, new Point(10, 10));
        }
    }
}
