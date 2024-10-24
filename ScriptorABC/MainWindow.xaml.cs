using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Polly;
using Polly.Retry;
using ScriptorABC.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ScriptorABC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool isRecording;
        private readonly AsyncRetryPolicy _recordingRetryPolicy;
        private readonly AsyncRetryPolicy _subscriptionRetryPolicy;
        private readonly IVoiceRecorder _voiceRecorder;
        private readonly ITranslator _translator;
        private readonly ILog _logger;
        private readonly IAnimator _animator;
        private readonly IJanitor _janitor;
        private readonly Thread _janitorThread;
        private readonly IClerk _clerk;
        private Thread _subscriptionThread;
        private bool _isSubscriptionActive;
        private bool _isRequestingSubscriptionInfo;
        private bool _purchasingSubscription;

        public MainWindow()
        {
            this.TryStyleWindow();
            this.InitializeComponent();

            RecordingGifImage.Opacity = 0;
            BitmapImage recordingBitmapImage = new(new Uri("ms-appx:///Assets/recording.gif"));
            RecordingGifImage.Source = recordingBitmapImage;

            _recordingRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, retryCount, context) => _logger.Error($"Retry for recording {retryCount} due to {exception.GetType().Name}: {exception.Message}")
            );

            _subscriptionRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                3, 
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, retryCount, context) => _logger.Error($"Retry for subscription {retryCount} due to {exception.GetType().Name}: {exception.Message}")
            );

            _voiceRecorder = App.ServiceProvider.GetRequiredService<IVoiceRecorder>();
            _translator = App.ServiceProvider.GetRequiredService<ITranslator>();
            _logger = App.ServiceProvider.GetRequiredService<ILog>();

            _animator = App.ServiceProvider.GetRequiredService<IAnimator>();
            _animator.SetupAnimations(this.Compositor, MicrophoneButton, ButtonIcon, RecordingInfoBar, BusyRing, RecordingGifImage, this.Content.XamlRoot);

            _janitor = App.ServiceProvider.GetRequiredService<IJanitor>();
            _janitorThread = new Thread(async () => { 
                await _janitor.CleanOlderFiles();
            });
            _janitorThread.Start();

            _clerk = App.ServiceProvider.GetRequiredService<IClerk>();

            this.Closed += MainWindow_Closed;
           
            _subscriptionThread = new Thread(async () => {
                try
                {
                    _isRequestingSubscriptionInfo = true;
                    
                    await _subscriptionRetryPolicy.ExecuteAsync(async () =>
                    {
                        _logger.Info("Validating subscription.");
                        _isSubscriptionActive = await _clerk.IsSubscriptionActive();
                    });
                }
                catch (Exception ex)
                {
                    this._logger.Error($"There was an error validating the subscription info. Error: {ex.Message}", ex);
                }
                finally
                { 
                    _isRequestingSubscriptionInfo = false;
                }
            });
            _subscriptionThread.Start();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _janitorThread?.Join();
            _subscriptionThread.Join();
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
                if (!_isRequestingSubscriptionInfo && !_isSubscriptionActive)
                {
                    await this.StopRecording(true);
                    MicrophoneButton.IsEnabled = false;
                    SubscriptionTeachingTip.IsOpen = true;
                }
                else
                {
                    await this.StopRecording();
                }
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
                    _logger.Error("Recording failed to start.");
                    RecordingInfoBar.Message = "Recording failed to start. Please try again.";
                    await this.StopRecording(true);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception starting recording.", ex);
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
                    var retryResult = false;
                    await _recordingRetryPolicy.ExecuteAsync(async () =>
                    {
                        var recordingName = await _voiceRecorder.StopRecording();
                        if (recordingName == null)
                        {
                            _logger.Error("Error stopping recording. Recordig name cannot be null.");
                            throw new Exception("Error stopping recording.");
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
                                _logger.Error("Error retrieving the file after recording and before translation.", ex);
                                throw;
                            }
                            finally
                            {
                                _animator.AnimateMakeBusyInvisible(() => DoneTeachingTip.IsOpen = true);
                            }

                            RecordingInfoBar.Message = "Press the button and start talking. We'll do the rest.";
                            retryResult = true;
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
                _logger.Error("Exception stopping recording.", ex);
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
            try
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
            catch (Exception ex)
            {
                this._logger.Error("Exception trying to style the window", ex);
            }
        }

        private async void SubscriptionTeachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (_purchasingSubscription)
                return;

            try
            {
                _purchasingSubscription = true;
                SubscriptionTeachingTip.IsEnabled = false;
                var retryResult = false;
                await _subscriptionRetryPolicy.ExecuteAsync(async () =>
                {
                    _logger.Info("Purchasing subscription.");
                    await _clerk.PurchaseLicense();

                    retryResult = true;
                });

                if (retryResult)
                {
                    SubscriptionTeachingTip.IsEnabled = true;
                    _isSubscriptionActive = true;
                    SubscriptionTeachingTip.Title = "Subscription Active";
                    SubscriptionTeachingTip.Subtitle = "Your subscription has been activated. THANK YOU!";
                    SubscriptionTeachingTip.ActionButtonContent = null;
                    MicrophoneButton.IsEnabled = true;
                    this._logger.Info("Subscription purchased.");
                }
                else
                {
                    this.SetSubscriptionPurchaseError();
                }
            }
            catch (Exception ex)
            {
                this.SetSubscriptionPurchaseError(ex);
            }
            finally
            {
                _purchasingSubscription = false;
            }
        }

        private void SetSubscriptionPurchaseError(Exception ex = null)
        {
            _logger.Error($"Error purchasing subscription. {(ex != null ? ex.Message : string.Empty)}");
            SubscriptionTeachingTip.IsEnabled = true;
            _isSubscriptionActive = false;
            SubscriptionTeachingTip.Title = "Error purchasing subscription";
            SubscriptionTeachingTip.Subtitle = "We are unable to purchase a subscription. Please try again later or contact us at scriptorabc@gmail.com";
            SubscriptionTeachingTip.ActionButtonContent = null;
        }

        private void SubscriptionTeachingTip_CloseButtonClick(TeachingTip sender, object args)
        {
            if (_isSubscriptionActive)
            {
                SubscriptionTeachingTip.IsOpen = false;
            }
            else
            {
                this.Close();
            }
        }
    }
}
