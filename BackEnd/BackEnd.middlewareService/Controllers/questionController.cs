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
        private readonly TokenValidator _TokenValidator;
        private readonly getSubCategoriesService _getSubCategoriesService;

        public QuestionsController(TokenValidator tokenvalid, getSubCategoriesService getSubcateogry)
        {
            _TokenValidator = tokenvalid;
            _getSubCategoriesService = getSubcateogry;
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

            try
            {
                var token = Authorization.Substring("Bearer ".Length).Trim();
                var categories = await _getSubCategoriesService.GetParentCategoriesAsync(token);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to fetch categories", Error = ex.Message });
            }
        }

        [HttpGet("get-sub-categories/{categoryId}")]
        public async Task<IActionResult> getSubCategories([FromHeader] string Authorization, int categoryId)
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

            try
            {
                var categoryDetails = await _getSubCategoriesService.GetCategoryDetailsAsync(token, categoryId);
                return Ok(new
                {
                    Message = "Category details fetched successfully",
                    Data = categoryDetails
                });
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return StatusCode(500, new { Message = "An unexpected error occurred", Exception = ex.Message });
            }
        }





    }




}
