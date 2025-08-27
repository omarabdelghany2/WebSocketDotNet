using Microsoft.AspNetCore.Mvc;
using BackEnd.middlewareService.Services;
using BackEnd.middlewareService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;


namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoreController : ControllerBase
    {
        private readonly StoreService _storeService;
        private readonly ILogger<StoreController> _logger;

        public StoreController(StoreService storeService, ILogger<StoreController> logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        // GET: api/store/avatars
        [HttpGet("avatars")]
        public async Task<IActionResult> GetAllAvatars([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            var avatars = await _storeService.GetAllAvatarsAsync(token);
            return Ok(new
            {
                Message = "Avatars retrieved successfully.",
                Data = avatars
            });
        }

        // POST: api/store/purchase
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseAvatar([FromHeader] string Authorization, [FromBody] int avatarId)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Step 1: Get user balance
            decimal userBalance = await _storeService.GetUserBalanceAsync(token);

            // Step 2: Get avatar price
            decimal avatarPrice = await _storeService.GetAvatarPriceAsync(token, avatarId);

            // Step 3: Check balance
            if (userBalance < avatarPrice)
            {
                return BadRequest(new
                {
                    Message = "Insufficient balance to purchase this avatar.",
                    CurrentBalance = userBalance,
                    AvatarPrice = avatarPrice
                });
            }

            // Step 4: Proceed to purchase
            var result = await _storeService.PurchaseAvatarAsync(token, avatarId);

            if (!result.Success)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new
            {
                Message = "Avatar purchased successfully.",
                RemainingBalance = result.NewBalance
            });
        }


        // PUT: api/store/change-avatar
        [HttpPut("change-avatar")]
        public async Task<IActionResult> ChangeAvatar([FromHeader] string Authorization, [FromBody] int avatarId)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            var success = await _storeService.ChangeAvatarAsync(token, avatarId);

            if (!success)
                return BadRequest(new { Message = "Failed to change avatar." });

            return Ok(new { Message = "Avatar changed successfully." });
        }

        // POST: api/store/paypal-webhook
        [HttpPost("paypal-webhook")]
        public async Task<IActionResult> PayPalWebhook([FromBody] JsonElement webhookEvent)
        {

            try
            {
                var resource = webhookEvent.GetProperty("resource");

                string customId = resource.GetProperty("custom_id").GetString(); // this is your app-specific token
                string amount = resource.GetProperty("amount").GetProperty("value").GetString();

                _logger.LogInformation("Extracted Token: {Token}, Amount: {Amount}", customId, amount);

                // Optional: parse amount as decimal
                if (!decimal.TryParse(amount, out var parsedAmount))
                {
                    _logger.LogWarning("Invalid amount format.");
                    return BadRequest("Invalid amount.");
                }

                // Call your store service
                var success = await _storeService.AddBalanceFromPayPalAsync(customId, parsedAmount);
                if (!success)
                    return StatusCode(500, new { Message = "Failed to add balance." });

                return Ok(new { Message = "Balance updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process PayPal webhook.");
                return BadRequest("Invalid webhook payload.");
            }
        }

    }
}
