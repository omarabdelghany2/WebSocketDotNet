using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    private static readonly Dictionary<string, Room> Rooms = new();
    private static readonly ConcurrentDictionary<string, string> UserRoomMapping = new();
    private static readonly Dictionary<string, string> TokenToUserId = new(); // Token to UserId mapping
    private static readonly ConcurrentDictionary<string, string> UserIdToConnectionId = new(); // UserId to ConnectionId mapping
    private static readonly ConcurrentDictionary<string, string> LoginRoomMapping = new(); // Separate mapping for login rooms

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.GetHttpContext()?.Request.Query["userId"];
        if (!string.IsNullOrEmpty(userId))
        {
            UserIdToConnectionId[userId] = Context.ConnectionId;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userRoom = UserRoomMapping.FirstOrDefault(ur => ur.Value == Context.ConnectionId);
        if (userRoom.Key != null)
        {
            var roomId = userRoom.Value;
            if (Rooms.TryGetValue(roomId, out var room))
            {
                room.Participants.Remove(userRoom.Key);

                if (room.Host == userRoom.Key)
                {
                    Rooms.Remove(roomId);
                    await Clients.Group(roomId).SendAsync("RoomDeleted");
                }
                else
                {
                    await Clients.Group(roomId).SendAsync("PlayerLeft", userRoom.Key);
                }
            }

            UserRoomMapping.TryRemove(userRoom.Key, out _);
        }

        // Remove from UserIdToConnectionId
        string? userId = UserIdToConnectionId.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId).Key;
        if (userId != null)
        {
            UserIdToConnectionId.TryRemove(userId, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

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

        // Create the room with the user as the host
        var room = new Room
        {
            RoomId = roomId,
            Host = userId,
            Participants = new List<string> { userId } // Add the host as the first participant
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
            var room = new Room
            {
                RoomId = roomId,
                Host = userId
            };

            Rooms[roomId] = room; // Save the room in the global Rooms dictionary
            LoginRoomMapping[userId] = roomId;

            // Add the user to the SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Notify the caller that the room was created
            await Clients.Caller.SendAsync("LoginRoomCreated", roomId);
        }

        return roomId;
    }


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
        if (room.Participants.Contains(userId))
        {
            await Clients.Caller.SendAsync("Error", "User already in the room.");
            return "Error: User already in the room";
        }

        // Add the user to the room
        room.Participants.Add(userId);

        // Add the user to the SignalR group for the room
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Notify the room that a new player joined
        await Clients.Group(roomId).SendAsync("PlayerJoined", userId);

        return "OK";
    }


    public async Task SendMessageToRoom(string roomId, string userId, string message)
    {
        if (!Rooms.ContainsKey(roomId))
        {
            await Clients.Caller.SendAsync("Error", "Room does not exist.");
            return;
        }

        await Clients.Group(roomId).SendAsync("ReceiveMessage", userId, message);
    }

    public async Task StartGame(string roomId, string userId)
    {
        if (Rooms.TryGetValue(roomId, out var room) && room.Host == userId)
        {
            await Clients.Group(roomId).SendAsync("GameStarted");
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Only the host can start the game.");
        }
    }

    // Associate a token with a user ID
    public void RegisterToken(string token, string userId)
    {
        TokenToUserId[token] = userId;
    }

    // Get user ID from a token
    public string? GetUserIdFromToken(string token)
    {
        return TokenToUserId.TryGetValue(token, out var userId) ? userId : null;
    }
}

public class Room
{
    public string RoomId { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
}
