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

        public async Task<List<CategoryDetails>> GetParentCategoriesAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Authorization token cannot be null or empty.", nameof(token));

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/categories/parent/");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Deserialize to CategoryResponse, then extract Results
                    var categoryResponse = JsonSerializer.Deserialize<CategoryResponse>(responseContent, options);
                    if (categoryResponse?.Results != null)
                        return categoryResponse.Results;

                    return new List<CategoryDetails>();
                }
                else
                {
                    Console.WriteLine($"Error retrieving parent categories: {responseContent}");
                    throw new HttpRequestException($"Failed to get parent categories. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }



        public async Task<List<SubCategory>> GetSubCategoriesAsync(string token, string category)
{
    if (string.IsNullOrWhiteSpace(token))
        throw new ArgumentException("Authorization token cannot be null or empty.", nameof(token));

    var encodedCategory = Uri.EscapeDataString(category.Trim());
    var fullUrl = $"{_baseUrl}/categories/subcategories/list/?categories={encodedCategory}";
    Console.WriteLine($"Request URL: {fullUrl}");

    var requestMessage = new HttpRequestMessage(HttpMethod.Get, fullUrl);
    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    try
    {
        var response = await _httpClient.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Subcategories retrieved successfully");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<SubCategory>>(responseContent, options)
                   ?? new List<SubCategory>();
        }
        else
        {
            Console.WriteLine($"Error retrieving subcategories: {responseContent}");
            throw new HttpRequestException($"Failed to get subcategories. Status code: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
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