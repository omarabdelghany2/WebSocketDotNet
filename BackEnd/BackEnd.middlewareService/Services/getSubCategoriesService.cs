using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Text.Json.Serialization;

namespace BackEnd.middlewareService.Services
{
    public class CategoryDetails
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("parent")]
        public object? Parent { get; set; }

        [JsonPropertyName("subcategories")]
        public List<SubCategory> Subcategories { get; set; } = new List<SubCategory>();
    }

    public class SubCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("results")]
        public List<CategoryDetails> Results { get; set; } = new List<CategoryDetails>();
    }
    public class getSubCategoriesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:8004/api";

        public getSubCategoriesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CategoryResponse> GetParentCategoriesAsync(string token)
        {
            // Validate token
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Invalid token provided.");
                throw new ArgumentException("Authorization token cannot be null or empty.", nameof(token));
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/categories/parent/");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Parent categories retrieved successfully");
                    return JsonSerializer.Deserialize<CategoryResponse>(responseContent) 
                           ?? throw new JsonException("Failed to deserialize the response");
                }
                else
                {
                    Console.WriteLine($"Error retrieving parent categories: {responseContent}");
                    throw new HttpRequestException($"Failed to get parent categories. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
                throw;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Parsing Exception: {jsonEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                throw;
            }
        }





    public async Task<string> GetSubCategoriesAsync(string token, string category)
    {
        // Construct the subcategories URL
        var databaseServerUrl = $"{_baseUrl}/categories/subcategories/list/";

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

        public async Task<CategoryDetails> GetCategoryDetailsAsync(string token, int categoryId)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Invalid token provided.");
                throw new ArgumentException("Authorization token cannot be null or empty.", nameof(token));
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/categories/{categoryId}/");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Category details retrieved successfully for ID: {categoryId}");
                    return JsonSerializer.Deserialize<CategoryDetails>(responseContent) 
                           ?? throw new JsonException("Failed to deserialize the response");
                }
                else
                {
                    Console.WriteLine($"Error retrieving category details: {responseContent}");
                    throw new HttpRequestException($"Failed to get category details. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
                throw;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Parsing Exception: {jsonEx.Message}");
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