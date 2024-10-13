using System;
using System.Threading.Tasks;
using System.Net.Http;
using log4net;
using OpenAI.Audio;
using Polly;


namespace ScriptorABC.Services
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
            var retryPolicy = Policy.Handle<Exception>()
                     .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            _logger.Info($"Translating recording.");

            var translation = string.Empty;

            var retryResult = await retryPolicy.Execute(async () =>
            {
                try
                {
                    var resourceContent = _resourceManager.GetResourceContent("ScriptorABC.Assets.scriptor-api.txt");
                    AudioClient client = new("whisper-1", resourceContent);

                    AudioTranslationOptions options = new()
                    {
                        ResponseFormat = AudioTranslationFormat.Verbose,
                        Prompt = "The audio is from a doctor explaining why a patient is coming for a consultation. The doctor describes the patient's current symptoms and provides relevant medical history. Translate this narration to english simplifying the language and summarizing. Use medical terms and diagnosis if needed. Focus on conveying the main symptoms, reasons for the consultation, and key points from the medical history. Avoid unnecessary details"
                    };

                    var translationResult = await client.TranslateAudioAsync(filePath, options);
                    translation = translationResult.Value.Text;
                    //await Task.Delay(5000);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unable to translate recording.", ex);
                    throw;
                }
            });

            if (!retryResult)
            {
                _logger.Info($"Translation retries exhausted.");
            }

            return translation;
        }
    }
}