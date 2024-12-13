
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using SignalRGame.Models;
using System.Threading.Tasks;
using SignalRGame.Hubs;
using SignalRGame.Services;
namespace SignalRGame.Hubs
{
    public partial  class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Room> Rooms = new();
        private static readonly Dictionary <string ,Room>LoginRooms = new();
        private static readonly ConcurrentDictionary<string, string> UserRoomMapping = new();
        private static readonly Dictionary<string, string> TokenToUserId = new(); // Token to UserId mapping
        private static readonly ConcurrentDictionary<string, string> UserIdToConnectionId = new(); // UserId to ConnectionId mapping
        private static readonly ConcurrentDictionary<string, string> LoginRoomMapping = new(); // Separate mapping for login rooms
        private static readonly ConcurrentDictionary<string,List<Question>> RoomToQuestions =new();//saves the Question in it with the Room Key When You Recieve it from Database in iT
        private static readonly ConcurrentDictionary<string,Question> RoomToCurrentQuestion =new();  //this for me to handel the Answers for it
        private readonly GameService _gameService;

        public GameHub(GameService gameService)
        {
            // Populate TokenToUserId with some sample data for testing
            TokenToUserId["token123"] = "user1";
            TokenToUserId["token456"] = "user2";
            TokenToUserId["token789"] = "user3";     
                    // Add questions directly to the dictionary
            selectQuestionsCategory("room1");
            selectQuestionsCategory("room2");
            _gameService = gameService;
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
            // Find the user based on the connection ID
            var userRoom = UserRoomMapping.FirstOrDefault(ur => ur.Value == Context.ConnectionId);
            if (userRoom.Key != null)
            {
                var roomId = userRoom.Value;
                if (Rooms.TryGetValue(roomId, out var room))
                {
                    // Find the player in the participants list
                    var player = room.Participants.FirstOrDefault(p => p.UserId == userRoom.Key);

                    if (player != null)
                    {
                        // Remove the player from the room's participants list
                        room.Participants.Remove(player);

                        // Check if the player is the host
                        if (room.Host.UserId == userRoom.Key)
                        {
                            // If the host leaves, remove the room and notify participants
                            if (room.Participants.Count > 0)
                            {
                                room.Host = room.Participants.First(); // Assign the new host to the first participant
                            }
                            else
                            {
                                Rooms[roomId]=null; // Remove the room if there are no participants left
                                await Clients.Group(roomId).SendAsync("RoomDeleted");
                            }
                        }
                        else
                        {
                            // Notify the room that a player left
                            await Clients.Group(roomId).SendAsync("PlayerLeft", userRoom.Key);
                        }
                    }
                }

                // Remove the mapping for the user from the room
                UserRoomMapping.TryRemove(userRoom.Key, out _);
            }

            // Remove from UserIdToConnectionId mapping
            string? userId = UserIdToConnectionId.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId).Key;
            if (userId != null)
            {
                UserIdToConnectionId.TryRemove(userId, out _);
            }

            await base.OnDisconnectedAsync(exception);
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


        public async Task NotifyPlayerTeam(string roomId)
        {
            var room = Rooms[roomId];
            foreach (var player in room.Participants)
            {
                await Clients.Group(roomId).SendAsync("PlayerTeam", player.UserId, player.Team);
            }
        }


        

    }
}









