using SignalRGame.Models;
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
        // ---- CREATE GUEST ROOM ----
        public async Task createGuestRoom()
        {
            var roomId = Guid.NewGuid().ToString();

            var guestPlayer = new Player
            {
                userId = Guid.NewGuid().ToString(), // Random guest ID
                team = "Blue",
                profileName = "Guest",
                score = 0,
                isBot = false
            };

            var room = new Room
            {
                RoomId = roomId,
                Host = guestPlayer,
                Participants = new List<Player> { guestPlayer },
                publicRoom = false
            };

            Rooms[roomId] = room;
            UserRoomMapping[guestPlayer.userId] = roomId;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            await Clients.Caller.SendAsync("guestRoomCreated", new { roomId = roomId, team = "Blue", error = false, errorMessage = "" });
        }


    }
}
