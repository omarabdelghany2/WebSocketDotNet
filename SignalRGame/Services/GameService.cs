using Microsoft.AspNetCore.SignalR; // For IHubContext
using System;
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalRGame.Models;
using SignalRGame.Hubs;


namespace SignalRGame.Services
{
    public class GameService
    {
        private readonly IHubContext<GameHub> _hubContext;

        public GameService(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendingQuestions(string roomId, ConcurrentDictionary<string, List<Question>> roomToQuestions,ConcurrentDictionary<string, Question> roomToCurrentQuestion,ConcurrentDictionary<string, Room> Rooms ,ConcurrentDictionary<string, string> LoginRoomMapping)
        {
            string winner="";
            var group = _hubContext.Clients.Group(roomId);

            //get the room object
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await group.SendAsync("Error", "Room does not exist.");
                return;
            }

            try
            {
                Console.WriteLine($"SendingQuestions started for room {roomId}.");

                if (!roomToQuestions.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
                {
                    await group.SendAsync("Error", "No questions available for this room.");
                    return;
                }

                string roundWinner="";
                // for (int i = 0; i < questions.Count; i++)
                for (int i = 0; i < 3; i++)
                {
                    var currentQuestion = questions[i];
                    roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    var answers = currentQuestion.answers;

                    // Shuffle the answers
                    var rng = new Random();
                    answers = answers.OrderBy(a => rng.Next()).ToList();

                    await group.SendAsync("receiveQuestion", new{subCategory=currentQuestion.subCategory,questionTitle=currentQuestion.questionTitle,answers = answers});
                    //sendin the Timer Here 1 after every second
                    for (int j = 15; j >= 0; j--)
                    {
                        await group.SendAsync("timer",new{timer=j}); // Send the current time on the "timer" channel
                        await Task.Delay(1000); // Wait for 1 second
                    }


                    //after we reached Here the countdown is done so
                    // Gather scores and teams
                    var blueTeam = room.Participants
                        .Where(player => player.team == "Blue")
                        .Select(player => new { userId=player.userId,profileName = player.profileName, gameScore = player.gameScore })
                        .ToList();

                    var redTeam = room.Participants
                        .Where(player => player.team == "Red")
                        .Select(player => new {userId=player.userId, profileName = player.profileName, gameScore = player.gameScore })
                        .ToList();

                    // Send the countdown completion with scores
                    if(room.blueTeamRoundScore>room.redTeamRoundScore){
                        room.blueTeamScore+=100;
                        roundWinner="blue";
                    }

                    else if(room.blueTeamRoundScore<room.redTeamRoundScore){
                        room.redTeamScore+=100;
                        roundWinner="red";
                    }
                    else{
                        room.redTeamScore+=100;
                        room.blueTeamScore+=100;
                        roundWinner="draw";
                    }
                    room.blueTeamRoundScore=0;
                    room.redTeamRoundScore=0;

                    await group.SendAsync("countDownComplete", new
                    {
                        questioIndex = i + 1,
                        correctAnswer = currentQuestion.correctAnswer,
                        blueTeam,
                        redTeam,
                        blueTeamScore=room.blueTeamScore,
                        redTeamScore=room.redTeamScore,
                        roundWinner=roundWinner
                    });
                    roomToCurrentQuestion.TryRemove(roomId, out _);
                    }

                    if(room.blueTeamScore>room.redTeamScore){
                        winner="Blue";
                    }
                    else if(room.blueTeamScore<room.redTeamScore){
                        winner="Red";
                    }
                    else{
                        winner="Draw";
                    }

                    //gameEnd 
                    foreach (var participant in room.Participants)
                    {
                        var userId = participant.userId; // Assuming participant.userId represents the friendId

                        if (LoginRoomMapping.TryGetValue(userId, out var loginRoomConnectionId))
                        {
                            await _hubContext.Clients.Group(loginRoomConnectionId).SendAsync("gameEnd", 
                                new 
                                { 
                                    userId = participant.userId, 
                                    gameScore = participant.gameScore, 
                                    score = participant.score, // Assuming 'score' is the same as 'gameScore'
                                    team = participant.team,
                                    winner = winner
                                });
                        }
                    }

                    //game END HERE in database
                    //TODO HERE

                    roomToQuestions.TryRemove(roomId, out _);


            }

                
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingQuestions: {ex.Message}");
                await group.SendAsync("Error", "An error occurred during the game.");
            }
        }
    }
}