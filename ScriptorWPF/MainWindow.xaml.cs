using System.Windows;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using ScriptorWPF.Services;

namespace ScriptorWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _recordingsLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private const string _recordingsFolderName = "Recordings";
        private bool _isRecording;
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
            InitializeComponent();

            _voiceRecorder = App.ServiceProvider.GetRequiredService<IVoiceRecorder>();
            _translator = App.ServiceProvider.GetRequiredService<ITranslator>();
            _logger = App.ServiceProvider.GetRequiredService<ILog>();
            _clerk = App.ServiceProvider.GetRequiredService<IClerk>();

            _animator = App.ServiceProvider.GetRequiredService<IAnimator>(); // Assuming you have your own implementation for setting up animations
            _animator.SetupAnimations(MicrophoneButton, ButtonIcon, RecordingGif, BusyRing, RecordingInfoBar);

            _janitor = App.ServiceProvider.GetRequiredService<IJanitor>();
            _janitorThread = new Thread(() =>
            {
                _janitor.CleanOlderFiles(System.IO.Path.Combine(_recordingsLocation, _recordingsFolderName));
            });
            _janitorThread.Start();

            _subscriptionThread = new Thread(async () =>
            {
                _isRequestingSubscriptionInfo = true;
                var result = await _clerk.IsSubscriptionActive();
                _isSubscriptionActive = result.Success && result.Value;
                _isRequestingSubscriptionInfo = false;
            });
            _subscriptionThread.Start();
            _isSubscriptionActive = true;

            this.Closed += MainWindow_Closed;
        }
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _janitorThread?.Join();
            _subscriptionThread.Join();

            _logger.Info("App window was closed.");
            LogManager.Shutdown();  // Flush logs and shut down log4net
        }

        private void DoneTeachingTip_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            RecordingInfoBar.Visibility = Visibility.Visible;
            DoneTeachingTip.Visibility = Visibility.Collapsed;
        }

        private async void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                await this.StartRecording();
            }
            else
            {
                if (!_isRequestingSubscriptionInfo && !_isSubscriptionActive)
                {
                    await this.StopRecording(true);
                    MicrophoneButton.IsEnabled = false;
                    SubscriptionTeachingTip.Visibility = Visibility.Visible;
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
                this._isRecording = !this._isRecording;
                MicrophoneButton.IsEnabled = false;
                _animator.AnimateButtonToTheRight();
                var recordingResult = await _voiceRecorder.StartRecording(_recordingsLocation, _recordingsFolderName);
                if (!recordingResult.Success || !recordingResult.Value)
                {
                    RecordingInfoBar.Text = "Recording failed to start. Please try again.";
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
                this._isRecording = !this._isRecording;
                MicrophoneButton.IsEnabled = false;
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
                        var assetsFolder = System.IO.Path.Combine(_recordingsLocation, "Recordings");
                        var file = System.IO.Path.Combine(assetsFolder, recordingResult.Value);
                        var translationResult = await _translator.Translate(file);
                       
                        if (translationResult.Success)
                        {
                            if (this.CopyTextToClipboard(translationResult.Value))
                            {
                                _animator.AnimateMakeBusyInvisible(() => 
                                {
                                    DoneTeachingTip.Visibility = Visibility.Visible;
                                    RecordingInfoBar.Visibility = Visibility.Collapsed;
                                });
                                RecordingInfoBar.Text = "Press the button and start talking. We'll do the rest.";
                                return;
                            }
                        }
                        else
                        {
                            _logger.Warn("There was an error translating.");
                            _animator.AnimateMakeBusyInvisible();
                        }
                    }
                }
                RecordingInfoBar.Text = "Sorry, there was an error. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.Error("Exception stopping recording.", ex);
                _animator.AnimateMakeBusyInvisible();
                RecordingInfoBar.Text = "Sorry, there was an error. Please try again.";
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
                Clipboard.SetText(textToCopy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception copying text to clipboard.", ex);
            }
            return false;
        }

        private async void SubscriptionTeachingTip_ActionButtonClick(object sender, RoutedEventArgs e)
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
                    SubscriptionTitle.Text = "Subscription Active";
                    SubscriptionSubTitle.Text = "Your subscription has been activated. THANK YOU!";
                    //SubscriptionTeachingTip.ActionButtonContent = null;
                    MicrophoneButton.IsEnabled = true;
                    _logger.Info("Subscription purchased.");
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
            SubscriptionTitle.Text = "Error purchasing subscription";
            SubscriptionSubTitle.Text = "We are unable to purchase a subscription. Please try again later or contact us at scriptorabc@gmail.com";
            //SubscriptionTeachingTip.ActionButtonContent = null;
        }

        private void SubscriptionTeachingTip_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            if (_isSubscriptionActive)
            {
                //SubscriptionTeachingTip.IsOpen = false;
            }
            else
            {
                this.Close();
            }
        }
    }
}