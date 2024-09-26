using System;
using System.Threading.Tasks;
using System.Net.Http;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Threading;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.Services.Organization.Client;
using Windows.ApplicationModel;


namespace Scriptor.Services
{
    public interface ITranslator
    {
        Task<string> Translate(string fileName);
    }

    public class Translator : ITranslator
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string translateApiUrl = "https://scriptor-backend-1093765759278.us-east1.run.app";

        public async Task<string> Translate(string filePath)
        {
            try
            {
                var credPath = await this.GetFilePath("scriptor-436001-263ca8b206ad.txt");
                var credential = GoogleCredential.FromFile(credPath);
                var audience = translateApiUrl;
                var token = await credential.GetOidcTokenAsync(OidcTokenOptions.FromTargetAudience(audience), CancellationToken.None);
                var bt = await token.GetAccessTokenAsync(CancellationToken.None);

                // Create HttpClient to send web request
                using (HttpClient hc = new HttpClient())
                {
                    hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bt);

                    // Prepare the file content
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var content = new MultipartFormDataContent();
                        var fileContent = new StreamContent(fileStream);

                        // Set the content type to audio/mpeg
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

                        // Add the file content to the form data
                        content.Add(fileContent, "file", Path.GetFileName(filePath));

                        // Send the POST request
                        HttpResponseMessage hr = await hc.PostAsync(translateApiUrl + "/upload", content);
                        string responseBody = await hr.Content.ReadAsStringAsync();

                        // Handle the response
                        if (hr.IsSuccessStatusCode)
                        {
                            Console.WriteLine("File uploaded successfully!");
                        }
                        else
                        {
                            Console.WriteLine($"Error uploading file: {responseBody}");
                        }

                        return responseBody;
                    }
                }

                //Create http client to send web request
                //HttpClient hc = new HttpClient();
                //hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bt);
                //HttpResponseMessage hr = await hc.GetAsync(translateApiUrl);
                //string responseBody = await hr.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        private async Task<string> GetFilePath(string fileName)
        {
            //TODO: Add try catch
            var assetsFolder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await assetsFolder.GetFileAsync(fileName);
            return file.Path;
        }
    }
}
