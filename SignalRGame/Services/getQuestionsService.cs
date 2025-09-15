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
    public async Task<string> GetQuestionsResponseAsync(string token, List<string> subCategories)
    {
        var databaseServerUrl = "http://127.0.0.1:8004/api/questions/";

        // Construct the query string with multiple `subcategory` parameters
        var queryString = string.Join("&", subCategories.Select(sub => $"subcategory={Uri.EscapeDataString(sub)}"));

        // Append the query string to the base URL
        var fullUrl = $"{databaseServerUrl}?{queryString}";
        Console.WriteLine($"Request URL: {fullUrl}"); // Log the request URL for debugging

        // Prepare the request message with GET
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, fullUrl);

        // Set the Authorization header to include the Bearer token
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the GET request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // If the response is successful, read and return the response content (JSON)
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
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


    public async Task<string> GetQuestionsResponseModeTwoAsync(string token)
    {
        var databaseServerUrl = "http://127.0.0.1:8004/api/millionaire/questions/";

        // Construct the query string with multiple `subcategory` parameters

        // Append the query string to the base URL

        // Prepare the request message with GET
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

        // Set the Authorization header to include the Bearer token
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the GET request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // If the response is successful, read and return the response content (JSON)
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
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
