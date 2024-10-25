using System;
using System.Threading.Tasks;
using System.Net.Http;
using log4net;
using OpenAI.Audio;
using Polly;
using ScriptorABC.Models;
using Polly.Retry;


namespace ScriptorABC.Services
{
    public interface ITranslator
    {
        Task<Result<string>> Translate(string filePath);
    }

    public class Translator : ITranslator
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string TranslateApiUrl = "https://scriptor-backend-1093765759278.us-east1.run.app";
        private readonly ILog _logger;
        private readonly IResourceManager _resourceManager;
        private readonly AsyncRetryPolicy _retryPolicy;

        public Translator(ILog logger, IResourceManager resourceManager)
        {
            _logger = logger;
            _resourceManager = resourceManager;

            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                   5,
                   retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   onRetry: (exception, retryCount, context) => _logger.Error($"Retry for translator {retryCount} due to {exception.GetType().Name}: {exception.Message}")
               );
        }

        public async Task<Result<string>> Translate(string filePath)
        {
            _logger.Info("Translating recording.");

            var result = new Result<string>();
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var resourceContent = _resourceManager.GetResourceContent("ScriptorABC.Assets.scriptor-api.txt");

                if (resourceContent.Success)
                {
                    AudioClient client = new("whisper-1", resourceContent.Value);

                    AudioTranslationOptions options = new()
                    {
                        ResponseFormat = AudioTranslationFormat.Verbose,
                        Prompt = "The audio is from a doctor explaining why a patient is coming for a consultation. The doctor describes the patient's current symptoms and provides relevant medical history. Translate this narration to english simplifying the language and summarizing. Use medical terms and diagnosis if needed. Focus on conveying the main symptoms, reasons for the consultation, and key points from the medical history. Avoid unnecessary details"
                    };

                    var translationResult = await client.TranslateAudioAsync(filePath, options);
                    result.Value = translationResult.Value.Text;
                    result.Success = true;
                    result.Message = string.Empty;
                }
                else
                {
                    result.Message = "Unable to retrieve resource for translation";
                    throw new Exception(result.Message);
                }
            });

            return result;
        }
    }
}