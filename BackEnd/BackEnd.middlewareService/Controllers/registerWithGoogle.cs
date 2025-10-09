using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/register")]
    public class registerWithGooglerController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public registerWithGooglerController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpPost("by-google")]
        public async Task<IActionResult> RegisterWithGoogle([FromBody] GoogleRegisterInput input)
        {
            try
            {
                // 1️⃣ Verify the Google token
                var payload = await GoogleJsonWebSignature.ValidateAsync(input.IdToken);

                string email = payload.Email;
                string name = payload.Name;

                Console.WriteLine($"[Google Register] Email: {email}, Name: {name}");

                // 2️⃣ Correct DB server API URL
                var databaseServerUrl = "http://127.0.0.1:8004/api/user/auth/google-register/";

                // 3️⃣ Build the JSON body correctly
                var body = new
                {
                    profile_name = name,
                    email = email
                };

                var jsonBody = JsonSerializer.Serialize(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // 4️⃣ Explicitly set headers
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // 5️⃣ Send request
                var response = await _httpClient.PostAsync(databaseServerUrl, content);

                // 6️⃣ Handle response
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ DB Response: {responseText}");

                    var tokens = JsonSerializer.Deserialize<TokenResponse>(responseText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Ok(new
                    {
                        Message = "Google Registration successful"
                    });
                }
                else
                {
                    Console.WriteLine($"❌ DB Error Response: {responseText}");
                    return StatusCode((int)response.StatusCode, new
                    {
                        Message = "Google Registration failed",
                        Error = responseText
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Google registration failed",
                    Exception = ex.Message
                });
            }
        }






        // [HttpPost("google")]
        // public async Task<IActionResult> RegisterWithGoogle([FromBody] GoogleRegisterInput input)
        // {
        //     try
        //     {
        //         // Just log the raw IdToken received from frontend
        //         Console.WriteLine($"Received Google IdToken: {input.IdToken}");

        //         // Validate the token with Google
        //         var payload = await GoogleJsonWebSignature.ValidateAsync(input.IdToken);

        //         // Extract some fields to see what we get
        //         Console.WriteLine($"Google Email: {payload.Email}");
        //         Console.WriteLine($"Google Name: {payload.Name}");
        //         Console.WriteLine($"Google Picture: {payload.Picture}");

        //         // For now, just return the raw payload (no DB call)
        //         return Ok(new
        //         {
        //             Message = "Google token received and validated successfully",
        //             Email = payload.Email,
        //             Name = payload.Name,
        //             Picture = payload.Picture,
        //             FullPayload = payload  // includes all claims from Google
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { Message = "Google registration failed", Exception = ex.Message });
        //     }
        // }

    }

    public class GoogleRegisterInput
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; }
    }
}
