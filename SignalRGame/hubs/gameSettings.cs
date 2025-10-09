using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task gameSettings(gameSettingsRequest request )
        {
            int questionTime = request.questionTime;
            List<string>subCategories= request.subCategories;
            string token=request.token;
            string roomId=request.roomId;
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);

            if (userId == "error")
            {
                await Clients.Caller.SendAsync("gameSettingsChanged", new{error =true ,errorMessage="Error retrieving userId; something went wrong with the Token."});
                return;
            }
            if (userId == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }
            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("gameSettingsChanged", new{error =true ,errorMessage="Room does not exist."});
                return;
            }

            // Find the player in the room's participants
            var player = room.Participants.FirstOrDefault(p => p.userId == userId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("gameSettingsChanged", new{error =true ,errorMessage="Player not found in the room."});
                return;
            }

            // Notify the room that the player has switched teams
            await Clients.OthersInGroup(roomId).SendAsync("gameSettingsChanged", new
            {
                subCategories = subCategories,
                questionTime = questionTime,
                error = false,
                errorMessage = ""
            });

        }

    }




    public class gameSettingsRequest
    {
        public string token { get; set; }
        public int questionTime { get; set; }
        public List <string> subCategories { get; set; }
        public string roomId{get;set;}
    }

}
        