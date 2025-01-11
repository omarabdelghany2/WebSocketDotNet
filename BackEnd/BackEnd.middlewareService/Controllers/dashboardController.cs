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

        public DashboardController(TokenValidator tokenValidator, ILogger<DashboardController> logger,
            numberOfUsersFromTokenService numberOfUsers, getMonthsSubscriptionsService monthlySubscribe)
        {
            _TokenValidator = tokenValidator;
            _logger = logger;
            _numberOfUsersFromTokenService = numberOfUsers;
            _getMonthsSubscriptionsService = monthlySubscribe;
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
            string validationResult = await _TokenValidator.ValidateTokenAsync(token);

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

                        // Deserialize the server response for the total number of users
                        var serializedResponce = JsonSerializer.Deserialize<UsersDashboardResponce>(serverResponse);

                        // Deserialize the second server response for monthly subscription data
                        var secondSerializedResponce = JsonSerializer.Deserialize<List<monthlySubscriptionResponce>>(secondServerResponse);

                        return Ok(new
                        {
                            numberOfOnlineUsers = numberOfOnlineUsers,
                            totalUsers = serializedResponce.totalUsers,
                            numberOfOfflineUsers = serializedResponce.totalUsers - numberOfOnlineUsers,
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
    }

    // Response models
    public class UsersDashboardResponce
    {
        [JsonPropertyName("total_users")]  // Mapping snake_case to PascalCase
        public int totalUsers { get; set; }
    }

    public class monthlySubscriptionResponce
    {
        [JsonPropertyName("month")]  // Mapping snake_case to PascalCase
        public string month { get; set; }

        [JsonPropertyName("total_amount")]  // Mapping snake_case to PascalCase
        public double totalAmount { get; set; }  // Use double to match the response value type (e.g., 3523.0)
    }
}
