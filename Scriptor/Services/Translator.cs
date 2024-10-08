using System;
using System.Threading.Tasks;
using System.Net.Http;
using log4net;


namespace Scriptor.Services
{
    public interface ITranslator
    {
        Task<string> Translate(string filePath);
    }

    public class Translator : ITranslator
    {
        private ILog _logger;
        private IResourceManager _resourceManager;
        private static readonly HttpClient httpClient = new HttpClient();
        private const string TranslateApiUrl = "https://scriptor-backend-1093765759278.us-east1.run.app";

        public Translator(ILog logger, IResourceManager resourceManager)
        {
            _logger = logger;
            _resourceManager = resourceManager;
        }

        public async Task<string> Translate(string filePath)
        {
            _logger.Info("Translating recording");
            try
            {
                var resourceContent = _resourceManager.GetResourceContent("Scriptor.Assets.scriptor-api.txt");
                //AudioClient client = new("whisper-1", resourceContent);

                //AudioTranscriptionOptions options = new()
                //{
                //    ResponseFormat = AudioTranscriptionFormat.Verbose,
                //    TimestampGranularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment,
                //};

                //var transcription = await client.TranscribeAudioAsync(filePath, options);
                //return transcription.Value.Text;
                await Task.Delay(1000);
                return "Hello";
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to translate recording", ex);
                return string.Empty;
            }
        }
    }
}