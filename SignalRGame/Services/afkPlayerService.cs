
using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Text.Json;  // For JsonSerializer
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Net.Http;   // For HttpClient
using System.Threading.Tasks;
using System;


namespace SignalRGame.Services
{
    public class afkPlayerService
    {
        private readonly HttpClient _httpClient;

        public afkPlayerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


    public async Task<bool> sendAfkPlayerToDataBase(string token)
    {
        var databaseServerUrl = "http://localhost:8000/api/core/leave-game-afk/";

        // Prepare the request message with GET
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

        // // Prepare the body content (email in JSON format)
        // var jsonPayload = JsonSerializer.Serialize(new {paypal_payment_id=paypalPaymentId });
        // var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set the Authorization header to include the Bearer token
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Prepare the body content (user_id in JSON format)

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string (if needed for debugging/logging purposes)
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                // Log the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return false; // Return false for non-success status codes
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false; // Return false if an exception occurs
        }
    }




    }
}






