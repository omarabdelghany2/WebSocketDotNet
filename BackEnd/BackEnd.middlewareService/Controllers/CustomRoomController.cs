using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BackEnd.middlewareService.Services;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Models;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/custom-room")]
    public class CustomRoomController : ControllerBase
    {
        private readonly CustomRoomService _customRoomService;

        public CustomRoomController(CustomRoomService customRoomService)
        {
            _customRoomService = customRoomService;
        }




        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomRoom([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool created = await _customRoomService.CreateCustomRoomAsync(token);

            if (created)
                return Ok(new { Message = "Custom room created successfully." });
            else
                return BadRequest(new { Message = "Failed to create custom room." });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCustomRoom([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool deleted = await _customRoomService.DeleteCustomRoomAsync(token);

            if (deleted)
                return Ok(new { Message = "Custom room deleted successfully." });
            else
                return BadRequest(new { Message = "Failed to delete custom room." });
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckCustomRoom([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool hasRoom = await _customRoomService.CheckIfUserHasCustomRoomAsync(token);

            return Ok(new
            {
                Message = "Check completed.",
                HasRoom = hasRoom
            });
        }

        [HttpGet("questions")]
        public async Task<IActionResult> GetCustomRoomQuestions([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool hasRoom = await _customRoomService.CheckIfUserHasCustomRoomAsync(token);

            if (!hasRoom)
            {
                return BadRequest(new { Message = "User does not have a custom room." });
            }

            string result = await _customRoomService.GetCustomRoomQuestionsAsync(token);

            if (result == "error")
            {
                return BadRequest(new { Message = "Error retrieving questions." });
            }

            try
            {
                var questions = JsonSerializer.Deserialize<List<Question>>(result);

                return Ok(new
                {
                    Message = "Questions fetched successfully.",
                    Data = questions
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing questions JSON", Exception = ex.Message });
            }
        }



        [HttpPost("save")]
        public async Task<IActionResult> SaveCustomRoom([FromHeader] string Authorization, [FromBody] List<Question> questions)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool success = await _customRoomService.SaveCustomRoomAsync(token, questions);

            if (success)
            {
                return Ok(new { Message = "Custom room saved successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to save custom room." });
            }
        }







    }
}
