
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    private static readonly Dictionary<string, Room> Rooms = new();
    private static readonly Dictionary <string ,Room>LoginRooms = new();
    private static readonly ConcurrentDictionary<string, string> UserRoomMapping = new();
    private static readonly Dictionary<string, string> TokenToUserId = new(); // Token to UserId mapping
    private static readonly ConcurrentDictionary<string, string> UserIdToConnectionId = new(); // UserId to ConnectionId mapping
    private static readonly ConcurrentDictionary<string, string> LoginRoomMapping = new(); // Separate mapping for login rooms
    private static readonly ConcurrentDictionary<string,List<Question>> RoomToQuestions =new();//saves the Question in it with the Room Key When You Recieve it from Database in iT
    private static readonly ConcurrentDictionary<string,Question> RoomToCurrentQuestion =new();  //this for me to handel the Answers for it


    //some intial data we can clear Then After connection with database
    public GameHub()
    {
        // Populate TokenToUserId with some sample data for testing
        TokenToUserId["token123"] = "user1";
        TokenToUserId["token456"] = "user2";
        TokenToUserId["token789"] = "user3";     
                // Add questions directly to the dictionary
        AddRoomWithQuestions("room1");
        AddRoomWithQuestions("room2");

    }
        private static void AddRoomWithQuestions(string roomId)
    {
        // Directly creating a list of questions for the room
        var roomQuestions = new List<Question>
        {
            new Question
            {
                QuestionTitle = "What is the capital of France?",
                Answers = new[] { "Paris", "London", "Berlin", "Madrid" },
                CorrectAnswer = "Paris"
            },
            new Question
            {
                QuestionTitle = "What is 2 + 2?",
                Answers = new[] { "3", "4", "5", "6" },
                CorrectAnswer = "4"
            },
            new Question
            {
                QuestionTitle = "What is the largest planet in our Solar System?",
                Answers = new[] { "Earth", "Mars", "Jupiter", "Venus" },
                CorrectAnswer = "Jupiter"
            }
        };

        // Add the questions list directly to the dictionary for the room
        RoomToQuestions.TryAdd(roomId, roomQuestions);
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
                            Rooms.Remove(roomId); // Remove the room if there are no participants left
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
            Player host = new Player { UserId = userId };

            var room = new Room
            {
                RoomId = roomId,
                Host=host
            };

            LoginRooms[roomId] = room; // Save the room in the global Rooms dictionary
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
        if (room.Participants.Any(p => p.UserId == userId))
        {
            await Clients.Caller.SendAsync("Error", "User already in the room.");
            return "Error: User already in the room";
        }

        // Assign the user to a team (blue if fewer blue players, red otherwise)
        string team = room.Participants.Count(p => p.Team == "Blue") < room.Participants.Count(p => p.Team == "Red") ? "Blue" : "Red";
        var newPlayer = new Player { UserId = userId, Team = team };

        // Add the user to the room
        room.Participants.Add(newPlayer);

        // Add the user to the SignalR group for the room
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Notify the room that a new player joined
        await Clients.Group(roomId).SendAsync("PlayerJoined", userId, team);

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
        if (room.Host.UserId != inviterUserId && !room.Participants.Any(p => p.UserId == inviterUserId))
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this room.");
            return;
        }

        // Check if the invited user is connected (i.e., is in the login room)
        if (LoginRoomMapping.TryGetValue(invitedUserId, out var loginRoomConnectionId))
        {
            // Send an invitation to the invited user's login room (using their user ID or token)
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
        var inviterIsValid = room.Host.UserId == inviterId || room.Participants.Any(p => p.UserId == inviterId);
        if (!inviterIsValid)
        {
            await Clients.Caller.SendAsync("Error", "Invalid inviter.");
            return "Error: Invalid inviter";
        }

        // Check if the user is already in the room
        if (room.Participants.Any(p => p.UserId == userId))
        {
            await Clients.Caller.SendAsync("Error", "You are already in the room.");
            return "Error: Already in the room";
        }

        // Assign the user to a team (blue if fewer blue players, red otherwise)
        string team = room.Participants.Count(p => p.Team == "Blue") < room.Participants.Count(p => p.Team == "Red") ? "Blue" : "Red";
        
        // Add the user to the room
        var newPlayer = new Player { UserId = userId, Team = team };
        room.Participants.Add(newPlayer);

        // Add the user to the SignalR group for the room
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Notify the room that a new player joined
        await Clients.Group(roomId).SendAsync("PlayerJoined", userId, team);

        // Notify the inviter that the invitation was accepted
        if (UserIdToConnectionId.TryGetValue(inviterId, out var inviterConnectionId))
        {
            await Clients.Client(inviterConnectionId).SendAsync("InvitationAccepted", roomId, userId);
        }

        return "OK";
    }



    public async Task StartGame(string token, string roomId)
    {
        Console.WriteLine("StartGame method called.");
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
        if (room.Host.UserId != userId)
        {
            await Clients.Caller.SendAsync("Error", "Only the host can start the game.");
            return;
        }

        // Notify all participants of their teams
        foreach (var player in room.Participants)
        {
            await Clients.Group(roomId).SendAsync("PlayerTeam", player.UserId, player.Team);
        }

        // Notify all participants that the game has started
        await Clients.Group(roomId).SendAsync("GameStarted");

        // Start the countdown timer
        await SendingQuestions(roomId); // Await the countdown task
    }

    private async Task SendingQuestions(string roomId)
    {
        try
        {
            Console.WriteLine("RunCountdown method started.");
            var questions = new List<Question>(RoomToQuestions["room1"]); // Use double quotes for strings
            
            // Perform 5 countdown steps (10 seconds each)
            for (int i =0 ; i<questions.Count ; i++)
            {
                // Notify the room about the countdown
                //Getting the current Question to Handle The answers
                RoomToCurrentQuestion["room1"]=questions[i];
                await Clients.Group(roomId).SendAsync("CountdownTick", i * 10);

                // Send a question to the room
                var question = questions[i];
                await Clients.Group(roomId).SendAsync("ReceiveQuestion", question.QuestionTitle, question.Answers);


                // Wait for 10 seconds before the next tick
                await Task.Delay(10000);
            }

            // Notify the room that the countdown is over
            RoomToCurrentQuestion["room1"] = null;
            await Clients.Group(roomId).SendAsync("CountdownComplete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RunCountdown: {ex.Message}");
            // Optionally notify the host or participants of the error
            await Clients.Group(roomId).SendAsync("Error", "Countdown failed.");
        }
    }

    public async Task NotifyPlayerTeam(string roomId)
    {
        var room = Rooms[roomId];
        foreach (var player in room.Participants)
        {
            await Clients.Group(roomId).SendAsync("PlayerTeam", player.UserId, player.Team);
        }
    }

    public async Task SwitchTeam(string token, string roomId)
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

        // Find the player in the room's participants
        var player = room.Participants.FirstOrDefault(p => p.UserId == userId);

        if (player == null)
        {
            await Clients.Caller.SendAsync("Error", "Player not found in the room.");
            return;
        }

        // Switch the player's team
        if (player.Team == "Blue")
        {
            player.Team = "Red";
        }
        else if (player.Team == "Red")
        {
            player.Team = "Blue";
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Player is not assigned to a valid team.");
            return;
        }

        // Notify the room that the player has switched teams
        await Clients.Group(roomId).SendAsync("PlayerTeamChanged", userId, player.Team);

        // Send the updated team back to the player
        await Clients.Caller.SendAsync("TeamSwitched", player.Team);  // Inform the client about the team switch
    }
    public async Task AnswerQuestion(string token, string roomId ,string answer)
    {
        // Retrieve the  user ID of from the token
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

        //get the current question

        if (!RoomToCurrentQuestion.TryGetValue(roomId, out var currentQuestion))
        {
            await Clients.Caller.SendAsync("Error", "Game Is End no Question to Answer.");
            return;
        }

        //check is the answer is correct and add the score to him in the room
        if(answer == currentQuestion.CorrectAnswer)
        {

            var participantIndex = room.Participants.FindIndex(p => p.UserId == userId);
            if (participantIndex != -1)
            {
                room.Participants[participantIndex].Score+=1;
            }

        }

        // Notify the Room that the UserId answered Succefully
        await Clients.Group(roomId).SendAsync("SuccefullyAnswered", userId);
    }



}

public class Room
{
    public string RoomId { get; set; } = string.Empty;
    public Player Host { get; set; } = new Player(); // Host is a Player with a team
    public List<Player> Participants { get; set; } = new List<Player>(); // List of Players (each with a team)
}


public class Player
{
    public string UserId { get; set; } = string.Empty;
    public string Team { get; set; } = "Unassigned"; // Team can be "Blue" or "Red"
    public int Score{ get; set;}= 0;
}


public class Question
{
    public string QuestionTitle { get; set; } = string.Empty; // The question text
    public string[] Answers { get; set; } = Array.Empty<string>(); // Array of 4 possible answers
    public string CorrectAnswer { get; set; } = string.Empty; // The correct answer
}