using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using log4net;

namespace ScriptorWPF.Services
{
    public interface IAnimator
    {
        void AnimateButtonToTheRight();
        void AnimateButtonToTheLeft();
        void AnimateMakeBitmapVisible();
        void AnimateMakeBitmapInvisible();
        void AnimateMakeBusyVisible();
        void AnimateMakeBusyInvisible(Action doneAction = null);
        void SetupAnimations(Button microphoneButton, TextBlock micButtonIcon, Image recordingGif, Image busyRing, TextBlock recordingInfoBar);
    }

    public class Animator : IAnimator
    {
        private readonly ILog _logger;
        private DispatcherTimer _recordingAnimationTimer;
        private Button _microphoneButton;
        private TextBlock _micButtonIcon;
        private Image _recordingGif;
        private Image _busyRing;
        private TextBlock _recordingInfoBar;

        public Animator(ILog logger)
        {
            _logger = logger;
        }

        public void SetupAnimations(Button microphoneButton, TextBlock micButtonIcon, Image recordingGifImage, Image busyRing, TextBlock recordingInfoBar)
        {
            _microphoneButton = microphoneButton;
            _recordingGif = recordingGifImage;
            _busyRing = busyRing;
            _micButtonIcon = micButtonIcon;
            _recordingInfoBar = recordingInfoBar;
            _recordingAnimationTimer = new DispatcherTimer();
            _recordingAnimationTimer.Tick += RecordingAnimationTimer_Tick;
        }

        public void AnimateButtonToTheRight()
        {
            try
            {
                var margin = _microphoneButton.Margin;
                var animation = new ThicknessAnimation
                {
                    From = margin,
                    To = new Thickness(195, margin.Top, margin.Right, margin.Bottom),
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                _microphoneButton.BeginAnimation(FrameworkElement.MarginProperty, animation);
                _recordingAnimationTimer.Interval = TimeSpan.FromSeconds(0.5);
                _recordingAnimationTimer.Start();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating button to the right.", ex);
            }
        }

        public void AnimateButtonToTheLeft()
        {
            try
            {
                var margin = _microphoneButton.Margin;
                var animation = new ThicknessAnimation
                {
                    From = margin,
                    To = new Thickness(0, margin.Top, margin.Right, margin.Bottom),
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                _microphoneButton.BeginAnimation(FrameworkElement.MarginProperty, animation);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating button to the left.", ex);
            }
        }

        public void AnimateMakeBitmapVisible()
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                _recordingGif.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating making bitmap visible.", ex);
            }
        }

        public void AnimateMakeBitmapInvisible()
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                _recordingGif.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating making bitmap invisible.", ex);
            }
        }

        public void AnimateMakeBusyVisible()
        {
            try
            {
                _recordingInfoBar.Text = "We are translating you recording. It is almost ready.";
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                _busyRing.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating making busy visible.", ex);
            }
        }

        public void AnimateMakeBusyInvisible(Action doneAction = null)
        {
            try
            {
                _micButtonIcon.Text = "\uE720";
                _micButtonIcon.Foreground = new SolidColorBrush(Colors.DarkBlue);
                ToolTipService.SetToolTip(_microphoneButton, "Press to start recording!");
                _recordingInfoBar.Text = "Press the button and start talking. We'll do the rest.";

                AnimateButtonToTheLeft();

                var animation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                _busyRing.BeginAnimation(UIElement.OpacityProperty, animation);

                if (doneAction != null)
                {
                    animation.Completed += (s, e) => doneAction();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating making busy invisible.", ex);
            }
        }

        private void RecordingAnimationTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _recordingAnimationTimer.Stop();

                _microphoneButton.IsEnabled = true;
                _micButtonIcon.Text = "\uE106";
                _micButtonIcon.Foreground = new SolidColorBrush(Colors.Red);
                ToolTipService.SetToolTip(_microphoneButton, "Press to stop recording!");
                _recordingInfoBar.Text = "We are listening. Press the button to stop recording.";

                AnimateMakeBitmapVisible();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception animating timer tick.", ex);
            }
        }
    }
}