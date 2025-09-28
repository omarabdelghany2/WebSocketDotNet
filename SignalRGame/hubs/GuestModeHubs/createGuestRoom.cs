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

        // Create bots
        var bot1 = new Player { userId = "bot1", team = "Blue", profileName = "Bot 1", score = 100, isBot = true };
        var bot2 = new Player { userId = "bot2", team = "Red", profileName = "Bot 2", score = 200, isBot = true };
        var bot3 = new Player { userId = "bot3", team = "Red", profileName = "Bot 3", score = 300, isBot = true };

        var room = new Room
        {
            RoomId = roomId,
            Host = guestPlayer,
            Participants = new List<Player> { guestPlayer, bot1, bot2, bot3 },
            publicRoom = false
        };

        Rooms[roomId] = room;
        UserRoomMapping[guestPlayer.userId] = roomId;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Send room object with bots included
        await Clients.Caller.SendAsync("guestRoomCreated", new 
        { 
            roomId = roomId, 
            team = "Blue", 
            error = false, 
            errorMessage = "", 
            room = room  // 👈 sending entire room including bots
        });
    }



    }
}
