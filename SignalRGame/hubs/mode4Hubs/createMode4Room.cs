using SignalRGame.Models;
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {

        // ---- CREATE MODE4 ROOM ----
        public async Task createMode4Room(string Authorization)
        {
            // 1. Get profile from token

            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(Authorization);

            if (serverResponse == "error")
            {
                await Clients.Caller.SendAsync("mode4RoomCreated", new
                {
                    roomId = "",
                    team = "",
                    error = true,
                    errorMessage = "Error retrieving userId; something went wrong with the Token."
                });
                return;
            }

            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;

            // 2. Check if user already has a room
            if (UserRoomMapping.ContainsKey(userId.ToString()))
            {
                await Clients.Caller.SendAsync("mode4RoomCreated", new
                {
                    roomId = "",
                    team = "",
                    error = true,
                    errorMessage = "The Host already has a Room"
                });
                return;
            }

            // 3. Create room
            var roomId = Guid.NewGuid().ToString();

            var hostPlayer = new Player
            {
                userId = userId.ToString(),
                team = "Red", // Or assign dynamically if you want
                profileName = profile?.profileName,
                score = profile?.score ?? 0,
                isBot = false
            };

            var room = new Room
            {
                RoomId = roomId,
                Host = hostPlayer,
                Participants = new List<Player> { hostPlayer },
                publicRoom = false, // default private, since only friends can join
                Mode = "Mode4"
            };

            // 4. Save and map
            Rooms[roomId] = room;
            UserRoomMapping[userId.ToString()] = roomId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // 5. Send response to caller
            await Clients.Caller.SendAsync("mode4RoomCreated", new
            {
                roomId = roomId,
                team = hostPlayer.team,
                error = false,
                errorMessage = "",
                room = room
            });
        }
    }
}
