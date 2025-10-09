using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Threading.Tasks;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task createCustomRoom(string authorization)
        {
            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(authorization);
            
            if (serverResponse == "error")
            {
                await Clients.Caller.SendAsync("customRoomCreated", new { roomId = "", team = "", error = true, errorMessage = "Invalid token." });
                return;
            }

            if (serverResponse == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }

            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;

            if (!await _isSubscribedService.isSubscribedAsync(authorization))
            {
                await Clients.Caller.SendAsync("customRoomCreated", new { roomId = "", team = "", error = true, errorMessage = "User is not subscribed." });
                return;
            }

            if (UserRoomMapping.ContainsKey(userId.ToString()))
            {
                await Clients.Caller.SendAsync("customRoomCreated", new { roomId = "", team = "", error = true, errorMessage = "User already has a room." });
                return;
            }

            var roomId = Guid.NewGuid().ToString();

            var room = new Room
            {
                RoomId = roomId,
                Host = new Player
                {
                    userId = userId.ToString(),
                    team = "Blue",
                    profileName = profile?.profileName,
                    score = profile?.score ?? 0
                },
                Participants = new List<Player>
                {
                    new Player
                    {
                        userId = userId.ToString(),
                        team = "Blue",
                        profileName = profile?.profileName,
                        score = profile?.score ?? 0
                    }
                },
                publicRoom = false,
                Mode= "mode3",
            };

            Rooms[roomId] = room;
            UserRoomMapping[userId.ToString()] = roomId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            await Clients.Caller.SendAsync("customRoomCreated", new { roomId = roomId, team = "Blue", error = false, errorMessage = "" });
        }
    }
}
