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



        public class CreateRoomRequest
        {
            public int userId { get; set; }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomRoom([FromHeader] string Authorization, [FromBody] CreateRoomRequest request)
        {
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool created = await _customRoomService.CreateCustomRoomAsync(request.userId, token);

            if (created)
                return Ok(new { Message = "Custom room created successfully." });
            else
                return BadRequest(new { Message = "Failed to create custom room." });
        }



        public class DeleteRoomRequest
        {
            public int userId { get; set; }
            public int roomId { get; set; }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCustomRoom([FromHeader] string Authorization,[FromBody] DeleteRoomRequest request){
            if (string.IsNullOrEmpty(Authorization))
                return BadRequest("Token is required.");

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool deleted = await _customRoomService.DeleteCustomRoomAsync(request.userId, request.roomId, token);

            if (deleted)
                return Ok(new { Message = "Custom room deleted successfully." });
            else
                return BadRequest(new { Message = "Failed to delete custom room." });
        }

       
       public class CheckRoomRequest
        {
            public int userId { get; set; }
        }

            
        [HttpPost("check")]
        public async Task<IActionResult> CheckCustomRoom([FromHeader] string Authorization,[FromBody] CheckRoomRequest request)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool hasRoom = await _customRoomService.CheckIfUserHasCustomRoomAsync(request.userId, token);

            return Ok(new
            {
                Message = "Check completed.",
                HasRoom = hasRoom
            });
        }
        public class GetQuestionsRequest
        {
            public int userId { get; set; }
        }


        [HttpPost("rooms")]
        public async Task<IActionResult> GetCustomRooms([FromHeader] string Authorization, [FromBody] GetQuestionsRequest request)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            string result = await _customRoomService.GetCustomRoomDetailsAsync(request.userId, token);

            if (result == "error")
            {
                return BadRequest(new { Message = "Error retrieving room details." });
            }

            try
            {
                // Parse the response just to verify it's valid JSON
                using var doc = JsonDocument.Parse(result);

                // Instead of picking questions, just return the full JSON
                var allRooms = JsonSerializer.Deserialize<object>(result);

                return Ok(new
                {
                    Message = "Rooms fetched successfully.",
                    Data = allRooms
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing room details JSON", Exception = ex.Message });
            }
        }

        public class SaveRoomRequest
        {
            public int userId { get; set; }
            public int roomId { get; set; }
            public List<Question> Questions { get; set; }
        }


        [HttpPost("save")]
        public async Task<IActionResult> SaveCustomRoom([FromHeader] string Authorization,[FromBody] SaveRoomRequest request)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            bool success = await _customRoomService.AddQuestionsToCustomRoomAsync(
                request.userId,
                request.roomId,
                token,
                request.Questions
            );

            if (success)
            {
                return Ok(new { Message = "Custom room updated successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to update custom room." });
            }
        }



    }
}
