using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task SwitchTeam(string token, string roomId)
        {
            // Retrieve the user ID from the token
            if (!TokenToUserId.TryGetValue(token, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return;
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room does not exist.");
                return;
            }

            // Find the player in the room's participants
            var player = room.Participants.FirstOrDefault(p => p.UserId == userId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found in the room.");
                return;
            }

            // Switch the player's team
            if (player.Team == "Blue")
            {
                player.Team = "Red";
            }
            else if (player.Team == "Red")
            {
                player.Team = "Blue";
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Player is not assigned to a valid team.");
                return;
            }

            // Notify the room that the player has switched teams
            await Clients.Group(roomId).SendAsync("PlayerTeamChanged", userId, player.Team);

            // Send the updated team back to the player
            await Clients.Caller.SendAsync("TeamSwitched", player.Team);  // Inform the client about the team switch
        }
    }
}
        