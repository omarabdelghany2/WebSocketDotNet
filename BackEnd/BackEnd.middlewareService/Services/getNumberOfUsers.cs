using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Text.Json;  // For JsonSerializer
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Net.Http;   // For HttpClient
using System.Threading.Tasks;
using System;


namespace BackEnd.middlewareService.Services
{
    public class numberOfUsersFromTokenService
    {
        private readonly HttpClient _httpClient;

        public numberOfUsersFromTokenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


    public async Task<string> GetNumberUsersAsync(string token)
    {
        var databaseServerUrl = "http://localhost:8000/api/admin/dashboard/";

        // Prepare the request message with GET
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

        // Set the Authorization header to include the Bearer token
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Prepare the body content (user_id in JSON format)

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Return the response content (e.g., a list of friends in JSON format)
                return await databaseResponse.Content.ReadAsStringAsync();
            }
            else
            {
                // Capture and print the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return "error";
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return "error";
        }
    }

    }
}