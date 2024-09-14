using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;

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
        private DispatcherTimer _teachingTipTimer;
        private ScalarKeyFrameAnimation _buttonToRightAnimation;
        private ScalarKeyFrameAnimation _buttonToLeftAnimation;
        private ScalarKeyFrameAnimation _talkingToVisibleAnimation;
        private ScalarKeyFrameAnimation _talkingToInvisibleAnimation;

        public MainWindow()
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 400));
            this.TrySetMicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;

            this.InitializeComponent();

            this.SetupAnimations();
            this.SetupRecordTeachingTip();
        }

        private void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                MicrophoneButton.IsEnabled = false;
                _buttonVisual.StartAnimation("Offset.X", _buttonToRightAnimation);
                _recordingAnimationTimer.Start();
            }
            else
            {
                MicrophoneButton.IsEnabled = false;
                _recordingBitmapVisual.StartAnimation("Opacity", _talkingToInvisibleAnimation);
                _stoppingAnimationTimer.Start();
            }

            this.isRecording = !this.isRecording;
        }

        private void RecordingAnimationTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _recordingAnimationTimer.Stop();

            MicrophoneButton.IsEnabled = true;
            ButtonIcon.Glyph = "\uE004"; // Stop icon

            _recordingBitmapVisual.StartAnimation("Opacity", _talkingToVisibleAnimation);
        }

        private void StoppingAnimationTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _stoppingAnimationTimer.Stop();

            MicrophoneButton.IsEnabled = true;
            ButtonIcon.Glyph = "\uE720"; // Stop icon

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
            float endX = startX + 100; // End position to the right

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

        private void SetupRecordTeachingTip()
        {
            RecordTeachingTip.IsOpen = true;
            
            // Initialize the timer
            _teachingTipTimer = new DispatcherTimer();
            _teachingTipTimer.Tick += TeachingTipTimer_Tick;
            _teachingTipTimer.Interval = TimeSpan.FromSeconds(5);
            _teachingTipTimer.Start();
        }

        private void TeachingTipTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _teachingTipTimer.Stop();
            RecordTeachingTip.IsOpen = false;
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
