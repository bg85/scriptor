using System;
using System.Threading.Tasks;
using System.Net.Http;
using log4net;
using OpenAI.Audio;


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
                //var resourceContent = _resourceManager.GetResourceContent("Scriptor.Assets.scriptor-api.txt");
                //AudioClient client = new("whisper-1", resourceContent);

                //AudioTranslationOptions options = new()
                //{
                //    ResponseFormat = AudioTranslationFormat.Verbose,
                //    Prompt = "Medical notes. Doctor Summary. The doctor is speaking."
                //};

                //var translation = await client.TranslateAudioAsync(filePath, options);
                //return translation.Value.Text;
                await Task.Delay(2000);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to translate recording", ex);
                return string.Empty;
            }
        }
    }
}