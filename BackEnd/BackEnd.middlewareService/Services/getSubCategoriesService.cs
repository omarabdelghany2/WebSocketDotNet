using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace BackEnd.middlewareService.Services
{
    public class getSubCategoriesService
    {
        private readonly HttpClient _httpClient;

        public getSubCategoriesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }





    public async Task<string> GetSubCategoriesAsync(string token, string category)
    {
        // Base URL
        var databaseServerUrl = "http://127.0.0.1:8000/api/categories/subcategories/list/";

        // Trim and encode the category to ensure proper URL formatting
        var trimmedCategory = category.Trim(); // Remove any leading or trailing whitespace/newlines
        var encodedCategory = Uri.EscapeDataString(trimmedCategory);

        // Construct the full URL
        var fullUrl = $"{databaseServerUrl}?categories={encodedCategory}";
        Console.WriteLine($"Request URL: {fullUrl}"); // Optional debugging log

        // Validate token
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Invalid token provided.");
            throw new ArgumentException("Authorization token cannot be null or empty.", nameof(token));
        }

        // Prepare the GET request
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            // Process the response
            if (databaseResponse.IsSuccessStatusCode)
            {
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {responseContent}"); // Log success
                return responseContent;
            }
            else
            {
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from server: {errorContent}"); // Log error
                return errorContent;
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Exception: {ex.Message}");
            throw;
        }
    }

    }
}

