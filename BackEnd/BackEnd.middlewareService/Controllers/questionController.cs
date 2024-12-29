using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers; // Added for MediaTypeHeaderValue
using System.Text.Json.Serialization;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/")]
    public class QuestionsController : ControllerBase
    {
        // Define the array of categories
        private static readonly string[] Categories = { "sports", "history", "mechanics" };
        private readonly TokenValidator  _TokenValidator;
        private readonly getSubCategoriesService _getSubCategoriesService;

        public QuestionsController(TokenValidator tokenvalid, getSubCategoriesService getSubcateogry)
        {
            _TokenValidator =tokenvalid;
            _getSubCategoriesService=getSubcateogry;
        }

        [HttpGet("get-categories")]
        public async Task<IActionResult> getCategories([FromHeader] string Authorization )
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Refresh Token is required.");
            }
            string result = await _TokenValidator.ValidateTokenAsync(Authorization);

            if (result == "error")
            {
                 return Unauthorized("You are not authorized to get the accessToken list.");
            }

            // Assume Authorization is valid and skip token verification logic for simplicity
            // Normally, validate the token and authenticate the user.

            // Return the array of categories
            return Ok(new
            {
                Categories = Categories
            });
        }

        [HttpGet("get-sub-categories")]
        public async Task<IActionResult> getSubCategories([FromHeader] string Authorization, [FromQuery] string categoryName)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Authorization token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();
            string validationResult = await _TokenValidator.ValidateTokenAsync(token);
            if (validationResult == "error")
            {
                return Unauthorized("You are not authorized to access the categories.");
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                return BadRequest("Category name is required.");
            }

            try
            {
                // Fetch subcategories
                string responseContent = await _getSubCategoriesService.GetSubCategoriesAsync(token, categoryName);

                // Check if the response is valid JSON and parse it
                try
                {
                    var subCategories = JsonSerializer.Deserialize<object>(responseContent);
                    return Ok(new
                    {
                        Message = "Subcategories list fetched successfully",
                        Data = subCategories
                    });
                }
                catch (JsonException)
                {
                    // Handle invalid JSON (indicates an error from the service)
                    return BadRequest(new
                    {
                        Message = "Failed to parse subcategories data",
                        Error = responseContent
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return StatusCode(500, new { Message = "An unexpected error occurred", Exception = ex.Message });
            }
        }





    }



    public class categoryRequest
    {
        public string categoryName { get; set; }

    }
}
