using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Scriptor.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.Media.Capture;
using System.Threading.Tasks;
using Polly.Retry;
using Polly;

namespace Scriptor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool isRecording;
        private Compositor _compositor;
        private Visual _buttonVisual;
        private Visual _recordingBitmapVisual;
        private DispatcherTimer _recordingAnimationTimer;
        private DispatcherTimer _stoppingAnimationTimer;
        //private DispatcherTimer _teachingTipTimer;
        private ScalarKeyFrameAnimation _buttonToRightAnimation;
        private ScalarKeyFrameAnimation _buttonToLeftAnimation;
        private ScalarKeyFrameAnimation _talkingToVisibleAnimation;
        private ScalarKeyFrameAnimation _talkingToInvisibleAnimation;
        private readonly RetryPolicy _retryPolicy;

        private readonly IVoiceRecorder _voiceRecorder;

        public MainWindow()
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(820, 450));
            this.TrySetMicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;

            this.InitializeComponent();

            this.SetupAnimations();
            //this.SetupRecordTeachingTip();

            _retryPolicy = Policy.Handle<Exception>()
                                 .WaitAndRetry(50, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            _voiceRecorder = App.ServiceProvider.GetRequiredService<IVoiceRecorder>();
            _voiceRecorder.MediaCapture_Failed = RecordingFailed;
            _voiceRecorder.MediaCapture_RecordLimitationExceeded = RecordingLimitationExceeded;
            Task.Run(async () =>
            {
                await _voiceRecorder.Initialize();
            });
            //var result = _retryPolicy.Execute(() =>
            //{
            //    if (!_voiceRecorder.IsReady)
            //    {
            //        throw new Exception("Storage Folder not created yet");
            //    }
            //    return true;
            //});
        }

        private void RecordingFailed(MediaCapture sender, MediaCaptureFailedEventArgs failedArgs)
        {
            //TODO Analytics, update UI
        }

        private void RecordingLimitationExceeded(MediaCapture sender)
        {
            //TODO Analytics, update UI
        }

        private void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                this.StartRecording();
            }
            else
            {
                this.StopRecording();
            }
        }

        private void StartRecording()
        {
            this.isRecording = !this.isRecording;
            MicrophoneButton.IsEnabled = false;
            _buttonVisual.StartAnimation("Offset.X", _buttonToRightAnimation);
            _recordingAnimationTimer.Start();

            Task.Run(async () =>
            {
                var recordingStarted = await _voiceRecorder.StartRecording();
                if (!recordingStarted)
                {
                    // TODO: Logs/Metrics (write exception message to logs, including userId)
                    // retry
                    RecordingInfoBar.Message = "Recording failed to start. Please try again.";
                    this.StopRecording(true);
                }
            });
        }

        private void StopRecording(bool withError = false)
        {
            this.isRecording = !this.isRecording;
            MicrophoneButton.IsEnabled = false;
            _recordingBitmapVisual.StartAnimation("Opacity", _talkingToInvisibleAnimation);
            _stoppingAnimationTimer.Start();

            if (!withError)
            {
                Task.Run(async () =>
                {
                    var recordingStopped = await _voiceRecorder.StopRecording();
                    if (!recordingStopped)
                    {
                        // TODO: Logs/Metrics (write exception message to logs, including userId)
                        //retry
                        //dispose class and recreate it 
                    }
                });
            }
        }
        private void RecordingAnimationTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _recordingAnimationTimer.Stop();

            MicrophoneButton.IsEnabled = true;
            ButtonIcon.Glyph = "\uE004"; // Stop icon
            ButtonIcon.Foreground = new SolidColorBrush(Colors.IndianRed);
            ToolTipService.SetToolTip(MicrophoneButton, "Press to stop recording!");
            RecordingInfoBar.Message = "We are listening. Press the button to stop recording.";

            _recordingBitmapVisual.StartAnimation("Opacity", _talkingToVisibleAnimation);
        }

        private void StoppingAnimationTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _stoppingAnimationTimer.Stop();

            MicrophoneButton.IsEnabled = true;
            ButtonIcon.Glyph = "\uE720"; // Microphone icon
            ButtonIcon.Foreground = new SolidColorBrush(Colors.DarkBlue);
            ToolTipService.SetToolTip(MicrophoneButton, "Press to start recording!");
            RecordingInfoBar.Message = "Press the button and start talking. We'll do the rest.";

            _buttonVisual.StartAnimation("Offset.X", _buttonToLeftAnimation);
        }

        private void SetupAnimations()
        {
            // Get the compositor
            _compositor = this.Compositor;
            _buttonVisual = ElementCompositionPreview.GetElementVisual(MicrophoneButton);
            _recordingBitmapVisual = ElementCompositionPreview.GetElementVisual(RecordingGifImage);

            // Initialize the timers
            _recordingAnimationTimer = new DispatcherTimer();
            _recordingAnimationTimer.Tick += RecordingAnimationTimer_Tick;

            // Initialize the timer
            _stoppingAnimationTimer = new DispatcherTimer();
            _stoppingAnimationTimer.Tick += StoppingAnimationTimer_Tick;

            //RecordingGifImage.Visibility = Visibility.Collapsed;
            RecordingGifImage.Opacity = 0;
            BitmapImage recordingBitmapImage = new(new Uri("ms-appx:///Assets/recording.gif"));
            RecordingGifImage.Source = recordingBitmapImage;

            // Get the current position of the button
            var transformMatrix = _buttonVisual.TransformMatrix;
            float originalX = (float)transformMatrix.M31; // Current X position of the button
            float marginLeft = (float)MicrophoneButton.Margin.Left; // Margin on the left

            // Define the animation parameters
            float startX = originalX + marginLeft; // Adjusted starting position considering padding
            float endX = startX + 90; // End position to the right

            //Create a scalar animation to move button to the right
            _buttonToRightAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _buttonToRightAnimation.InsertKeyFrame(0.0f, startX); // Start at adjusted position
            _buttonToRightAnimation.InsertKeyFrame(1.0f, endX);   // Move to the right
            _buttonToRightAnimation.Duration = TimeSpan.FromSeconds(1);
            _recordingAnimationTimer.Interval = _buttonToRightAnimation.Duration;

            //Create a scalar animation to move button to the left
            _buttonToLeftAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _buttonToLeftAnimation.InsertKeyFrame(0.0f, endX);    // Start at the moved position
            _buttonToLeftAnimation.InsertKeyFrame(1.0f, startX);  // Move back to original position
            _buttonToLeftAnimation.Duration = TimeSpan.FromSeconds(1);

            // Create a scalar animation for the opacity of the recording bitmap
            _talkingToVisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _talkingToVisibleAnimation.InsertKeyFrame(0f, 0f); // Start as invisible
            _talkingToVisibleAnimation.InsertKeyFrame(1f, 1f); // End as fully visible
            _talkingToVisibleAnimation.Duration = TimeSpan.FromSeconds(1);

            // Create a scalar animation for the opacity of the recording bitmap
            _talkingToInvisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _talkingToInvisibleAnimation.InsertKeyFrame(0f, 1f); // Start as invisible
            _talkingToInvisibleAnimation.InsertKeyFrame(1f, 0f); // End as fully visible
            _talkingToInvisibleAnimation.Duration = TimeSpan.FromSeconds(1);
        }

        //private void SetupRecordTeachingTip()
        //{
        //    RecordTeachingTip.IsOpen = true;
            
        //    // Initialize the timer
        //    _teachingTipTimer = new DispatcherTimer();
        //    _teachingTipTimer.Tick += TeachingTipTimer_Tick;
        //    _teachingTipTimer.Interval = TimeSpan.FromSeconds(5);
        //    _teachingTipTimer.Start();
        //}

        //private void TeachingTipTimer_Tick(object sender, object e)
        //{
        //    // Stop the timer
        //    _teachingTipTimer.Stop();
        //    RecordTeachingTip.IsOpen = false;
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
