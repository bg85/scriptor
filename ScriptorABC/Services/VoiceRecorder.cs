using log4net;
using ScriptorABC.Models;
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
        Task<Result<bool>> StartRecording(string location, string fodlerName, AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium);
        Task<Result<string>> StopRecording();
    }

    public class VoiceRecorder(ILog logger) : IVoiceRecorder, IDisposable
    {
        private MediaCapture _mediaCapture;
        private StorageFolder _storageFolder;
        private string _fileName;
        private readonly ILog _logger = logger;

        private async Task InitializeMediaCaptureAsync()
        {
            _logger.Info("Initializing media capture.");

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

        public async Task<Result<bool>> StartRecording(string location, string folderName, AudioEncodingQuality encodingQuality = AudioEncodingQuality.Medium)
        {
            _logger.Info("Starting recording.");

            var result = new Result<bool>();
            try
            {
                //_storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Recordings", CreationCollisionOption.OpenIfExists);
                _storageFolder = await (await StorageFolder.GetFolderFromPathAsync(location)).CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
                
                var file = await _storageFolder.CreateFileAsync($"{Guid.NewGuid().ToString()}.mp3", CreationCollisionOption.GenerateUniqueName);
                _fileName = file.Name;

                await InitializeMediaCaptureAsync();
                await _mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(encodingQuality), file);
                
                result.Success = true;
                result.Value = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Value = false;
                result.Message = $"Failed to start recording. Exception: {ex.Message}";
                _logger.Error("Failed to start recording.", ex);
            }

            return result;
        }

        public async Task<Result<string>> StopRecording()
        {
            _logger.Info("Stopping recording.");

            var result = new Result<string>();
            try
            {
                await _mediaCapture.StopRecordAsync();
                result.Success = true;
                result.Value = _fileName;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Failed to stop recording. Exception: {ex.Message}";
                _logger.Error("Failed to stop recording.", ex);
            }

            return result;
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
