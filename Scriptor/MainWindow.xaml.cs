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

namespace Scriptor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
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

            RecordingGifImage.Opacity = 0;
            BitmapImage recordingBitmapImage = new(new Uri("ms-appx:///Assets/recording.gif"));
            RecordingGifImage.Source = recordingBitmapImage;

            _retryPolicy = Policy.Handle<Exception>()
                                 .WaitAndRetry(50, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            
            _voiceRecorder = App.ServiceProvider.GetRequiredService<IVoiceRecorder>();
            _translator = App.ServiceProvider.GetRequiredService<ITranslator>();
            _logger = App.ServiceProvider.GetRequiredService<ILog>();
            
            _animator = App.ServiceProvider.GetRequiredService<IAnimator>();
            _animator.SetupAnimations(this.Compositor, MicrophoneButton, ButtonIcon, RecordingInfoBar, BusyRing, RecordingGifImage);
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
                    _logger.Error($"Recording failed to start for client: {Windows.System.Profile.SystemIdentification.GetSystemIdForPublisher()}");
                    RecordingInfoBar.Message = "Recording failed to start. Please try again.";
                    await this.StopRecording(true);
                }
            }
            catch (Exception ex) 
            {
                _logger.Error("Exception starting recording.", ex);
            }
        }

        private async Task StopRecording(bool withError = false)
        {
            try
            {
                this.isRecording = !this.isRecording;
                MicrophoneButton.IsEnabled = false;
                _animator.AnimateMakeBitmapInvisible();

                if (!withError)
                {
                    await _retryPolicy.Execute(async () =>
                    {
                        var recordingName = await _voiceRecorder.StopRecording();
                        if (recordingName == null)
                        {
                            _logger.Error("Error stopping recording.");
                            throw new Exception("Error stopping recording.");
                        }
                        else
                        {
                            try
                            {
                                var assetsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Recordings");
                                var file = await assetsFolder.GetFileAsync(recordingName);
                                var translation = await _translator.Translate(file.Path);
                                this.CopyTextToClipboard(translation);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error("Error retrieving the file after recording and before translation", ex);
                            }

                            MicrophoneButton.IsEnabled = true;
                            BusyRing.Visibility = Visibility.Collapsed;
                            RecordingInfoBar.Message = "The text has been copied to your clipboard.";
                            await Task.Delay(5000);
                            RecordingInfoBar.Message = "Press the button and start talking. We'll do the rest.";
                            return true;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception stopping recording.", ex);
            }
        }

        private void CopyTextToClipboard(string textToCopy)
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

                return true; // Succeeded.
            }

            return false; // Mica is not supported on this system.
        }
    }
}
