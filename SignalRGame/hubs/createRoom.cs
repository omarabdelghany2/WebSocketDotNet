using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task createRoom(string Authorization)
        {
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(Authorization);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("roomCreated", new{roomId="",team ="" ,error =true ,errorMessage="Error retrieving userId; something went wrong with the Token."});
                
            }

            // Check if the user already has a room
            if (UserRoomMapping.ContainsKey(userId))
            {
                await Clients.Caller.SendAsync("roomCreated", new{roomId="",team ="" ,error =true ,errorMessage="the Host is already has a Room"});
                
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
            Console.WriteLine(roomId);
            await Clients.Caller.SendAsync("roomCreated", new{roomId=roomId,team ="Blue" ,error =false ,errorMessage=""});

        }

    }
}