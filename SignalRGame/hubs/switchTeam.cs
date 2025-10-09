using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task switchTeam(SwitchTeamRequest request )
        {
            string token = request.token;
            string roomId = request.roomId;
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("playerTeamChanged", new{team ="" , userId=0, error =true ,errorMessage="Error retrieving userId; something went wrong with the Token."});
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
                await Clients.Caller.SendAsync("playerTeamChanged", new { team = "", userId = 0, error = true, errorMessage = "Room does not exist." });
                return;
            }

            // Find the player in the room's participants
            var player = room.Participants.FirstOrDefault(p => p.userId == userId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("playerTeamChanged", new{team ="" , userId=0 , error =true ,errorMessage="Player not found in the room."});
                return;
            }

            // Switch the player's team
            if (player.team == "Blue")
            {
                player.team = "Red";
            }
            else if (player.team == "Red")
            {
                player.team = "Blue";
            }
            else
            {
                await Clients.Caller.SendAsync("playerTeamChanged", new{team ="" ,userId=0 , error =true ,errorMessage="Player is not assigned to a valid team"});
                return;
            }

            // Notify the room that the player has switched teams
            await Clients.Group(roomId).SendAsync("playerTeamChanged", new{team = player.team , userId = Convert.ToInt32(userId) ,error = false ,errorMessage=""});

        }

    }




    public class SwitchTeamRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }
    }

}
        