using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
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

            _voiceRecorder = App.ServiceProvider.GetRequiredService<IVoiceRecorder>();
            _translator = App.ServiceProvider.GetRequiredService<ITranslator>();
            _logger = App.ServiceProvider.GetRequiredService<ILog>();
            _clerk = App.ServiceProvider.GetRequiredService<IClerk>();

            _animator = App.ServiceProvider.GetRequiredService<IAnimator>();
            _animator.SetupAnimations(this.Compositor, MicrophoneButton, ButtonIcon, RecordingInfoBar, BusyRing, RecordingGifImage, this.Content.XamlRoot);

            _janitor = App.ServiceProvider.GetRequiredService<IJanitor>();
            _janitorThread = new Thread(async () => { 
                await _janitor.CleanOlderFiles();
            });
            _janitorThread.Start();
           
            _subscriptionThread = new Thread(async () => {
                _isRequestingSubscriptionInfo = true;
                    
                var result = await _clerk.IsSubscriptionActive();
                _isSubscriptionActive = result.Success && result.Value;
                
                _isRequestingSubscriptionInfo = false;
            });
            _subscriptionThread.Start();

            this.Closed += MainWindow_Closed;
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

                var recordingResult = await _voiceRecorder.StartRecording();
                if (!recordingResult.Success || !recordingResult.Value)
                {
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
                    var recordingResult = await _voiceRecorder.StopRecording();
                    if (!recordingResult.Success)
                    {
                        _logger.Error($"Error stopping recording. {recordingResult.Message}");
                    }
                    else
                    {
                        _animator.AnimateMakeBusyVisible();
                        var assetsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Recordings");
                        var file = await assetsFolder.GetFileAsync(recordingResult.Value);
                        var translationResult = await _translator.Translate(file.Path);

                        if (translationResult.Success)
                        {
                            if (this.CopyTextToClipboard(translationResult.Value))
                            {
                                _animator.AnimateMakeBusyInvisible(() => DoneTeachingTip.IsOpen = true);

                                RecordingInfoBar.Message = "Press the button and start talking. We'll do the rest.";

                                return;
                            }
                        }
                        else
                        {
                            _animator.AnimateMakeBusyInvisible();
                        }
                    }
                }
                RecordingInfoBar.Message = "Sorry, there was an error. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.Error("Exception stopping recording.", ex);
                _animator.AnimateMakeBusyInvisible();
            }
            finally
            {
                MicrophoneButton.IsEnabled = true;
            }
        }

        private bool CopyTextToClipboard(string textToCopy)
        {
            try
            {
                DataPackage dataPackage = new();
                dataPackage.SetText(textToCopy);
                Clipboard.SetContent(dataPackage);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception copying text to clipboard.", ex);
            }

            return false;
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
            }
            catch (Exception ex)
            {
                this._logger.Error("Exception trying to style the window", ex);
            }

            return false;
        }

        private async void SubscriptionTeachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            if (_purchasingSubscription)
                return;

            try
            {
                _purchasingSubscription = true;
                SubscriptionTeachingTip.IsEnabled = false;
                    
                var purchaseResult = await _clerk.PurchaseLicense();

                if (purchaseResult.Value)
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
