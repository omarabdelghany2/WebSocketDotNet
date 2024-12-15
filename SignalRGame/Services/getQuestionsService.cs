using System.Text.Json;  // For JsonSerializer
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Net.Http;   // For HttpClient
using System.Threading.Tasks;
using System;

namespace SignalRGame.Services
{
    public class getQuestionsService
    {
        private readonly HttpClient _httpClient;

        public getQuestionsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

                    // Function to send token and list of questions
        public async Task<string> GetQuestionsResponseAsync(string token, List<string> categories)
        {
            var databaseServerUrl = "http://192.168.1.64:8000/api/questions/";

            // Prepare the request message with GET
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

            // Set the Authorization header to include the Bearer token
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Prepare the body content (send categories as JSON)
            var jsonPayload = JsonSerializer.Serialize(new { categories = categories });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Set the content of the request (send categories in the body)
            requestMessage.Content = content;

            try
            {
                // Send the GET request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // If the response is successful, read and return the response content (JSON)
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response from database: {responseContent}");
                    return responseContent;
                }
                else
                {
                    // If the response is not successful, capture and print the error response
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occurred during the HTTP request
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }




    }
}
