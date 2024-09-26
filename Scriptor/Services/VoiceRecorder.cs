using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Scriptor.Services
{
    public interface IVoiceRecorder {
        bool IsReady { get; }
        Task<bool> StartRecording(AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium);
        Task<string> StopRecording();
    }

    public class VoiceRecorder : IVoiceRecorder, IDisposable
    {
        private MediaCapture _mediaCapture;
        private StorageFolder _storageFolder;
        private string _fileName;

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
                // Handle exceptions
            }
        }

        public bool IsReady { get => _mediaCapture != null && _storageFolder != null; }

        public async Task<bool> StartRecording(AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium)
        {
            try {
                _storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Recordings", CreationCollisionOption.OpenIfExists);
                var file = await _storageFolder.CreateFileAsync($"{Guid.NewGuid().ToString()}.mp3", CreationCollisionOption.GenerateUniqueName);
                _fileName = file.Name;

                await this.InitializeMediaCaptureAsync();
                await _mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(encodingQuality), file);
                return true;
            }
            catch (Exception ex)
            {
                // TODO: Logs/Metrics (write exception message to logs, including userId)
                // Handle exceptions gracefully
                //System.Diagnostics.Debug.WriteLine(ex.Message);
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
                // Handle exceptions gracefully
                System.Diagnostics.Debug.WriteLine(ex.Message);
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
                //await _mediaCapture.StopRecordAsync();
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!disposedValue)
        //    {
        //        if (disposing)
        //        {
        //            _mediaCapture = null;
        //        }

        //        Task.Run(async () =>
        //        {
        //            await _mediaCapture.FinishAsync();
        //            disposedValue = true;
        //        });
        //    }
        //}

        //~VoiceRecorder()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: false);
        //}

        //void IDisposable.Dispose()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
    }
}
