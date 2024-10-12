using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Media;
using Scriptor.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Polly.Retry;
using Polly;
using Windows.Storage;
using log4net;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Security.Principal;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;

namespace Scriptor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Define constants for setting small and large icons
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(IntPtr hInstance, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        private bool isRecording;
        private readonly RetryPolicy _retryPolicy;
        private readonly IVoiceRecorder _voiceRecorder;
        private readonly ITranslator _translator;
        private readonly ILog _logger;
        private readonly IAnimator _animator;

        public MainWindow()
        {
            this.TryStyleWindow();
            this.InitializeComponent();
            SetWindowIcon("Assets/mic-icon.ico");

            RecordingGifImage.Opacity = 0;
            BitmapImage recordingBitmapImage = new(new Uri("ms-appx:///Assets/recording.gif"));
            RecordingGifImage.Source = recordingBitmapImage;

            _retryPolicy = Policy.Handle<Exception>()
                                 .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            
            _voiceRecorder = App.ServiceProvider.GetRequiredService<IVoiceRecorder>();
            _translator = App.ServiceProvider.GetRequiredService<ITranslator>();
            _logger = App.ServiceProvider.GetRequiredService<ILog>();
            
            _animator = App.ServiceProvider.GetRequiredService<IAnimator>();
            _animator.SetupAnimations(this.Compositor, MicrophoneButton, ButtonIcon, RecordingInfoBar, BusyRing, RecordingGifImage, this.Content.XamlRoot);
        }

        private void SetWindowIcon(string iconPath)
        {
            // Load the small and large icons from the provided .ico file
            IntPtr hIconSmall = LoadImage(IntPtr.Zero, iconPath, 1 /* IMAGE_ICON */, 16, 16, 0x00000010 /* LR_LOADFROMFILE */);
            IntPtr hIconBig = LoadImage(IntPtr.Zero, iconPath, 1 /* IMAGE_ICON */, 32, 32, 0x00000010 /* LR_LOADFROMFILE */);

            // Get the current window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Set the small icon
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hIconSmall);

            // Set the large icon
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, hIconBig);
        }

        private void DoneTeachingTip_CloseButtonClick(TeachingTip sender, object args)
        {
            DoneTeachingTip.IsOpen = false;
        }

        private async void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                await this.StartRecording();
            }
            else
            {
                await this.StopRecording();
            }
        }

        private async Task StartRecording()
        {
            try
            {
                this.isRecording = !this.isRecording;
                MicrophoneButton.IsEnabled = false;
                _animator.AnimateButtonToTheRight();

                var recordingStarted = await _voiceRecorder.StartRecording();
                if (!recordingStarted)
                {
                    _logger.Error($"Recording failed to start for client: {WindowsIdentity.GetCurrent().Name}");
                    RecordingInfoBar.Message = "Recording failed to start. Please try again.";
                    await this.StopRecording(true);
                }
            }
            catch (Exception ex) 
            {
                _logger.Error($"Exception starting recording.for client: {WindowsIdentity.GetCurrent().Name}", ex);
                MicrophoneButton.IsEnabled = true;
            }
        }

        private async Task StopRecording(bool withError = false)
        {
            try
            {
                this.isRecording = !this.isRecording;
                _animator.AnimateMakeBitmapInvisible();

                if (!withError)
                {
                    var retryResult = await _retryPolicy.Execute(async () =>
                    {
                        var recordingName = await _voiceRecorder.StopRecording();
                        if (recordingName == null)
                        {
                            _logger.Error($"Error stopping recording for client: {WindowsIdentity.GetCurrent().Name}");
                            throw new Exception($"Error stopping recording for client: {WindowsIdentity.GetCurrent().Name}");
                        }
                        else
                        {
                            try
                            {
                                _animator.AnimateMakeBusyVisible();
                                var assetsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Recordings");
                                var file = await assetsFolder.GetFileAsync(recordingName);
                                var translation = await _translator.Translate(file.Path);
                                CopyTextToClipboard(translation);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Error retrieving the file after recording and before translation for client: {WindowsIdentity.GetCurrent().Name}", ex);
                                throw;
                            }
                            finally
                            {
                                _animator.AnimateMakeBusyInvisible(() => DoneTeachingTip.IsOpen = true);
                            }

                            MicrophoneButton.IsEnabled = true;
                            RecordingInfoBar.Message = "Press the button and start talking. We'll do the rest.";
                            return true;
                        }
                    });

                    if (!retryResult)
                    {
                        RecordingInfoBar.Message = "Sorry, there was an error. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception stopping recording for client: {WindowsIdentity.GetCurrent().Name}", ex);
            }
            finally
            {
                MicrophoneButton.IsEnabled = true;
            }
        }

        private static void CopyTextToClipboard(string textToCopy)
        {
            DataPackage dataPackage = new();
            dataPackage.SetText(textToCopy);
            Clipboard.SetContent(dataPackage);
        }

        private bool TryStyleWindow()
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(820, 450));
            this.ExtendsContentIntoTitleBar = true;

            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                MicaBackdrop micaBackdrop = new()
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base
                };
                this.SystemBackdrop = micaBackdrop;

                return true;
            }

            return false;
        }
    }
}
