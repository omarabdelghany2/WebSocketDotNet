using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace BackEnd.middlewareService.Services
{
    public class paypalDatabaseServices
    {
        private readonly HttpClient _httpClient;

        public paypalDatabaseServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

    public async Task<bool> billingAsync(string subscriptionId,string amount,string paypalPaymentId)
    {
        var databaseServerUrl = $"http://localhost:8002/api/subscription/{subscriptionId}/payment/";

        // Prepare the request message with PATCH method
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

        // Prepare the body content (email in JSON format)
        var jsonPayload = JsonSerializer.Serialize(new {paypal_payment_id=paypalPaymentId });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set the content of the request
        requestMessage.Content = content;

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

    public async Task<bool> subscribeAsync(string userId,string planId, string startTime,string subscriptionId)
    {
        var databaseServerUrl = "http://localhost:8002/api/subscription/";

        // Prepare the request message with PATCH method
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

        // Prepare the body content (email in JSON format)
        var jsonPayload = JsonSerializer.Serialize(new 
        {
            user_id = int.Parse(userId), 
            plan_id = 1, 
            start_date = startTime, 
            paypal_subscription_id = subscriptionId
        });

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set the content of the request
        requestMessage.Content = content;

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

    public async Task<bool> activateAsync(string subscriptionId)
    {
        var databaseServerUrl = $"http://127.0.0.1:8002/api/subscription/{subscriptionId}/active/";

        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, databaseServerUrl);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> cancelAsync(string subscriptionId)
    {
        var databaseServerUrl = $"http://127.0.0.1:8002/api/subscription/{subscriptionId}/cancel/";

        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, databaseServerUrl);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from server: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> expireAsync(string subscriptionId)
    {
        var databaseServerUrl = $"http://127.0.0.1:8002/api/subscription/{subscriptionId}/expired/";

        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, databaseServerUrl);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from server: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false;
        }
    }


    }
}

