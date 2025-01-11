
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using SignalRGame.Models;
using System.Threading.Tasks;
using SignalRGame.Hubs;
using SignalRGame.Services;
using System.Text.Json;

using System.Text.Json.Serialization;
namespace SignalRGame.Hubs
{
    public partial  class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Room> Rooms = new();
        private static readonly Dictionary <string ,Room>LoginRooms = new();
        private static readonly ConcurrentDictionary<string, string> UserRoomMapping = new();
        private static readonly ConcurrentDictionary<string, string> ParticipantRoomMapping = new();
        private static readonly Dictionary<string, string> TokenToUserId = new(); // Token to UserId mapping
        private static readonly ConcurrentDictionary<string, string> UserIdToConnectionId = new(); // UserId to ConnectionId mapping
        private static readonly ConcurrentDictionary<string, string> LoginRoomMapping = new(); // Separate mapping for login rooms
        private static readonly ConcurrentDictionary<string,List<Question>> RoomToQuestions =new();//saves the Question in it with the Room Key When You Recieve it from Database in iT
        private static readonly ConcurrentDictionary<string,Question> RoomToCurrentQuestion =new();  //this for me to handel the Answers for it
        private readonly getQuestionsService _GetQuestions;
        private readonly GameService _gameService;
        private readonly userIdFromTokenService _userIdFromTokenService;
        private readonly FriendsService _FriendsService;
        private readonly userProfileFromTokenService _userProfileFromTokenService;
        private readonly GetFriendsByIdService  _GetFriendsByIdService;
        private readonly userIdFromProfileNameService _userIdFromProfileNameService;
        private static  string recentNewsLetter;
        //add the variables of questions
        private readonly HttpClient _httpClient;
        public GameHub(GameService gameService ,getQuestionsService getQuestions ,userIdFromTokenService userIdFromToken ,FriendsService friendsService , userProfileFromTokenService userProfile ,GetFriendsByIdService userFriendsById ,userIdFromProfileNameService userIdfromProfile)
        {
                    // Add questions directly to the dictionary

            _gameService = gameService;
            _GetQuestions =getQuestions;
            _userIdFromTokenService=userIdFromToken;
            _FriendsService=friendsService;
            _userProfileFromTokenService=userProfile;
            _GetFriendsByIdService=userFriendsById;
            _userIdFromProfileNameService=userIdfromProfile;
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
                await Clients.Group(roomId).SendAsync("PlayerTeam", player.userId, player.team);
            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Find the user based on the connection ID
            string? userId = UserIdToConnectionId.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId).Key;

            if (!string.IsNullOrEmpty(userId))
            {
                // Step 1: Handle disconnection from UserIdToConnectionId map
                HandleUserDisconnection(userId);

                // Step 2: Handle disconnection from login room
                await HandleLoginRoomDisconnection(userId);

                // Step 3: Handle disconnection from created or joined room
                bool isHost= await HandleRoomDisconnection(userId);
                if(!isHost){
                        await HandleParticipantRoomDisconnection(userId);
                }
                string error = await HandleNotfiyFriendsOfDisconnection(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }




        public async Task logOut(logoutRequest request){
            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(request.token);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0; // Default to 0 if profile.id is null


                    // Step 1: Handle disconnection from UserIdToConnectionId map
            HandleUserDisconnection(userId.ToString());

            // Step 2: Handle disconnection from login room
            await HandleLoginRoomDisconnection(userId.ToString());

            // Step 3: Handle disconnection from created or joined room
            bool isHost= await HandleRoomDisconnection(userId.ToString());
            if(!isHost){
                    await HandleParticipantRoomDisconnection(userId.ToString());
            }
            string error = await HandleNotfiyFriendsOfDisconnection(userId.ToString());
        }


        private void HandleUserDisconnection(string userId)
        {
            // Remove the user from the UserIdToConnectionId map
            if (UserIdToConnectionId.TryRemove(userId, out _))
            {
                Console.WriteLine($"Removed user {userId} from UserIdToConnectionId map successfully.");
            }
        }

        private async Task HandleLoginRoomDisconnection(string userId)
        {
            if (LoginRoomMapping.TryGetValue(userId, out var userLoginRoom) && !string.IsNullOrEmpty(userLoginRoom))
            {
                // Set the login room to null and remove from the mapping
                LoginRooms[userLoginRoom] = null;
                LoginRoomMapping.TryRemove(userId, out _);
                Console.WriteLine($"Removed user {userId} from their login room successfully.");
            }

            await Task.CompletedTask;
        }

        private async Task<bool> HandleRoomDisconnection(string userId)
        {
            // Check if the user is associated with a room
            if (UserRoomMapping.TryGetValue(userId, out var roomId) && !string.IsNullOrEmpty(roomId))
            {
                // Check if the room exists
                if (Rooms.TryGetValue(roomId, out var room))
                {
                    bool isHost = room.Host.userId == userId;

                    // Remove the user from the participants list
                    var player = room.Participants.FirstOrDefault(p => p.userId == userId);
                    if (player != null)
                    {
                        room.Participants.Remove(player);
                    }

                    // Handle host-specific logic
                    if (isHost)
                    {
                        room.Host = null; // Remove host
                        if (room.Participants.Count > 0)
                        {
                            // Assign a new host from participants
                            room.Host = room.Participants.First();
                            UserRoomMapping[room.Host.userId] = roomId;
                            Console.WriteLine($"Host left; reassigned new host: {room.Host.userId}");
                            await Clients.Group(roomId).SendAsync("hostLeft", new { hostId=Convert.ToInt32(player.userId),team=player.team,newHostId = Convert.ToInt32(room.Host.userId)});
                        }
                        else
                        {
                            // No participants left, delete the room
                            Rooms.TryRemove(roomId, out _);
                            Console.WriteLine("Room deleted as the host and participants left.");
                            await Clients.Group(roomId).SendAsync("roomDeleted");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Participant {userId} left room {roomId}.");
                    }

                    // Remove the mapping for the user
                    UserRoomMapping.TryRemove(userId, out _);

                    // Successfully disconnected
                    return true;
                }
            }

            // If userId is not part of any room, return false
            Console.WriteLine($"User {userId} was not Host of any room.");
            return false;
        }

        private async Task HandleParticipantRoomDisconnection(string userId)
        {
            // Check if the user is associated with a room
            if (ParticipantRoomMapping.TryGetValue(userId, out var roomId) && !string.IsNullOrEmpty(roomId))
            {
                // Check if the room exists
                if (Rooms.TryGetValue(roomId, out var room))
                {

                    // Remove the user from the participants list
                    var player = room.Participants.FirstOrDefault(p => p.userId == userId);
                    if (player != null)
                    {
                        room.Participants.Remove(player);
                    }

                    await Clients.Group(roomId).SendAsync("playerLeft" ,new{userId=Convert.ToInt32(userId),team=player.team});                       
                    Console.WriteLine($"Participant {userId} left room {roomId}.");
                

                    // Remove the mapping for the user
                    ParticipantRoomMapping.TryRemove(userId, out _);

                    // Successfully disconnected
                    
                }
            }

            // If userId is not part of any room, return false
            Console.WriteLine($"User {userId} was not particpant of any room.");
            
        }


        private async Task<string> HandleNotfiyFriendsOfDisconnection(string userId){
                        // Fetch the friends list from the service
            var friendsListJson = await _GetFriendsByIdService.GetFriendsByIdAsync(Convert.ToInt32(userId));

            if (!string.IsNullOrEmpty(friendsListJson))
            {


                // Log the raw response for debugging purposes

                Console.WriteLine("entered HandleNotifyofDiscnnection");
                Console.WriteLine($"Received friendsListJson: {friendsListJson}");

                // Deserialize the JSON response into a list of Friend objects
                try
                {
                    List<Friend> friendsList = JsonSerializer.Deserialize<List<Friend>>(friendsListJson);
                    
                    if (friendsList == null || !friendsList.Any())
                    {
                        Console.WriteLine($"No friends found");
                        return"didntFindAnyFriendsAfterSerialization";
                    }

                    // Loop through each friend in the list
                    foreach (var friend in friendsList)
                    {
                        int friendId = friend.friendId;  // Now friendId is an int
                        string profileName = friend.profileName;  // Get the profile name
                        int friendScore = friend.friendScore;  // Parse friendScore if it's a string

                        Console.WriteLine($"Friend ID: {friendId}, Profile Name: {profileName}, Score: {friendScore}");

                        // Check if the friend is logged in (by checking LoginRoomMapping)
                        if (LoginRoomMapping.ContainsKey(friendId.ToString()))
                        {
                            // Get the roomId from the LoginRoomMapping using the friend's ID
                            string roomId = LoginRoomMapping[friendId.ToString()];
                            Console.WriteLine($"Friend {profileName} is online, roomId: {roomId}");

                            // Send a message to the room (you can customize the message)
                            await Clients.Group(roomId).SendAsync("connectionStatus", new { userId = userId, status = false });
                            return"sent The Status";
                        }
                    }
                    return"sent The Status";


                }
                catch (JsonException ex)
                {
                    // Handle JSON deserialization error
                    Console.WriteLine($"Error deserializing friends list: {ex.Message}");
                    return"asd";

                }
                
            }
            else{
                return"didnt receive any Friends From Database";
            }
        }

    }


        public class Friend
    {
        [JsonPropertyName("friend_id")]  // Mapping snake_case to PascalCase
        public int friendId { get; set; }  // Ensure this is an int for numeric friend_id

        [JsonPropertyName("profile_name")]  // Mapping snake_case to PascalCase
        public string profileName { get; set; }

        [JsonPropertyName("friend_score")]  // Mapping snake_case to PascalCase
        public int friendScore { get; set; }  // Change this from string to int
    }




        public class logoutRequest
    {
        public string token { get; set; }
    }
}









