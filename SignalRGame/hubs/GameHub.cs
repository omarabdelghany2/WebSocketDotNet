using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    private static readonly Dictionary<string, Room> Rooms = new();

    public async Task CreateRoom(string roomId)
    {
        if (Rooms.ContainsKey(roomId))
        {
            await Clients.Caller.SendAsync("Error", "Room already exists.");
            return;
        }

        Rooms[roomId] = new Room
        {
            RoomId = roomId,
            Host = Context.ConnectionId,
            Participants = new List<string> { Context.ConnectionId }
        };

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Caller.SendAsync("RoomCreated", roomId);
    }

    public async Task JoinRoom(string roomId)
    {
        if (!Rooms.TryGetValue(roomId, out var room))
        {
            await Clients.Caller.SendAsync("Error", "Room does not exist.");
            return;
        }

        room.Participants.Add(Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("PlayerJoined", Context.ConnectionId);
    }

    public async Task SendMessageToRoom(string roomId, string message)
    {
        if (!Rooms.ContainsKey(roomId))
        {
            await Clients.Caller.SendAsync("Error", "Room does not exist.");
            return;
        }

        await Clients.Group(roomId).SendAsync("ReceiveMessage", Context.ConnectionId, message);
    }

    public async Task StartGame(string roomId)
    {
        if (Rooms.TryGetValue(roomId, out var room) && room.Host == Context.ConnectionId)
        {
            await Clients.Group(roomId).SendAsync("GameStarted");
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Only the host can start the game.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = Rooms.Values.FirstOrDefault(r => r.Participants.Contains(Context.ConnectionId));
        if (room != null)
        {
            room.Participants.Remove(Context.ConnectionId);
            if (room.Host == Context.ConnectionId)
            {
                Rooms.Remove(room.RoomId);
                await Clients.Group(room.RoomId).SendAsync("RoomDeleted");
            }
            else
            {
                await Clients.Group(room.RoomId).SendAsync("PlayerLeft", Context.ConnectionId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}

public class Room
{
    public string RoomId { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
}
