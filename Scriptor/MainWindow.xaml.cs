using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Media.Animation;
using System.Timers;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Scriptor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool isRecording;
        //private Timer _timer;
        //private Random _random;

        public MainWindow()
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 400));
            this.TrySetMicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;

            this.InitializeComponent();

            //BitmapImage bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/recording.gif"));
            //LocalGifImage.Source = bitmapImage;

            //_random = new Random();
            //_timer = new Timer(500); // Update every 500ms
            //_timer.Elapsed += OnTimerElapsed;
            //_timer.Start();
        }
        //private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        //{
        //    DispatcherQueue.TryEnqueue(() =>
        //    {
        //        var points = VoicePolyline.Points.ToList();
        //        for (int i = 0; i < points.Count; i++)
        //        {
        //            points[i] = new Point(points[i].X, _random.Next(50, 150));
        //        }
        //        VoicePolyline.Points.Clear();
        //        foreach (var point in points)
        //        {
        //            VoicePolyline.Points.Add(point);
        //        }
        //    });
        //}

        //private void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!isRecording)
        //    {
        //        ButtonIcon.Glyph = "\uE711"; // Stop sign icon
        //    }
        //    else
        //    {
        //        ButtonIcon.Glyph = "\uE720"; // Microphone icon
        //    }
        //    this.isRecording = !this.isRecording;
        //}

        private bool TrySetMicaBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                Microsoft.UI.Xaml.Media.MicaBackdrop micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                micaBackdrop.Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;
                this.SystemBackdrop = micaBackdrop;

                return true; // Succeeded.
            }

            return false; // Mica is not supported on this system.
        }
    }
}
