using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> CreateRoom(string token)
        {
            // Retrieve the user ID from the token
            if (!TokenToUserId.TryGetValue(token, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return null;
            }

            // Check if the user already has a room
            if (UserRoomMapping.ContainsKey(userId))
            {
                await Clients.Caller.SendAsync("Error", "User already has a room.");
                return null;
            }

            // Generate a unique room ID
            var roomId = Guid.NewGuid().ToString();

            // Create the room with the user as the host (assigned to blue team by default)
            var room = new Room
            {
                RoomId = roomId,
                Host = new Player { UserId = userId, Team = "Blue" },
                Participants = new List<Player> { new Player { UserId = userId, Team = "Blue" } }
            };

            // Save the room
            Rooms[roomId] = room;
            UserRoomMapping[userId] = roomId;

            // Add the connection to the SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Notify the caller that the room has been created
            await Clients.Caller.SendAsync("RoomCreated", roomId);

            return roomId; // Return the room ID
        }

    }
}