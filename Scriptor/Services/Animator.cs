using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Scriptor.Services
{
    public interface IAnimator {
        void AnimateButtonToTheRight();
        void AnimateButtonToTheLeft();
        void AnimateMakeBitmapVisible();
        void AnimateMakeBitmapInvisible();
        void SetupAnimations(Compositor compositor,
            Button microphoneButton,
            FontIcon buttonIcon,
            InfoBar recordingInfoBar,
            ProgressRing progressRing,
            Image recordingGifImage);
    }

    public class Animator : IAnimator
    {
        private Compositor _compositor;
        private DispatcherTimer _recordingAnimationTimer;
        private DispatcherTimer _stoppingAnimationTimer;
        private Button _microphoneButton;
        private Visual _buttonVisual;
        private FontIcon _buttonIcon;
        private InfoBar _recordingInfoBar;
        private ProgressRing _progressRing;
        private Image _recordingGifImage;
        private Visual _recordingBitmapVisual;
        private ScalarKeyFrameAnimation _buttonToRightAnimation;

        public void AnimateButtonToTheRight()
        {
            // Get the current position of the button
            var transformMatrix = _buttonVisual.TransformMatrix;
            float originalX = (float)transformMatrix.M31; // Current X position of the button
            float marginLeft = (float)_microphoneButton.Margin.Left; // Margin on the left

            // Define the animation parameters
            float startX = originalX + marginLeft; // Adjusted starting position considering padding
            float endX = startX + 90; // End position to the right

            //Create a scalar animation to move button to the right
            var buttonToRightAnimation = _compositor.CreateScalarKeyFrameAnimation();
            buttonToRightAnimation.InsertKeyFrame(0.0f, startX); // Start at adjusted position
            buttonToRightAnimation.InsertKeyFrame(1.0f, endX);   // Move to the right
            buttonToRightAnimation.Duration = TimeSpan.FromSeconds(1);
            _recordingAnimationTimer.Interval = buttonToRightAnimation.Duration;

            _buttonVisual.StartAnimation("Offset.X", buttonToRightAnimation);
            _recordingAnimationTimer.Start();
        }

        public void AnimateButtonToTheLeft()
        {
            // Get the current position of the button
            var transformMatrix = _buttonVisual.TransformMatrix;
            float originalX = (float)transformMatrix.M31; // Current X position of the button
            float marginLeft = (float)_microphoneButton.Margin.Left; // Margin on the left

            // Define the animation parameters
            float startX = originalX + marginLeft; // Adjusted starting position considering padding
            float endX = startX + 90; // End position to the right

            //Create a scalar animation to move button to the left
            var buttonToLeftAnimation = _compositor.CreateScalarKeyFrameAnimation();
            buttonToLeftAnimation.InsertKeyFrame(0.0f, endX);    // Start at the moved position
            buttonToLeftAnimation.InsertKeyFrame(1.0f, startX);  // Move back to original position
            buttonToLeftAnimation.Duration = TimeSpan.FromSeconds(1);

            _buttonVisual.StartAnimation("Offset.X", buttonToLeftAnimation);
        }

        public void AnimateMakeBitmapVisible()
        {
            // Create a scalar animation for the opacity of the recording bitmap
            var talkingToVisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            talkingToVisibleAnimation.InsertKeyFrame(0f, 0f); // Start as invisible
            talkingToVisibleAnimation.InsertKeyFrame(1f, 1f); // End as fully visible
            talkingToVisibleAnimation.Duration = TimeSpan.FromSeconds(1);

            _recordingBitmapVisual.StartAnimation("Opacity", talkingToVisibleAnimation);
        }

        public void AnimateMakeBitmapInvisible()
        {
            // Create a scalar animation for the opacity of the recording bitmap
            var talkingToInvisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            talkingToInvisibleAnimation.InsertKeyFrame(0f, 1f); // Start as fully visible
            talkingToInvisibleAnimation.InsertKeyFrame(1f, 0f); // End as invisible
            talkingToInvisibleAnimation.Duration = TimeSpan.FromSeconds(1);

            _recordingBitmapVisual.StartAnimation("Opacity", talkingToInvisibleAnimation);
            _stoppingAnimationTimer.Start();
        }

        public void SetupAnimations(Compositor compositor, 
            Button microphoneButton, 
            FontIcon buttonIcon, 
            InfoBar recordingInfoBar,
            ProgressRing progressRing,
            Image recordingGifImage)
        {
            // Get the compositor
            _compositor = compositor;
            _microphoneButton = microphoneButton;
            _buttonIcon = buttonIcon;
            _recordingInfoBar = recordingInfoBar;
            _progressRing = progressRing;
            _recordingGifImage = recordingGifImage;

            _buttonVisual = ElementCompositionPreview.GetElementVisual(_microphoneButton);
            _recordingBitmapVisual = ElementCompositionPreview.GetElementVisual(_recordingGifImage);

            // Initialize the timer
            _recordingAnimationTimer = new DispatcherTimer();
            _recordingAnimationTimer.Tick += RecordingAnimationTimer_Tick;

            // Initialize the timer
            _stoppingAnimationTimer = new DispatcherTimer();
            _stoppingAnimationTimer.Tick += StoppingAnimationTimer_Tick;
        }

        private void RecordingAnimationTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _recordingAnimationTimer.Stop();

            _microphoneButton.IsEnabled = true;
            _buttonIcon.Glyph = "\uE004"; // Stop icon
            _buttonIcon.Foreground = new SolidColorBrush(Colors.IndianRed);
            ToolTipService.SetToolTip(_microphoneButton, "Press to stop recording!");
            _recordingInfoBar.Message = "We are listening. Press the button to stop recording.";

            this.AnimateMakeBitmapVisible();
        }

        private void StoppingAnimationTimer_Tick(object sender, object e)
        {
            // Stop the timer
            _stoppingAnimationTimer.Stop();

            _buttonIcon.Glyph = "\uE720"; // Microphone icon
            _buttonIcon.Foreground = new SolidColorBrush(Colors.DarkBlue);
            ToolTipService.SetToolTip(_microphoneButton, "Press to start recording!");
            _recordingInfoBar.Message = "Translating and copying to your clipboard.";

            this.AnimateButtonToTheLeft();
            _progressRing.Visibility = Visibility.Visible;
        }
    }
}
