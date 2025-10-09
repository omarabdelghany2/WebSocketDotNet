using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace BackEnd.middlewareService.Services
{
    public class ParagraphUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://104.248.35.179:8004/api";

        public ParagraphUploadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> UploadParagraphWithQuestionsAsync(string token, IFormFile csvFile, string paragraphText)
        {
            try
            {
                var requestContent = new MultipartFormDataContent();

                // Add the CSV file
                var fileContent = new StreamContent(csvFile.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                requestContent.Add(fileContent, "file", csvFile.FileName);

                // Add the paragraph text
                requestContent.Add(new StringContent(paragraphText), "paragraph_text");

                // Create request message
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/paragraph/upload-questions/");
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                requestMessage.Content = requestContent;

                // Send request
                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error uploading paragraph: {responseContent}");
                    throw new HttpRequestException($"Failed to upload paragraph. Status code: {response.StatusCode}");
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UploadParagraphWithQuestionsAsync: {ex.Message}");
                throw;
            }
        }
    }
}