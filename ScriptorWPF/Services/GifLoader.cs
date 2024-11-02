using log4net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ScriptorWPF.Services
{
    public interface IGifLoader
    {
        void Load(string fileName, Image imageControl);
    }

    public class GifLoader(ILog logger) : IGifLoader
    {
        private readonly ILog _logger = logger;

        public void Load(string fileName, Image imageControl)
        {
            try
            {
                var gifUri = new Uri($"pack://application:,,,/Assets/{fileName}");
                var gifBitmapDecoder = new GifBitmapDecoder(gifUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                imageControl.Source = gifBitmapDecoder.Frames[0];

                var animation = new ObjectAnimationUsingKeyFrames
                {
                    Duration = new Duration(TimeSpan.FromMilliseconds(100 * gifBitmapDecoder.Frames.Count)),
                    RepeatBehavior = RepeatBehavior.Forever
                };

                for (int i = 0; i < gifBitmapDecoder.Frames.Count; i++)
                {
                    animation.KeyFrames.Add(new DiscreteObjectKeyFrame(gifBitmapDecoder.Frames[i], KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100 * i))));
                }

                imageControl.BeginAnimation(Image.SourceProperty, animation);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading gif: {fileName}", ex);
            }
        }
    }
}
