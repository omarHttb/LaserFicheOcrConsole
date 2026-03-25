using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using LaserFicheOcrConsole.models;

namespace LaserFicheOcrConsole
{
    public class OcrClient
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<ApiResponse> ExtractText(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found.", filePath);

                client.BaseAddress = new Uri("http://76.13.13.84:8008");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                using (var content = new MultipartFormDataContent())
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var fileContent = new StreamContent(fileStream);

                       
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        
                        string fileName = Path.GetFileName(filePath);

                        content.Add(fileContent, "file", fileName);

                        HttpResponseMessage response = await client.PostAsync("api/v1/extract-text", content);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                            return result ?? new ApiResponse();
                        }
                        else
                        {
                            string errorDetails = await response.Content.ReadAsStringAsync();
                            throw new Exception($"Error: {response.StatusCode} - {errorDetails}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new ApiResponse();
            }
        }
    }
}
