using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace BackEnd.middlewareService.Services
{
    public class insertQuestionsSerivce
    {
        private readonly HttpClient _httpClient;

        public insertQuestionsSerivce(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<string> insertQuestion(IFormFile file, string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/questions/upload/";

            using (var content = new MultipartFormDataContent())
            using (var stream = file.OpenReadStream())
            {
                // Add file to form-data
                content.Add(new StreamContent(stream), "file", file.FileName);

                // Prepare HTTP request
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl))
                {
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    requestMessage.Content = content;

                    try
                    {
                        // Send the request
                        var databaseResponse = await _httpClient.SendAsync(requestMessage);

                        if (databaseResponse.IsSuccessStatusCode)
                        {
                            return await databaseResponse.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"Error from database: {errorContent}");
                            return "error";
                        }




                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception occurred: {ex.Message}");
                        return $"Exception: {ex.Message}";
                    }
                }
            }
        }





    }
}
