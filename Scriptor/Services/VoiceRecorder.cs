using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Scriptor.Services
{
    public interface IVoiceRecorder {
        MediaCaptureFailedEventHandler MediaCapture_Failed { set; }
        RecordLimitationExceededEventHandler MediaCapture_RecordLimitationExceeded { set; }
        bool IsReady { get; }

        Task Initialize();
        Task<bool> StartRecording(AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium);
        Task<bool> StopRecording();
    }

    public class VoiceRecorder : IVoiceRecorder, IDisposable
    {
        private MediaCapture _mediaCapture;
        private StorageFolder _storageFolder;
        private LowLagMediaRecording _mediaRecording;
        private bool disposedValue;

        public MediaCaptureFailedEventHandler MediaCapture_Failed { 
            set { 
                if( _mediaCapture == null )
                    _mediaCapture = new MediaCapture();
                _mediaCapture.Failed += value; 
            } 
        }
        public RecordLimitationExceededEventHandler MediaCapture_RecordLimitationExceeded { 
            set {
                if (_mediaCapture == null)
                    _mediaCapture = new MediaCapture();
                _mediaCapture.RecordLimitationExceeded += value; 
            } 
        }

        public bool IsReady { get => _mediaCapture != null && _storageFolder != null; }

        public async Task Initialize()
        {
            _storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Recordings", CreationCollisionOption.OpenIfExists);
            
            if (_mediaCapture == null)
                _mediaCapture = new MediaCapture();

            await _mediaCapture.InitializeAsync();
        }

        public async Task<bool> StartRecording(AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium)
        {
            try {
                var file = await _storageFolder.CreateFileAsync($"{Guid.NewGuid().ToString()}.mp3", CreationCollisionOption.GenerateUniqueName);
                _mediaRecording = await _mediaCapture.PrepareLowLagRecordToStorageFileAsync(
                        MediaEncodingProfile.CreateMp3(encodingQuality), file);
                await _mediaRecording.StartAsync();

                return true;
            }
            catch (Exception)
            {
                // TODO: Logs/Metrics (write exception message to logs, including userId)
                // Handle exceptions gracefully
                //System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> StopRecording()
        {
            try
            {
                await _mediaRecording.StopAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Handle exceptions gracefully
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _mediaRecording = null;
                }

                Task.Run(async () =>
                {
                    await _mediaRecording.FinishAsync();
                    disposedValue = true;
                });
            }
        }

        ~VoiceRecorder()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
