// // Import necessary namespaces
// using Microsoft.AspNetCore.SignalR.Client;
// using System;
// using Microsoft.AspNetCore.Mvc;
// using System.Threading.Tasks;
// using BackEnd.middlewareService.Services;
// using System.Text.Json;
// using System.Linq;
// using System.Net.Http.Headers; // For MediaTypeHeaderValue
// using System.Text.Json.Serialization;

// namespace BackEnd.middlewareService.Controllers
// {
//     [ApiController]
//     [Route("api/")]
//     public class DashboardController : ControllerBase
//     {
//         private readonly TokenValidator _TokenValidator;
//         private HubConnection _hubConnection; // SignalR Hub connection
//         private readonly ILogger<DashboardController> _logger;
//         private TaskCompletionSource<object> _messageCompletionSource; // TaskCompletionSource to hold the response

//         private readonly numberOfUsersFromTokenService _numberOfUsersFromTokenService;
//         private readonly getMonthsSubscriptionsService _getMonthsSubscriptionsService;
//         private readonly numberOfSubscriptionsFromTokenService _numberOfSubscriptionsFromTokenService;

//         private readonly insertQuestionsSerivce _insertQuestionsSerivce;
//              private readonly ParagraphUploadService _uploadService;

//         public DashboardController(TokenValidator tokenValidator, ILogger<DashboardController> logger,
//             numberOfUsersFromTokenService numberOfUsers, getMonthsSubscriptionsService monthlySubscribe, numberOfSubscriptionsFromTokenService subscriptions, insertQuestionsSerivce insertQuestions,ParagraphUploadService uploadService)
//         {
//             _TokenValidator = tokenValidator;
//             _logger = logger;
//             _numberOfUsersFromTokenService = numberOfUsers;
//             _getMonthsSubscriptionsService = monthlySubscribe;
//             _numberOfSubscriptionsFromTokenService = subscriptions;
//             _insertQuestionsSerivce = insertQuestions;
//             _uploadService = uploadService;
//             InitializeSignalRConnection(); // Initialize SignalR connection on controller creation
//         }

//         private void InitializeSignalRConnection()
//         {
//             try
//             {
//                 _hubConnection = new HubConnectionBuilder()
//                     .WithUrl("http://localhost:5274/gamehub") // Your hub URL here
//                     .Build();

//                 _hubConnection.On<object>("ReceiveOnlineAndOfflineUsers", messageData =>
//                 {
//                     try
//                     {
//                         if (_messageCompletionSource != null && !_messageCompletionSource.Task.IsCompleted)
//                         {
//                             _messageCompletionSource.TrySetResult(messageData); // Set the result of the TaskCompletionSource
//                         }
//                     }
//                     catch (Exception ex)
//                     {
//                         _logger.LogError(ex, "Error processing the received message.");
//                     }
//                 });

//                 _hubConnection.StartAsync().ContinueWith(task =>
//                 {
//                     if (task.Exception != null)
//                     {
//                         _logger.LogError("Error starting SignalR connection: {Error}", task.Exception.GetBaseException());
//                     }
//                     else
//                     {
//                         _logger.LogInformation("SignalR connection started successfully.");
//                     }
//                 });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError("Error initializing SignalR connection: {Error}", ex.Message);
//             }
//         }

//         [HttpGet("dashboard")]
//         public async Task<IActionResult> GetOnlineUsersFromWebSocket([FromHeader] string Authorization)
//         {
//             if (string.IsNullOrEmpty(Authorization))
//             {
//                 return BadRequest("Refresh Token is required.");
//             }

//             var token = Authorization.Substring("Bearer ".Length).Trim();
//             string validationResult = await _TokenValidator.ValidateAdminAsync(token);

//             if (validationResult == "error")
//             {
//                 return Unauthorized("You are not authorized to get the accessToken list.");
//             }

//             if (_hubConnection.State != HubConnectionState.Connected)
//             {
//                 return StatusCode(500, "SignalR connection is not established.");
//             }

//             _messageCompletionSource = new TaskCompletionSource<object>();

//             await _hubConnection.SendAsync("onlineAndOfflineUsers");

//             try
//             {
//                 // Wait for the SignalR message with a timeout
//                 var result = await Task.WhenAny(_messageCompletionSource.Task, Task.Delay(10000)); // 10-second timeout

//                 if (result == _messageCompletionSource.Task)
//                 {
//                     // Extract the numberOfUsers from the result if it's in the expected format
//                     if (_messageCompletionSource.Task.Result is System.Text.Json.JsonElement jsonElement &&
//                         jsonElement.TryGetProperty("numberOfUsers", out var numberOfUsersProperty))
//                     {
//                         int numberOfOnlineUsers = numberOfUsersProperty.GetInt32();

//                         // Get the users from the database (serializedResponce)
//                         string serverResponse = await _numberOfUsersFromTokenService.GetNumberUsersAsync(token);
//                         string secondServerResponse = await _getMonthsSubscriptionsService.getMonthlySubscription(token);
//                         string thirdServerResponse = await _numberOfSubscriptionsFromTokenService.GetNumberSubsAsync(token);

//                         // Deserialize the server response for the total number of users
//                         var serializedResponce = JsonSerializer.Deserialize<TotalUsersDashboardResponce>(serverResponse);

//                         // Deserialize the second server response for monthly subscription data
//                         var secondSerializedResponce = JsonSerializer.Deserialize<List<monthlySubscriptionResponce>>(secondServerResponse);
//                         var thirdSerializedResponce = JsonSerializer.Deserialize<SubscriptionsDashboardResponce>(thirdServerResponse);

//                         return Ok(new
//                         {
//                             numberOfOnlineUsers = numberOfOnlineUsers,
//                             totalUsers = serializedResponce.totalUsers,
//                             numberOfOfflineUsers = serializedResponce.totalUsers - numberOfOnlineUsers,
//                             totalSubsctiptions = thirdSerializedResponce.totalSubscriptions,
//                             monthlySubscriptions = secondSerializedResponce?.Select(subscription => new
//                             {
//                                 month = subscription.month,
//                                 totalAmount = subscription.totalAmount
//                             }).ToList()
//                         });
//                     }
//                     else
//                     {
//                         return BadRequest("Unexpected response format from WebSocket server.");
//                     }
//                 }
//                 else
//                 {
//                     return StatusCode(504, "Timeout waiting for a response from the WebSocket server.");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error waiting for WebSocket response.");
//                 return StatusCode(500, "An error occurred while waiting for the response.");
//             }
//         }




//         [HttpPost("dashboard/send-news")]
//         public async Task<IActionResult> SendNewsToWebSocket([FromHeader] string Authorization, [FromBody] newsRequest request)
//         {
//             if (string.IsNullOrEmpty(Authorization))
//             {
//                 return BadRequest("Refresh Token is required.");
//             }

//             var token = Authorization.Substring("Bearer ".Length).Trim();
//             string validationResult = await _TokenValidator.ValidateAdminAsync(token);

//             if (validationResult == "error")
//             {
//                 return Unauthorized("You are not authorized to send news.");
//             }

//             if (_hubConnection.State != HubConnectionState.Connected)
//             {
//                 return StatusCode(500, "SignalR connection is not established.");
//             }

//             try
//             {
//                 await _hubConnection.SendAsync("updateNews", request);

//                 return Ok(new
//                 {
//                     news = request.news,
//                 });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error sending news to WebSocket.");
//                 return StatusCode(500, "An error occurred while sending the news.");
//             }
//         }





//         [HttpPost("dashboard/insert-question")]
//         [Consumes("multipart/form-data")]
//         public async Task<IActionResult> InsertQuestion([FromForm] InsertQuestionRequest request)
//         {
//             var authorization = HttpContext.Request.Headers["Authorization"].ToString();

//             if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
//             {
//                 return BadRequest(new
//                 {
//                     Message = "Authorization header is missing or invalid.",
//                     Status = "Error"
//                 });
//             }

//             var token = authorization.Substring("Bearer ".Length).Trim();
//             string validationResult = await _TokenValidator.ValidateTokenAsync(token);

//             if (validationResult == "error")
//             {
//                 return Unauthorized(new
//                 {
//                     Message = "You are not authorized to get the accessToken list."
//                 });
//             }

//             if (request.File == null || request.File.Length == 0)
//             {
//                 return BadRequest(new { Message = "File is required." });
//             }

//             string response = await _insertQuestionsSerivce.insertQuestion(request.File, token);

//             if (response == "error")
//             {
//                 return BadRequest(new { Message = "error from database." });
//             }

//             return Ok(new { Message = "File sent successfully." });
//         }

//         [HttpDelete("dashboard/question/{questionId}")]
//         public async Task<IActionResult> DeleteQuestion([FromRoute] int questionId)
//         {
//             var authorization = HttpContext.Request.Headers["Authorization"].ToString();
//             if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
//             {
//                 return Unauthorized(new { message = "No valid token provided" });
//             }
//             var token = authorization.Substring("Bearer ".Length).Trim();
//             string validationResult = await _TokenValidator.ValidateAdminAsync(token);
//             if (validationResult == "error")
//             {
//                 return Unauthorized(new { message = "You are not authorized to delete questions." });
//             }
//             bool result = await _insertQuestionsSerivce.DeleteQuestionAsync(questionId, token);
//             if (result)
//             {
//                 return Ok(new { message = $"Question {questionId} deleted successfully." });
//             }
//             else
//             {
//                 return StatusCode(500, new { message = $"Failed to delete question {questionId}." });
//             }
//         }

//         [HttpPatch("dashboard/question/{questionId}")]
//         public async Task<IActionResult> UpdateQuestion([FromRoute] int questionId, [FromBody] object updateBody)
//         {
//             var authorization = HttpContext.Request.Headers["Authorization"].ToString();
//             if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
//             {
//                 return Unauthorized(new { message = "No valid token provided" });
//             }
//             var token = authorization.Substring("Bearer ".Length).Trim();
//             string validationResult = await _TokenValidator.ValidateAdminAsync(token);
//             if (validationResult == "error")
//             {
//                 return Unauthorized(new { message = "You are not authorized to update questions." });
//             }
//             bool result = await _insertQuestionsSerivce.UpdateQuestionAsync(questionId, token, updateBody);
//             if (result)
//             {
//                 return Ok(new { message = $"Question {questionId} updated successfully." });
//             }
//             else
//             {
//                 return StatusCode(500, new { message = $"Failed to update question {questionId}." });
//             }
//         }


//         public class InsertQuestionRequest
//         {
//             [FromForm(Name = "file")]
//             public IFormFile File { get; set; }
//         }


//         [HttpPost("dashboard/upload-paragraph")]
//         [Consumes("multipart/form-data")]
//         public async Task<IActionResult> UploadParagraphQuestions([FromForm] ParagraphUploadRequest request)
//         {
//             if (request.File == null || request.File.Length == 0)
//             {
//                 return BadRequest("No file uploaded");
//             }

//             if (string.IsNullOrWhiteSpace(request.paragraphText))
//             {
//                 return BadRequest("Paragraph text is required");
//             }

//             // Get token from Authorization header
//             var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
//             if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
//             {
//                 return Unauthorized("No valid token provided");
//             }

//             var token = authHeader.Substring("Bearer ".Length).Trim();

//             try
//             {
//                 var result = await _uploadService.UploadParagraphWithQuestionsAsync(token, request.File, request.paragraphText);
//                 return Ok(new
//                 {
//                     message = "Paragraph and questions uploaded successfully",
//                     data = result
//                 });
//             }
//             catch (HttpRequestException ex)
//             {
//                 return StatusCode(500, new { error = ex.Message });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
//             }
//         }






//         [HttpGet("dashboard/classic-questions")]
//         public async Task<IActionResult> GetClassicQuestions(
//             [FromHeader] string Authorization,
//             [FromQuery] int page = 1,
//             [FromQuery] int pageSize = 10)
//         {
//             if (string.IsNullOrEmpty(Authorization))
//                 return BadRequest("Authorization token is required.");

//             var token = Authorization.Replace("Bearer ", "").Trim();
//             string validationResult = await _TokenValidator.ValidateAdminAsync(token);

//             if (validationResult == "error")
//                 return Unauthorized("You are not authorized to access this resource.");

//             try
//             {
//                 using var client = new HttpClient();
//                 client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//                 client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                 // 🔹 Using localhost instead of the public IP
//                 var response = await client.GetAsync("http://localhost:8004/api/questions/?subcategory=Description");
//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, "Failed to fetch classic questions from server.");

//                 var jsonString = await response.Content.ReadAsStringAsync();
//                 var questions = JsonSerializer.Deserialize<List<QuestionWithIds>>(jsonString,
//                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//                 // 🔹 Pagination
//                 int totalItems = questions.Count;
//                 var paginatedQuestions = questions
//                     .Skip((page - 1) * pageSize)
//                     .Take(pageSize)
//                     .ToList();

//                 string baseUrl = $"http://localhost:5038/api/dashboard/classic-questions";
//                 string? next = (page * pageSize < totalItems)
//                     ? $"{baseUrl}?page={page + 1}&pageSize={pageSize}"
//                     : null;
//                 string? previous = (page > 1)
//                     ? $"{baseUrl}?page={page - 1}&pageSize={pageSize}"
//                     : null;

//                 var result = new
//                 {
//                     count = totalItems,
//                     next,
//                     previous,
//                     results = paginatedQuestions
//                 };

//                 return Ok(result);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching classic questions.");
//                 return StatusCode(500, new { error = ex.Message });
//             }
//         }



//         [HttpGet("dashboard/millionaire-questions")]
//         public async Task<IActionResult> GetMillionaireQuestions(
//             [FromHeader] string Authorization,
//             [FromQuery] int page = 1,
//             [FromQuery] int pageSize = 10)
//         {
//             if (string.IsNullOrEmpty(Authorization))
//                 return BadRequest("Authorization token is required.");

//             var token = Authorization.Replace("Bearer ", "").Trim();
//             string validationResult = await _TokenValidator.ValidateAdminAsync(token);

//             if (validationResult == "error")
//                 return Unauthorized("You are not authorized to access this resource.");

//             try
//             {
//                 using var client = new HttpClient();
//                 client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//                 client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                 // 🔹 Using localhost instead of the public IP
//                 var response = await client.GetAsync("http://localhost:8004/api/millionaire/questions/");
//                 if (!response.IsSuccessStatusCode)
//                     return StatusCode((int)response.StatusCode, "Failed to fetch millionaire questions from server.");

//                 var jsonString = await response.Content.ReadAsStringAsync();
//                 var questions = JsonSerializer.Deserialize<List<QuestionWithIds>>(jsonString,
//                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//                 // 🔹 Pagination
//                 int totalItems = questions.Count;
//                 var paginatedQuestions = questions
//                     .Skip((page - 1) * pageSize)
//                     .Take(pageSize)
//                     .ToList();

//                 string baseUrl = $"http://localhost:5038/api/dashboard/millionaire-questions";
//                 string? next = (page * pageSize < totalItems)
//                     ? $"{baseUrl}?page={page + 1}&pageSize={pageSize}"
//                     : null;
//                 string? previous = (page > 1)
//                     ? $"{baseUrl}?page={page - 1}&pageSize={pageSize}"
//                     : null;

//                 var result = new
//                 {
//                     count = totalItems,
//                     next,
//                     previous,
//                     results = paginatedQuestions
//                 };

//                 return Ok(result);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching millionaire questions.");
//                 return StatusCode(500, new { error = ex.Message });
//             }
//         }



//         // [HttpGet("dashboard/classic-questions")]
//         // public async Task<IActionResult> GetClassicQuestions(
//         //     [FromHeader] string Authorization,
//         //     [FromQuery] int page = 1,
//         //     [FromQuery] int pageSize = 10)
//         // {
//         //     if (string.IsNullOrEmpty(Authorization))
//         //         return BadRequest("Authorization token is required.");

//         //     var token = Authorization.Replace("Bearer ", "").Trim();
//         //     string validationResult = await _TokenValidator.ValidateAdminAsync(token);

//         //     if (validationResult == "error")
//         //         return Unauthorized("You are not authorized to access this resource.");

//         //     try
//         //     {
//         //         using var client = new HttpClient();
//         //         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//         //         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

//         //         // 🔹 Using localhost instead of the public IP
//         //         var response = await client.GetAsync("http://localhost:8004/api/questions/?subcategory=Description");
//         //         if (!response.IsSuccessStatusCode)
//         //             return StatusCode((int)response.StatusCode, "Failed to fetch classic questions from server.");

//         //         var jsonString = await response.Content.ReadAsStringAsync();
//         //         var questions = JsonSerializer.Deserialize<List<BackEnd.middlewareService.Models.Question>>(jsonString,
//         //             new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//         //         foreach (var q in questions)
//         //         {
//         //             var correct = q.answers.FirstOrDefault(a => a.is_correct);
//         //             q.correctAnswer = correct?.answerText ?? "";
//         //         }

//         //         // 🔹 Pagination
//         //         int totalItems = questions.Count;
//         //         var paginatedQuestions = questions
//         //             .Skip((page - 1) * pageSize)
//         //             .Take(pageSize)
//         //             .ToList();

//         //         string baseUrl = $"http://localhost:5038/api/dashboard/classic-questions";
//         //         string? next = (page * pageSize < totalItems)
//         //             ? $"{baseUrl}?page={page + 1}&pageSize={pageSize}"
//         //             : null;
//         //         string? previous = (page > 1)
//         //             ? $"{baseUrl}?page={page - 1}&pageSize={pageSize}"
//         //             : null;

//         //         var result = new
//         //         {
//         //             count = totalItems,
//         //             next,
//         //             previous,
//         //             results = paginatedQuestions
//         //         };

//         //         return Ok(result);
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         _logger.LogError(ex, "Error fetching classic questions.");
//         //         return StatusCode(500, new { error = ex.Message });
//         //     }
//         // }



//         // [HttpGet("dashboard/millionaire-questions")]
//         // public async Task<IActionResult> GetMillionaireQuestions(
//         //     [FromHeader] string Authorization,
//         //     [FromQuery] int page = 1,
//         //     [FromQuery] int pageSize = 10)
//         // {
//         //     if (string.IsNullOrEmpty(Authorization))
//         //         return BadRequest("Authorization token is required.");

//         //     var token = Authorization.Replace("Bearer ", "").Trim();
//         //     string validationResult = await _TokenValidator.ValidateAdminAsync(token);

//         //     if (validationResult == "error")
//         //         return Unauthorized("You are not authorized to access this resource.");

//         //     try
//         //     {
//         //         using var client = new HttpClient();
//         //         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//         //         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

//         //         // 🔹 Using localhost instead of the public IP
//         //         var response = await client.GetAsync("http://localhost:8004/api/millionaire/questions/");
//         //         if (!response.IsSuccessStatusCode)
//         //             return StatusCode((int)response.StatusCode, "Failed to fetch millionaire questions from server.");

//         //         var jsonString = await response.Content.ReadAsStringAsync();
//         //         var questions = JsonSerializer.Deserialize<List<BackEnd.middlewareService.Models.Question>>(jsonString,
//         //             new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//         //         foreach (var q in questions)
//         //         {
//         //             var correct = q.answers.FirstOrDefault(a => a.is_correct);
//         //             q.correctAnswer = correct?.answerText ?? "";
//         //         }

//         //         // 🔹 Pagination
//         //         int totalItems = questions.Count;
//         //         var paginatedQuestions = questions
//         //             .Skip((page - 1) * pageSize)
//         //             .Take(pageSize)
//         //             .ToList();

//         //         string baseUrl = $"http://localhost:5038/api/dashboard/millionaire-questions";
//         //         string? next = (page * pageSize < totalItems)
//         //             ? $"{baseUrl}?page={page + 1}&pageSize={pageSize}"
//         //             : null;
//         //         string? previous = (page > 1)
//         //             ? $"{baseUrl}?page={page - 1}&pageSize={pageSize}"
//         //             : null;

//         //         var result = new
//         //         {
//         //             count = totalItems,
//         //             next,
//         //             previous,
//         //             results = paginatedQuestions
//         //         };

//         //         return Ok(result);
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         _logger.LogError(ex, "Error fetching millionaire questions.");
//         //         return StatusCode(500, new { error = ex.Message });
//         //     }
//         // }

//     }

//     public class ParagraphUploadRequest
//     {
//         [FromForm(Name = "file")]
//         public IFormFile File { get; set; }
//         public string paragraphText { get; set; }
//     }


//     public class QuestionWithIds
//     {
//         [JsonPropertyName("id")]
//         public int Id { get; set; }

//         [JsonPropertyName("text")]
//         public string QuestionText { get; set; }

//         [JsonPropertyName("sub_category")]
//         public string SubCategory { get; set; }

//         [JsonPropertyName("answers")]
//         public List<AnswerWithId> Answers { get; set; }
//     }

//     public class AnswerWithId
//     {
//         [JsonPropertyName("id")]
//         public int Id { get; set; }

//         [JsonPropertyName("text")]
//         public string AnswerText { get; set; }

//         [JsonPropertyName("is_correct")]
//         public bool IsCorrect { get; set; }
//     }

   

//     // Response models
//     public class TotalUsersDashboardResponce
//     {
//         [JsonPropertyName("total_users")]  // Mapping snake_case to PascalCase
//         public int totalUsers { get; set; }
//     }

//     public class SubscriptionsDashboardResponce
//     {
//         [JsonPropertyName("total_subscriptions")]  // Mapping snake_case to PascalCase
//         public int totalSubscriptions { get; set; }
//     }
//     public class monthlySubscriptionResponce
//     {
//         [JsonPropertyName("month")]  // Mapping snake_case to PascalCase
//         public string month { get; set; }

//         [JsonPropertyName("total_amount")]  // Mapping snake_case to PascalCase
//         public double totalAmount { get; set; }  // Use double to match the response value type (e.g., 3523.0)
//     }


//     public class newsRequest{
//         public string news {get; set;}
//     }
// }

// Import necessary namespaces
using Microsoft.AspNetCore.SignalR.Client;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Linq;
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Text.Json.Serialization;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/")]
    public class DashboardController : ControllerBase
    {
        private readonly TokenValidator _TokenValidator;
        private HubConnection _hubConnection; // SignalR Hub connection
        private readonly ILogger<DashboardController> _logger;
        private TaskCompletionSource<object> _messageCompletionSource; // TaskCompletionSource to hold the response

        private readonly numberOfUsersFromTokenService _numberOfUsersFromTokenService;
        private readonly getMonthsSubscriptionsService _getMonthsSubscriptionsService;
        private readonly numberOfSubscriptionsFromTokenService _numberOfSubscriptionsFromTokenService;

        private readonly insertQuestionsSerivce _insertQuestionsSerivce;
             private readonly ParagraphUploadService _uploadService;

        public DashboardController(TokenValidator tokenValidator, ILogger<DashboardController> logger,
            numberOfUsersFromTokenService numberOfUsers, getMonthsSubscriptionsService monthlySubscribe, numberOfSubscriptionsFromTokenService subscriptions, insertQuestionsSerivce insertQuestions,ParagraphUploadService uploadService)
        {
            _TokenValidator = tokenValidator;
            _logger = logger;
            _numberOfUsersFromTokenService = numberOfUsers;
            _getMonthsSubscriptionsService = monthlySubscribe;
            _numberOfSubscriptionsFromTokenService = subscriptions;
            _insertQuestionsSerivce = insertQuestions;
            _uploadService = uploadService;
            InitializeSignalRConnection(); // Initialize SignalR connection on controller creation
        }

        private void InitializeSignalRConnection()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5274/gamehub") // Your hub URL here
                    .Build();

                _hubConnection.On<object>("ReceiveOnlineAndOfflineUsers", messageData =>
                {
                    try
                    {
                        if (_messageCompletionSource != null && !_messageCompletionSource.Task.IsCompleted)
                        {
                            _messageCompletionSource.TrySetResult(messageData); // Set the result of the TaskCompletionSource
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing the received message.");
                    }
                });

                _hubConnection.StartAsync().ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _logger.LogError("Error starting SignalR connection: {Error}", task.Exception.GetBaseException());
                    }
                    else
                    {
                        _logger.LogInformation("SignalR connection started successfully.");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing SignalR connection: {Error}", ex.Message);
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetOnlineUsersFromWebSocket([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Refresh Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();
            string validationResult = await _TokenValidator.ValidateAdminAsync(token);

            if (validationResult == "error")
            {
                return Unauthorized("You are not authorized to get the accessToken list.");
            }

            if (_hubConnection.State != HubConnectionState.Connected)
            {
                return StatusCode(500, "SignalR connection is not established.");
            }

            _messageCompletionSource = new TaskCompletionSource<object>();

            await _hubConnection.SendAsync("onlineAndOfflineUsers");

            try
            {
                // Wait for the SignalR message with a timeout
                var result = await Task.WhenAny(_messageCompletionSource.Task, Task.Delay(10000)); // 10-second timeout

                if (result == _messageCompletionSource.Task)
                {
                    // Extract the numberOfUsers from the result if it's in the expected format
                    if (_messageCompletionSource.Task.Result is System.Text.Json.JsonElement jsonElement &&
                        jsonElement.TryGetProperty("numberOfUsers", out var numberOfUsersProperty))
                    {
                        int numberOfOnlineUsers = numberOfUsersProperty.GetInt32();

                        // Get the users from the database (serializedResponce)
                        string serverResponse = await _numberOfUsersFromTokenService.GetNumberUsersAsync(token);
                        string secondServerResponse = await _getMonthsSubscriptionsService.getMonthlySubscription(token);
                        string thirdServerResponse = await _numberOfSubscriptionsFromTokenService.GetNumberSubsAsync(token);

                        // Deserialize the server response for the total number of users
                        var serializedResponce = JsonSerializer.Deserialize<TotalUsersDashboardResponce>(serverResponse);

                        // Deserialize the second server response for monthly subscription data
                        var secondSerializedResponce = JsonSerializer.Deserialize<List<monthlySubscriptionResponce>>(secondServerResponse);
                        var thirdSerializedResponce = JsonSerializer.Deserialize<SubscriptionsDashboardResponce>(thirdServerResponse);

                        return Ok(new
                        {
                            numberOfOnlineUsers = numberOfOnlineUsers,
                            totalUsers = serializedResponce.totalUsers,
                            numberOfOfflineUsers = serializedResponce.totalUsers - numberOfOnlineUsers,
                            totalSubsctiptions = thirdSerializedResponce.totalSubscriptions,
                            monthlySubscriptions = secondSerializedResponce?.Select(subscription => new
                            {
                                month = subscription.month,
                                totalAmount = subscription.totalAmount
                            }).ToList()
                        });
                    }
                    else
                    {
                        return BadRequest("Unexpected response format from WebSocket server.");
                    }
                }
                else
                {
                    return StatusCode(504, "Timeout waiting for a response from the WebSocket server.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for WebSocket response.");
                return StatusCode(500, "An error occurred while waiting for the response.");
            }
        }




        [HttpPost("dashboard/send-news")]
        public async Task<IActionResult> SendNewsToWebSocket([FromHeader] string Authorization, [FromBody] newsRequest request)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Refresh Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();
            string validationResult = await _TokenValidator.ValidateAdminAsync(token);

            if (validationResult == "error")
            {
                return Unauthorized("You are not authorized to send news.");
            }

            if (_hubConnection.State != HubConnectionState.Connected)
            {
                return StatusCode(500, "SignalR connection is not established.");
            }

            try
            {
                await _hubConnection.SendAsync("updateNews", request);

                return Ok(new
                {
                    news = request.news,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending news to WebSocket.");
                return StatusCode(500, "An error occurred while sending the news.");
            }
        }





        [HttpPost("dashboard/insert-question")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> InsertQuestion([FromForm] InsertQuestionRequest request)
        {
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
            {
                return BadRequest(new
                {
                    Message = "Authorization header is missing or invalid.",
                    Status = "Error"
                });
            }

            var token = authorization.Substring("Bearer ".Length).Trim();
            string validationResult = await _TokenValidator.ValidateTokenAsync(token);

            if (validationResult == "error")
            {
                return Unauthorized(new
                {
                    Message = "You are not authorized to get the accessToken list."
                });
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { Message = "File is required." });
            }

            string response = await _insertQuestionsSerivce.insertQuestion(request.File, token);

            if (response == "error")
            {
                return BadRequest(new { Message = "error from database." });
            }

            return Ok(new { Message = "File sent successfully." });
        }

        [HttpDelete("dashboard/question/{questionId}")]
        public async Task<IActionResult> DeleteQuestion([FromRoute] int questionId)
        {
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "No valid token provided" });
            }
            var token = authorization.Substring("Bearer ".Length).Trim();
            string validationResult = await _TokenValidator.ValidateAdminAsync(token);
            if (validationResult == "error")
            {
                return Unauthorized(new { message = "You are not authorized to delete questions." });
            }
            bool result = await _insertQuestionsSerivce.DeleteQuestionAsync(questionId, token);
            if (result)
            {
                return Ok(new { message = $"Question {questionId} deleted successfully." });
            }
            else
            {
                return StatusCode(500, new { message = $"Failed to delete question {questionId}." });
            }
        }

        [HttpPatch("dashboard/question/{questionId}")]
        public async Task<IActionResult> UpdateQuestion([FromRoute] int questionId, [FromBody] object updateBody)
        {
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "No valid token provided" });
            }
            var token = authorization.Substring("Bearer ".Length).Trim();
            string validationResult = await _TokenValidator.ValidateAdminAsync(token);
            if (validationResult == "error")
            {
                return Unauthorized(new { message = "You are not authorized to update questions." });
            }
            bool result = await _insertQuestionsSerivce.UpdateQuestionAsync(questionId, token, updateBody);
            if (result)
            {
                return Ok(new { message = $"Question {questionId} updated successfully." });
            }
            else
            {
                return StatusCode(500, new { message = $"Failed to update question {questionId}." });
            }
        }


        public class InsertQuestionRequest
        {
            [FromForm(Name = "file")]
            public IFormFile File { get; set; }
        }


        [HttpPost("dashboard/upload-paragraph")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadParagraphQuestions([FromForm] ParagraphUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (string.IsNullOrWhiteSpace(request.paragraphText))
            {
                return BadRequest("Paragraph text is required");
            }

            // Get token from Authorization header
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized("No valid token provided");
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var result = await _uploadService.UploadParagraphWithQuestionsAsync(token, request.File, request.paragraphText);
                return Ok(new
                {
                    message = "Paragraph and questions uploaded successfully",
                    data = result
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }









        [HttpGet("dashboard/classic-questions")]
        public async Task<IActionResult> GetClassicQuestions(
            [FromHeader] string Authorization,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Authorization token is required.");

            var token = Authorization.Replace("Bearer ", "").Trim();
            string validationResult = await _TokenValidator.ValidateAdminAsync(token);

            if (validationResult == "error")
                return Unauthorized("You are not authorized to access this resource.");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // 🔹 Using localhost instead of the public IP
                var response = await client.GetAsync("http://localhost:8004/api/questions/?subcategory=Description");
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Failed to fetch classic questions from server.");

                var jsonString = await response.Content.ReadAsStringAsync();
                var questions = JsonSerializer.Deserialize<List<BackEnd.middlewareService.Models.Question>>(jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                foreach (var q in questions)
                {
                    var correct = q.answers.FirstOrDefault(a => a.is_correct);
                    q.correctAnswer = correct?.answerText ?? "";
                }

                // 🔹 Pagination
                int totalItems = questions.Count;
                var paginatedQuestions = questions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                string baseUrl = $"http://localhost:5038/api/dashboard/classic-questions";
                string? next = (page * pageSize < totalItems)
                    ? $"{baseUrl}?page={page + 1}&pageSize={pageSize}"
                    : null;
                string? previous = (page > 1)
                    ? $"{baseUrl}?page={page - 1}&pageSize={pageSize}"
                    : null;

                var result = new
                {
                    count = totalItems,
                    next,
                    previous,
                    results = paginatedQuestions
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching classic questions.");
                return StatusCode(500, new { error = ex.Message });
            }
        }



        [HttpGet("dashboard/millionaire-questions")]
        public async Task<IActionResult> GetMillionaireQuestions(
            [FromHeader] string Authorization,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Authorization token is required.");

            var token = Authorization.Replace("Bearer ", "").Trim();
            string validationResult = await _TokenValidator.ValidateAdminAsync(token);

            if (validationResult == "error")
                return Unauthorized("You are not authorized to access this resource.");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // 🔹 Using localhost instead of the public IP
                var response = await client.GetAsync("http://localhost:8004/api/millionaire/questions/");
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Failed to fetch millionaire questions from server.");

                var jsonString = await response.Content.ReadAsStringAsync();
                var questions = JsonSerializer.Deserialize<List<BackEnd.middlewareService.Models.Question>>(jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                foreach (var q in questions)
                {
                    var correct = q.answers.FirstOrDefault(a => a.is_correct);
                    q.correctAnswer = correct?.answerText ?? "";
                }

                // 🔹 Pagination
                int totalItems = questions.Count;
                var paginatedQuestions = questions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                string baseUrl = $"http://localhost:5038/api/dashboard/millionaire-questions";
                string? next = (page * pageSize < totalItems)
                    ? $"{baseUrl}?page={page + 1}&pageSize={pageSize}"
                    : null;
                string? previous = (page > 1)
                    ? $"{baseUrl}?page={page - 1}&pageSize={pageSize}"
                    : null;

                var result = new
                {
                    count = totalItems,
                    next,
                    previous,
                    results = paginatedQuestions
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching millionaire questions.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }

    public class ParagraphUploadRequest
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; }
        public string paragraphText { get; set; }
    }




   

    // Response models
    public class TotalUsersDashboardResponce
    {
        [JsonPropertyName("total_users")]  // Mapping snake_case to PascalCase
        public int totalUsers { get; set; }
    }

    public class SubscriptionsDashboardResponce
    {
        [JsonPropertyName("total_subscriptions")]  // Mapping snake_case to PascalCase
        public int totalSubscriptions { get; set; }
    }
    public class monthlySubscriptionResponce
    {
        [JsonPropertyName("month")]  // Mapping snake_case to PascalCase
        public string month { get; set; }

        [JsonPropertyName("total_amount")]  // Mapping snake_case to PascalCase
        public double totalAmount { get; set; }  // Use double to match the response value type (e.g., 3523.0)
    }


    public class newsRequest{
        public string news {get; set;}
    }
}