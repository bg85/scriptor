using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace ScriptorABC.Services
{
    public interface IAnimator
    {
        void AnimateButtonToTheRight();
        void AnimateButtonToTheLeft();
        void AnimateMakeBitmapVisible();
        void AnimateMakeBitmapInvisible();
        void AnimateMakeBusyVisible();
        void AnimateMakeBusyInvisible(Action doneAction);

        void SetupAnimations(Compositor compositor,
            Button microphoneButton,
            FontIcon buttonIcon,
            InfoBar recordingInfoBar,
            ProgressRing progressRing,
            Image recordingGifImage,
            XamlRoot xamlRoot);
    }

    public class Animator : IAnimator
    {
        private XamlRoot _xamlRoot;
        private Compositor _compositor;
        private DispatcherTimer _recordingAnimationTimer;
        private Button _microphoneButton;
        private Visual _buttonVisual;
        private FontIcon _buttonIcon;
        private InfoBar _recordingInfoBar;
        private ProgressRing _progressRing;
        private Visual _progressRingVisual;
        private Image _recordingGifImage;
        private Visual _recordingBitmapVisual;

        public void AnimateButtonToTheRight()
        {
            var transformMatrix = _buttonVisual.TransformMatrix;
            float originalX = transformMatrix.M31; // Current X position of the button
            float marginLeft = (float)_microphoneButton.Margin.Left; // Margin on the left

            float startX = originalX + marginLeft; // Adjusted starting position considering padding
            float endX = startX + 90; // End position to the right

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
            var transformMatrix = _buttonVisual.TransformMatrix;
            float originalX = transformMatrix.M31; // Current X position of the button
            float marginLeft = (float)_microphoneButton.Margin.Left; // Margin on the left

            float startX = originalX + marginLeft; // Adjusted starting position considering padding
            float endX = startX + 90; // End position to the right

            var buttonToLeftAnimation = _compositor.CreateScalarKeyFrameAnimation();
            buttonToLeftAnimation.InsertKeyFrame(0.0f, endX);    // Start at the moved position
            buttonToLeftAnimation.InsertKeyFrame(1.0f, startX);  // Move back to original position
            buttonToLeftAnimation.Duration = TimeSpan.FromSeconds(1);

            _buttonVisual.StartAnimation("Offset.X", buttonToLeftAnimation);
        }

        public void AnimateMakeBitmapVisible()
        {
            var talkingToVisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            talkingToVisibleAnimation.InsertKeyFrame(0f, 0f); // Start as invisible
            talkingToVisibleAnimation.InsertKeyFrame(1f, 1f); // End as fully visible
            talkingToVisibleAnimation.Duration = TimeSpan.FromSeconds(1);

            _recordingBitmapVisual.StartAnimation("Opacity", talkingToVisibleAnimation);
        }

        public void AnimateMakeBitmapInvisible()
        {
            var talkingToInvisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            talkingToInvisibleAnimation.InsertKeyFrame(0f, 1f); // Start as fully visible
            talkingToInvisibleAnimation.InsertKeyFrame(1f, 0f); // End as invisible
            talkingToInvisibleAnimation.Duration = TimeSpan.FromSeconds(1);

            _recordingBitmapVisual.StartAnimation("Opacity", talkingToInvisibleAnimation);
        }

        public void SetupAnimations(Compositor compositor,
            Button microphoneButton,
            FontIcon buttonIcon,
            InfoBar recordingInfoBar,
            ProgressRing progressRing,
            Image recordingGifImage,
            XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;
            _compositor = compositor;
            _microphoneButton = microphoneButton;
            _buttonIcon = buttonIcon;
            _recordingInfoBar = recordingInfoBar;
            _progressRing = progressRing;
            _recordingGifImage = recordingGifImage;

            _buttonVisual = ElementCompositionPreview.GetElementVisual(_microphoneButton);
            _recordingBitmapVisual = ElementCompositionPreview.GetElementVisual(_recordingGifImage);
            _progressRingVisual = ElementCompositionPreview.GetElementVisual(_progressRing);

            _recordingAnimationTimer = new DispatcherTimer();
            _recordingAnimationTimer.Tick += RecordingAnimationTimer_Tick;
        }

        public void AnimateMakeBusyVisible()
        {
            _microphoneButton.Opacity = 0;
            _recordingInfoBar.Message = "We are translating you recording. It is almost ready.";

            var busyToVisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            busyToVisibleAnimation.InsertKeyFrame(0f, 0f); // Start as invisible
            busyToVisibleAnimation.InsertKeyFrame(1f, 1f); // End as fully visible
            busyToVisibleAnimation.Duration = TimeSpan.FromSeconds(0.5);

            _progressRingVisual.StartAnimation("Opacity", busyToVisibleAnimation);
        }

        public void AnimateMakeBusyInvisible(Action doneAction)
        {
            _microphoneButton.Opacity = 1;
            _buttonIcon.Glyph = "\uE720"; // Microphone icon
            _buttonIcon.Foreground = new SolidColorBrush(Colors.DarkBlue);
            ToolTipService.SetToolTip(_microphoneButton, "Press to start recording!");
            _recordingInfoBar.Message = "Press the button and start talking. We'll do the rest.";
            AnimateButtonToTheLeft();

            var busyToInvisibleAnimation = _compositor.CreateScalarKeyFrameAnimation();
            busyToInvisibleAnimation.InsertKeyFrame(0f, 1f); // Start as fully visible
            busyToInvisibleAnimation.InsertKeyFrame(1f, 0f); // End as invisible
            busyToInvisibleAnimation.Duration = TimeSpan.FromSeconds(1);

            _progressRingVisual.StartAnimation("Opacity", busyToInvisibleAnimation);
            doneAction();
        }

        private void RecordingAnimationTimer_Tick(object sender, object e)
        {
            _recordingAnimationTimer.Stop();

            _microphoneButton.IsEnabled = true;
            _buttonIcon.Glyph = "\uE004"; // Stop icon
            _buttonIcon.Foreground = new SolidColorBrush(Colors.IndianRed);
            ToolTipService.SetToolTip(_microphoneButton, "Press to stop recording!");
            _recordingInfoBar.Message = "We are listening. Press the button to stop recording.";

            AnimateMakeBitmapVisible();
        }
    }
}
