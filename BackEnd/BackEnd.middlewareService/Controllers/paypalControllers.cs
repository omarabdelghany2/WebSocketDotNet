using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;



namespace BackEnd.middlewareService.Controllers
{

    [ApiController]
    [Route("api/")]
    public class SubscriptionWebhookController : ControllerBase
    {

        [HttpPost]
        [Route("subscribe")]
        public async Task<IActionResult> subscription([FromHeader] string Authorization, [FromBody] subscribeRequest input)
        {
            try
            {
                // Log the Authorization header
                Console.WriteLine("Authorization Header:");
                Console.WriteLine(Authorization);

                // Log the body data
                Console.WriteLine("Received Body Data:");
                Console.WriteLine($"subscriptionToken: {input.subscriptionToken}");
                Console.WriteLine($"subscriptionId: {input.subscriptionId}");

                // Acknowledge receipt of the data
                return Ok(new { message = "Data received successfully" });
            }
            catch (Exception ex)
            {
                // Log any errors
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost]
        [Route("activate")]
        public async Task<IActionResult> activateSubscription()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

                var subscriptionId = data.GetProperty("subscription_id").GetString();
                var customerId = data.GetProperty("customer_id").GetString();

                Console.WriteLine($"Activating subscription {subscriptionId} for customer {customerId}");

                // Simulate activation logic
                await Task.CompletedTask;

                return Ok(new { message = "Subscription activated successfully" });
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error: Invalid JSON payload. {jsonEx.Message}");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("expire")]
        public async Task<IActionResult> expireSubscription()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

                var subscriptionId = data.GetProperty("subscription_id").GetString();

                Console.WriteLine($"Expiring subscription {subscriptionId}");

                // Simulate expiration logic
                await Task.CompletedTask;

                return Ok(new { message = "Subscription expired successfully" });
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error: Invalid JSON payload. {jsonEx.Message}");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("cancel")]
        public async Task<IActionResult> cancelSubscription()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

                var subscriptionId = data.GetProperty("subscription_id").GetString();

                Console.WriteLine($"Cancelling subscription {subscriptionId}");

                // Simulate cancellation logic
                await Task.CompletedTask;

                return Ok(new { message = "Subscription cancelled successfully" });
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error: Invalid JSON payload. {jsonEx.Message}");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("renewal")]
        public async Task<IActionResult> renewSubscription()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

                var subscriptionId = data.GetProperty("subscription_id").GetString();

                Console.WriteLine($"Renewing subscription {subscriptionId}");

                // Simulate renewal logic
                await Task.CompletedTask;

                return Ok(new { message = "Subscription renewed successfully" });
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Error: Invalid JSON payload. {jsonEx.Message}");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }


    public class subscribeRequest{

        public string subscriptionToken{get;set;}

        public string subscriptionId{get;set;}
    }

}