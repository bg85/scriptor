using log4net;
using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace ScriptorABC.Services
{
    public interface IVoiceRecorder
    {
        bool IsReady { get; }
        Task<bool> StartRecording(AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium);
        Task<string> StopRecording();
    }

    public class VoiceRecorder : IVoiceRecorder, IDisposable
    {
        private MediaCapture _mediaCapture;
        private StorageFolder _storageFolder;
        private string _fileName;
        private ILog _logger;

        public VoiceRecorder(ILog logger)
        {
            _logger = logger;
        }

        private async Task InitializeMediaCaptureAsync()
        {
            _mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio
            };

            try
            {
                await _mediaCapture.InitializeAsync(settings);
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to Initialize media capture object.", ex);
            }
        }

        public bool IsReady { get => _mediaCapture != null && _storageFolder != null; }

        public async Task<bool> StartRecording(AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium)
        {
            try
            {
                _storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Recordings", CreationCollisionOption.OpenIfExists);
                var file = await _storageFolder.CreateFileAsync($"{Guid.NewGuid().ToString()}.mp3", CreationCollisionOption.GenerateUniqueName);
                _fileName = file.Name;

                await InitializeMediaCaptureAsync();
                await _mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(encodingQuality), file);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start recording.", ex);
                return false;
            }
        }

        public async Task<string> StopRecording()
        {
            try
            {
                await _mediaCapture.StopRecordAsync();
                return _fileName;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop recording.", ex);
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }
    }
}
