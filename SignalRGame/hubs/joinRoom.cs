using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> JoinRoom(string token, string roomId)
        {
            // Retrieve the user ID from the token
            if (!TokenToUserId.TryGetValue(token, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return "Error: Invalid token";
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room does not exist.");
                return "Error: Room does not exist";
            }

            // Check if the user is already in the room
            if (room.Participants.Any(p => p.UserId == userId))
            {
                await Clients.Caller.SendAsync("Error", "User already in the room.");
                return "Error: User already in the room";
            }

            // Assign the user to a team (blue if fewer blue players, red otherwise)
            string team = room.Participants.Count(p => p.Team == "Blue") < room.Participants.Count(p => p.Team == "Red") ? "Blue" : "Red";
            var newPlayer = new Player { UserId = userId, Team = team };

            // Add the user to the room
            room.Participants.Add(newPlayer);

            // Add the user to the SignalR group for the room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Notify the room that a new player joined
            await Clients.Group(roomId).SendAsync("PlayerJoined", userId, team);

            return "OK";
        }
    }
}
