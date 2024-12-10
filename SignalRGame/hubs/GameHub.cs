// using Microsoft.AspNetCore.SignalR;
// using System.Collections.Concurrent;
// using SignalRGame.Models;
// using SignalRGame.Services;
// namespace SignalRGame.Hubs
// {
// public class GameHub : Hub
// {
//     private static readonly Dictionary<string, Room> GameRoom = new();
//     private static readonly Dictionary<string, Room> UserLoginRoom = new();
//     private static readonly ConcurrentDictionary<string, string> UserRoomMapping = new();
//     private static readonly Dictionary<string, string> TokenToUserId = new(); 
//     private static readonly ConcurrentDictionary<string, string> UserIdToConnectionId = new(); 
//     private static readonly ConcurrentDictionary<string, string> LoginRoomMapping = new(); 

//     private readonly ITokenService _tokenService;
//     private readonly IRoomService _roomService;
//     private readonly IInvitationService _invitationService;
//     private readonly IGameService _gameService;

//     public GameHub(ITokenService tokenService, IRoomService roomService, IInvitationService invitationService, IGameService gameService)
//     {
//         _tokenService = tokenService;
//         _roomService = roomService;
//         _invitationService = invitationService;
//         _gameService = gameService;
//     }

//     // OnConnectedAsync and OnDisconnectedAsync will remain the same
//     public override async Task OnConnectedAsync()
//     {
//         // Retrieve the userId from the connection query string
//         string? userId = Context.GetHttpContext()?.Request.Query["userId"];
//         if (!string.IsNullOrEmpty(userId))
//         {
//             // Store the connectionId with the associated userId
//             UserIdToConnectionId[userId] = Context.ConnectionId;
//         }

//         // Call the base method to handle the connection event
//         await base.OnConnectedAsync();
//     }

    
//     public override async Task OnDisconnectedAsync(Exception? exception)
//     {
//         // Find the room associated with this connectionId
//         var userRoom = UserRoomMapping.FirstOrDefault(ur => ur.Value == Context.ConnectionId);
//         if (userRoom.Key != null)
//         {
//             var roomId = userRoom.Value;
//             if (GameRoom.TryGetValue(roomId, out var room))
//             {
//                 room.Participants.Remove(userRoom.Key);

//                 // If the user was the host, remove the room
//                 if (room.Host == userRoom.Key)
//                 {
//                     GameRoom.Remove(roomId);
//                     await Clients.Group(roomId).SendAsync("RoomDeleted");
//                 }
//                 else
//                 {
//                     // Notify other participants that the user left the room
//                     await Clients.Group(roomId).SendAsync("PlayerLeft", userRoom.Key);
//                 }
//             }

//             // Remove user from the UserRoomMapping
//             UserRoomMapping.TryRemove(userRoom.Key, out _);
//         }

//         // Remove the user from the UserIdToConnectionId mapping
//         string? userId = UserIdToConnectionId.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId).Key;
//         if (userId != null)
//         {
//             UserIdToConnectionId.TryRemove(userId, out _);
//         }

//         // Call the base method to handle the disconnection event
//         await base.OnDisconnectedAsync(exception);
//     }


//     public async Task<string> CreateRoom(string token)
//     {
//         return await _roomService.CreateRoom(token, Context);
//     }

//     public async Task<string> LoginRoom(string token)
//     {
//         return await _roomService.LoginRoom(token, Context);
//     }

//     public async Task<string> JoinRoom(string token, string roomId)
//     {
//         return await _roomService.JoinRoom(token, roomId, Context);
//     }

//     public async Task InviteToRoom(string token, string roomId, string invitedUserId)
//     {
//         await _invitationService.InviteToRoom(token, roomId, invitedUserId, Context);
//     }

//     public async Task<string> AcceptInvitation(string token, string roomId, string inviterId)
//     {
//         return await _invitationService.AcceptInvitation(token, roomId, inviterId, Context);
//     }

//     public async Task StartGame(string token, string roomId)
//     {
//         await _gameService.StartGame(token, roomId, Context);
//     }
// }
// }
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    private static readonly Dictionary<string, Room> Rooms = new();
    private static readonly ConcurrentDictionary<string, string> UserRoomMapping = new();
    private static readonly Dictionary<string, string> TokenToUserId = new(); // Token to UserId mapping
    private static readonly ConcurrentDictionary<string, string> UserIdToConnectionId = new(); // UserId to ConnectionId mapping
    private static readonly ConcurrentDictionary<string, string> LoginRoomMapping = new(); // Separate mapping for login rooms


    //some intial data we can clear Then After connection with database
    public GameHub()
    {
        // Populate TokenToUserId with some sample data for testing
        TokenToUserId["token123"] = "user1";
        TokenToUserId["token456"] = "user2";
        TokenToUserId["token789"] = "user3";
    }
    

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




    public async Task InviteToRoom(string token, string roomId, string invitedUserId)
    {
        // Retrieve the inviter's user ID from the token
        if (!TokenToUserId.TryGetValue(token, out var inviterUserId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid token.");
            return;
        }

        // Check if the room exists
        if (!Rooms.TryGetValue(roomId, out var room))
        {
            await Clients.Caller.SendAsync("Error", "Room does not exist.");
            return;
        }

        // Ensure the inviter is the host or a participant of the room
        if (room.Host != inviterUserId && !room.Participants.Contains(inviterUserId))
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this room.");
            return;
        }

        // Check if the invited user is connected (i.e., is in the login room)
        if (LoginRoomMapping.TryGetValue(invitedUserId, out var loginRoomConnectionId))
        {
            // Send an invitation to the invited user's login room (using their user ID or token)
            // await Clients.Group(invitedUserId).SendAsync("RoomInvitation", roomId, inviterUserId);
            await Clients.Group(loginRoomConnectionId).SendAsync("RoomInvitation", roomId, inviterUserId);

        }
        else
        {
            await Clients.Caller.SendAsync("Error", "The invited user is not connected.");
            return;
        }

        // Notify the inviter that the invitation was sent successfully
        await Clients.Caller.SendAsync("InvitationSent", invitedUserId);
    }





    public async Task<string> AcceptInvitation(string token, string roomId, string inviterId)
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

        // Check if the inviter is a participant or the host of the room
        if (room.Host != inviterId && !room.Participants.Contains(inviterId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid inviter.");
            return "Error: Invalid inviter";
        }

        // Check if the user is already in the room
        if (room.Participants.Contains(userId))
        {
            await Clients.Caller.SendAsync("Error", "You are already in the room.");
            return "Error: Already in the room";
        }

        // Add the user to the room
        room.Participants.Add(userId);

        // Add the user to the SignalR group for the room
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Notify the room that a new player joined
        await Clients.Group(roomId).SendAsync("PlayerJoined", userId);

        // Notify the inviter that the invitation was accepted
        if (UserIdToConnectionId.TryGetValue(inviterId, out var inviterConnectionId))
        {
            await Clients.Group(roomId).SendAsync("InvitationAccepted", roomId, userId);
        }

        return "OK";
    }


        public async Task StartGame(string token, string roomId)
    {
        // Retrieve the user ID from the token
        if (!TokenToUserId.TryGetValue(token, out var userId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid token.");
            return;
        }

        // Check if the room exists
        if (!Rooms.TryGetValue(roomId, out var room))
        {
            await Clients.Caller.SendAsync("Error", "Room does not exist.");
            return;
        }

        // Verify the user is the host of the room
        if (room.Host != userId)
        {
            await Clients.Caller.SendAsync("Error", "Only the host can start the game.");
            return;
        }

        // Notify all participants that the game has started
        await Clients.Group(roomId).SendAsync("GameStarted");

        // Start the countdown timer
        await RunCountdown(roomId); // Await the countdown task
    }
    private async Task RunCountdown(string roomId)
    {
        try
        {
            // Perform 5 countdown steps (10 seconds each)
            for (int i = 5; i > 0; i--)
            {
                // Notify the room about the countdown
                await Clients.Group(roomId).SendAsync("CountdownTick", i * 10);

                // Wait for 10 seconds before the next tick
                await Task.Delay(10000);
            }

            // Notify the room that the countdown is over
            await Clients.Group(roomId).SendAsync("CountdownComplete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RunCountdown: {ex.Message}");
            // Optionally notify the host or participants of the error
            await Clients.Group(roomId).SendAsync("Error", "Countdown failed.");
        }
    }



}

public class Room
{
    public string RoomId { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
}