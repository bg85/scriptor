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

        public MainWindow()
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 400));
            this.TrySetMicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;

            this.InitializeComponent();
        }

        private void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                ButtonIcon.Glyph = "\uE711"; // Stop sign icon
            }
            else
            {
                ButtonIcon.Glyph = "\uE720"; // Microphone icon
            }
            this.isRecording = !this.isRecording;
        }

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
