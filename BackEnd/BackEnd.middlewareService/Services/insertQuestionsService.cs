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

        public async Task<bool> DeleteQuestionAsync(int questionId, string token)
        {
            var databaseServerUrl = $"http://localhost:8004/api/questions/detail/{questionId}/";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, databaseServerUrl))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                try
                {
                    var databaseResponse = await _httpClient.SendAsync(requestMessage);
                    return databaseResponse.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred while deleting question: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<bool> UpdateQuestionAsync(int questionId, string token, object updateBody)
        {
            var databaseServerUrl = $"http://localhost:8004/api/questions/detail/{questionId}/";
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(updateBody);
            var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), databaseServerUrl);
            requestMessage.Content = content;
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while updating question: {ex.Message}");
                return false;
            }
        }


    }
}
