using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Logging;
using BackEnd.middlewareService.Services;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/")]
    public class SubscriptionWebhookController : ControllerBase
    {
        private readonly ILogger<SubscriptionWebhookController> _logger;
        private readonly paypalDatabaseServices _paypalDatabaseServices;

        public SubscriptionWebhookController(ILogger<SubscriptionWebhookController> logger, paypalDatabaseServices paypalDatabaseServices)
        {
            _logger = logger;
            _paypalDatabaseServices = paypalDatabaseServices;
        }


        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscription()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                // Parse the JSON body
                var requestData = JsonDocument.Parse(requestBody);
                _logger.LogInformation("Incoming JSON: {RequestBody}", requestBody);
                // Extract the required fields
                var resource = requestData.RootElement.GetProperty("resource");

                // Check if 'custom_id' exists and extract it
                var userId = resource.TryGetProperty("custom_id", out var customIdElement)
                    ? customIdElement.GetString()
                    : null; // Handle the case where 'custom_id' is missing

                var startTime=resource.GetProperty("create_time").GetString();
                var planId=resource.GetProperty("plan_id").GetString();
                var subscriptionId = resource.GetProperty("id").GetString();
                var status = resource.GetProperty("status").GetString();

                // Log the data
                _logger.LogInformation("User ID: {UserId}", userId);
                _logger.LogInformation("Subscription ID: {SubscriptionId}", subscriptionId);
                // _logger.LogInformation("Status: {Status}", status);

                // Database call (if needed)
                // Note: Since 'payerId' is not available in the JSON, you may need to adjust your database logic
                bool result = await _paypalDatabaseServices.subscribeAsync(userId=userId, planId=planId, startTime=startTime,subscriptionId=subscriptionId);

                return Ok(new
                {
                    message = "Subscription created",
                    subscription_id = subscriptionId,
                    custom_id = userId
                });
            }
            catch (Exception ex)
            {
                // Log any errors that occur
                _logger.LogError(ex, "Error processing subscription request");

                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }


        [HttpPost("billing")]
        public async Task<IActionResult> payments()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                
                // Parse the JSON body
                var requestData = JsonDocument.Parse(requestBody);
                _logger.LogInformation("Incoming JSON: {RequestBody}", requestBody);
                
                // Extract the required fields
                var resource = requestData.RootElement.GetProperty("resource");

                // Correct the key for PayPal payment ID, it should be 'id' instead of 'xfx'
                string paypalPaymentId = resource.GetProperty("id").GetString(); // <-- fixed key here
                string amount = resource.GetProperty("amount").GetProperty("total").GetString();
                var subscriptionId = resource.GetProperty("billing_agreement_id").GetString(); // Use correct field for subscription ID

                // Log the data
                _logger.LogInformation("Subscription ID: {SubscriptionId}", subscriptionId);
                
                // Database call (if needed)
                bool result = await _paypalDatabaseServices.billingAsync(subscriptionId = subscriptionId, amount = amount, paypalPaymentId = paypalPaymentId);

                return Ok(new
                {
                    message = "payment success"
                });
            }
            catch (Exception ex)
            {
                // Log any errors that occur
                _logger.LogError(ex, "Error processing subscription request");

                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }



        [HttpPost]
        [Route("activate")]
        public async Task<IActionResult> activateSubscription()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                // Parse the JSON body
                var requestData = JsonDocument.Parse(requestBody);
                _logger.LogInformation("Incoming JSON: {RequestBody}", requestBody);

                // Extract the required fields
                var resource = requestData.RootElement.GetProperty("resource");
                var payerId = resource.GetProperty("subscriber").GetProperty("payer_id").GetString();
                var userId = resource.GetProperty("custom_id").GetString();
                var subscriptionId = resource.GetProperty("id").GetString();
                var status = resource.GetProperty("status").GetString();

                // Log the data
                _logger.LogInformation("Payer ID: {PayerId}", payerId);
                _logger.LogInformation("User ID: {UserId}", userId);
                _logger.LogInformation("Subscription ID: {SubscriptionId}", subscriptionId);
                _logger.LogInformation("Status: {Status}", status);



                //call the database to make the edits there

                bool result = await _paypalDatabaseServices.activateAsync(subscriptionId);
                // Return success response
                return Ok(new
                {
                    Message = "Subscription activated",
                    payerId=payerId,
                    UserId = userId,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error processing activateSubscription request");

                // Return error response
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpPost]
        [Route("expire")]
        public async Task<IActionResult> expireSubscription()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                // Parse the JSON body
                var requestData = JsonDocument.Parse(requestBody);

                // Extract the required fields
                var resource = requestData.RootElement.GetProperty("resource");

                // Custom ID might be available in 'custom_id', if present
                var subscriptionId = resource.GetProperty("id").GetString();
                // var userId = resource.TryGetProperty("custom_id", out var customId) ? customId.GetString() : "N/A"; // Handle absence
                // var status = resource.GetProperty("status").GetString();

                // // Log the data
                // _logger.LogInformation("User ID: {UserId}", userId);
                // _logger.LogInformation("Status: {Status}", status);

                // Call the database to make the edits there
                bool result = await _paypalDatabaseServices.expireAsync(subscriptionId=subscriptionId);

                // Return success response
                return Ok(new
                {
                    Message = "Subscription expired"
                });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error processing expiredSubscription request");

                // Return error response
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpPost]
        [Route("cancel")]
        public async Task<IActionResult> cancelSubscription()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                // Parse the JSON body
                var requestData = JsonDocument.Parse(requestBody);

                // Extract the required fields
                var resource = requestData.RootElement.GetProperty("resource");
                var subscriptionId = resource.GetProperty("id").GetString();
                // var userId = resource.GetProperty("custom_id").GetString();
                // var status = resource.GetProperty("status").GetString();

                // // Log the data
                // _logger.LogInformation("User ID: {UserId}", userId);
                // _logger.LogInformation("Status: {Status}", status);
                _logger.LogInformation("subscriptionId: {subscriptionId}", subscriptionId);
                // Call the database to make the edits there
                bool result = await _paypalDatabaseServices.cancelAsync(subscriptionId);

                // Return success response
                return Ok(new
                {
                    Message = "Subscription cancelled"
                    // UserId = userId,
                    // Status = status
                });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error processing cancelSubscription request");

                // Return error response
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }




        [HttpPost("paypal-webhook")]
        public async Task<IActionResult> PaypalWebhook()
        {
            try
            {
                // Read body
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation("🔔 PayPal Webhook received:\n{Body}", body);

                // Parse JSON
                var json = JsonDocument.Parse(body);
                var eventType = json.RootElement.GetProperty("event_type").GetString();
                _logger.LogInformation("Event type: {EventType}", eventType);

                // Extract resource
                if (json.RootElement.TryGetProperty("resource", out var resource))
                {
                    // Get PayPal transaction info
                    string transactionId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : "N/A";
                    string amount = "N/A";

                    if (resource.TryGetProperty("amount", out var amountProp))
                    {
                        amount = amountProp.TryGetProperty("value", out var valProp) ? valProp.GetString() : "N/A";
                    }

                    // Get custom user ID (we’ll hardcode 900 in HTML)
                    string customId = resource.TryGetProperty("custom_id", out var customProp)
                        ? customProp.GetString()
                        : "N/A";

                    _logger.LogInformation("💰 Transaction ID: {TransactionId}, Amount: {Amount}, Custom ID: {CustomId}", transactionId, amount, customId);

                    // Optionally, you can process it only when payment completed
                    if (eventType == "PAYMENT.CAPTURE.COMPLETED")
                    {
                        _logger.LogInformation("✅ Payment captured successfully for User {CustomId}", customId);
                        // Optionally call your database service
                        // await _paypalDatabaseServices.MarkPaymentReceivedAsync(customId, transactionId, amount);
                    }
                }

                // Return OK to PayPal
                return Ok(new { message = "Webhook received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing PayPal webhook");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }




            [HttpGet("get-packages")]
            public IActionResult GetPackages()
            {
                try
                {
                    var packages = new List<object>
                    {
                        new { name = "Package 1", price = 10, coins = 10 * 10 },
                        new { name = "Package 2", price = 20, coins = 20 * 10 },
                        new { name = "Package 3", price = 30, coins = 30 * 10 }
                    };

                    return Ok(new
                    {
                        message = "Success",
                        data = packages
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error returning packages");
                    return StatusCode(500, new { message = "Internal server error", error = ex.Message });
                }
            }


    }

}

