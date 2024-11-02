using System.IO;
using log4net;
using NAudio.Wave;
using ScriptorWPF.Models;

namespace ScriptorWPF.Services
{
    public interface IVoiceRecorder
    {
        bool IsReady { get; }
        Task<Result<bool>> StartRecording(string location, string folderName, int bitRate = 128000);
        Task<Result<string>> StopRecording();
    }

    public class VoiceRecorder(ILog logger) : IVoiceRecorder, IDisposable
    {
        private WaveInEvent _waveIn;
        private WaveFileWriter _writer;
        private string _filePath;
        private readonly ILog _logger = logger;

        public bool IsReady => _waveIn != null && _writer != null;

        public async Task<Result<bool>> StartRecording(string location, string folderName, int bitRate = 128000)
        {
            _logger.Info("Starting recording.");
            var result = new Result<bool>();
            try
            {
                var directoryPath = Path.Combine(location, folderName);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                _filePath = Path.Combine(directoryPath, $"{Guid.NewGuid()}.wav");

                _waveIn = new WaveInEvent();
                _waveIn.WaveFormat = new WaveFormat(44100, 1);
                _writer = new WaveFileWriter(_filePath, _waveIn.WaveFormat);

                _waveIn.DataAvailable += (sender, e) =>
                {
                    _writer.Write(e.Buffer, 0, e.BytesRecorded);
                };

                _waveIn.RecordingStopped += (sender, e) =>
                {
                    _writer?.Dispose();
                    _writer = null;
                    _waveIn?.Dispose();
                    _waveIn = null;
                };

                _waveIn.StartRecording();
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

            return await Task.FromResult(result);
        }

        public async Task<Result<string>> StopRecording()
        {
            _logger.Info("Stopping recording.");
            var result = new Result<string>();
            try
            {
                _waveIn?.StopRecording();
                result.Success = true;
                result.Value = _filePath;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Failed to stop recording. Exception: {ex.Message}";
                _logger.Error("Failed to stop recording.", ex);
            }

            return await Task.FromResult(result);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer?.Dispose();
                _waveIn?.Dispose();
            }
        }
    }
}
