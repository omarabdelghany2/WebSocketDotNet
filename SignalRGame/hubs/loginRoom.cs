using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> LoginRoom(string token)
        {
            // Retrieve the user ID from the token
            if (!TokenToUserId.TryGetValue(token, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return null;
            }

            // Update or add the user to UserIdToConnectionId map
            UserIdToConnectionId[userId] = Context.ConnectionId;

            string roomId;

            // Check if the user already has a login room
            if (LoginRoomMapping.TryGetValue(userId, out var existingRoomId))
            {
                roomId = existingRoomId;

                // Update the SignalR group membership
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, existingRoomId);
                await Groups.AddToGroupAsync(Context.ConnectionId, existingRoomId);

                // Notify the caller that the room was updated
                await Clients.Caller.SendAsync("LoginRoomUpdated", roomId);
            }
            else
            {
                // Generate a new login room ID
                roomId = $"login-{Guid.NewGuid()}";

                // Create the login room with only the host
                Player host = new Player { UserId = userId };

                var room = new Room
                {
                    RoomId = roomId,
                    Host=host
                };

                LoginRooms[roomId] = room; // Save the room in the global Rooms dictionary
                LoginRoomMapping[userId] = roomId;

                // Add the user to the SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Notify the caller that the room was created
                await Clients.Caller.SendAsync("LoginRoomCreated", roomId);
            }

            return roomId;
        }
    }
}