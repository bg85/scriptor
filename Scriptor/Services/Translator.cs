using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Windows.Storage;
using OpenAI.Audio;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Reflection;


namespace Scriptor.Services
{
    public interface ITranslator
    {
        Task<string> Translate(string filePath);
    }

    public class Translator : ITranslator
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string translateApiUrl = "https://scriptor-backend-1093765759278.us-east1.run.app";

        private async Task<string> GetFileContent()
        {
            try
            {

                //var localFolder = ApplicationData.Current.LocalFolder;
                //var file = await localFolder.GetFileAsync("scriptor-api.txt");
                //var fileContent = await FileIO.ReadTextAsync(file);

                //return fileContent;

                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Scriptor.Assets.scriptor-api.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string content = reader.ReadToEnd();
                            return content;
                        }
                    }
                    else
                    {
                        return "Resource not found.";
                    }
                }
            }
            catch (Exception)
            {
                //TODO: Error handling
                throw;
            }
        }

        public async Task<string> Translate(string filePath)
        {
            try
            {
                //AudioClient client = new("whisper-1", await this.GetFileContent());

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
                //TODO: handle exception
                //throw;
                return string.Empty;
            }
        }
    }
}