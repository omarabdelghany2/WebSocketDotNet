using Microsoft.AspNetCore.SignalR; // For IHubContext
using System;
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalRGame.Models;
using SignalRGame.Hubs;

using System.Text.Json;
using System.Text;


namespace SignalRGame.Services
{
    public class GameService
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly HttpClient _httpClient;
        public GameService(IHubContext<GameHub> hubContext,HttpClient httpClient)
        {
            _hubContext = hubContext;

            _httpClient = httpClient;
        }

        public async Task SendingQuestions(string token ,string roomId, ConcurrentDictionary<string, List<Question>> roomToQuestions,ConcurrentDictionary<string, Question> roomToCurrentQuestion,ConcurrentDictionary<string, Room> Rooms ,List<string>subCategories,int questionTime)
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
                

                if (!roomToQuestions.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
                {
                    await group.SendAsync("Error", "No questions available for this room.");
                    return;
                }

                string roundWinner="";
                // for (int i = 0; i < questions.Count; i++)
                for (int i = 0; i < 3; i++)
                {
                    room.answersCount=0;



                    //make the answered variable by falase


                    foreach (var participant in room.Participants)
                    {
                        participant.answered=false;
                    }

                    //get the current question
                    var currentQuestion = questions[i];
                    roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    var answers = currentQuestion.answers;

                    // Shuffle the answers
                    var rng = new Random();
                    answers = answers.OrderBy(a => rng.Next()).ToList();

                    await group.SendAsync("receiveQuestion", new{subCategory=currentQuestion.subCategory,questionTitle=currentQuestion.questionTitle,answers = answers});
                    //sendin the Timer Here 1 after every second
                    for (int j = questionTime; j >= 0; j--)
                    {
                        await group.SendAsync("timer",new{timer=j}); // Send the current time on the "timer" channel
                        if (room.answersCount == room.Participants.Count)
                        {
                            break; // Break the loop if all participants have answered
                        }
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
                    else if (room.blueTeamRoundScore == 0 && room.redTeamRoundScore == 0)
                    {
                        roundWinner = "NegativeDraw";
                    }


                    else{
                        room.redTeamScore+=100;
                        room.blueTeamScore+=100;
                        roundWinner="PositiveDraw";
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
                    await Task.Delay(1000); // Wait for 1 second
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


                    






                    //ADD THE SCORES OF THE WINNERS AND MINUS THE SCORES OF THE LOSERS

                    if(winner=="Red"){

                        foreach (var participant in room.Participants)
                        {
                            if (participant.team == "Red")
                            {
                                participant.score += 100+participant.gameScore; // Increase score by 1 for Blue team
                                participant.gameScore+=100;
                            }
                            else if (participant.team == "Blue")
                            {
                                participant.score -= (100+participant.gameScore); // Decrease score by 1 for Red team
                                participant.gameScore-=100;
                                if(participant.score<0){
                                    participant.score=0;
                                }
                            }
                        }

                    }

                    else if(winner=="Blue"){

                        foreach (var participant in room.Participants)
                        {
                            if (participant.team == "Blue")
                            {
                                participant.score += 100+participant.gameScore; // Increase score by 1 for Blue team
                                participant.gameScore+=100;
                            }
                            else if (participant.team == "Red")
                            {
                                 participant.score -= (100+participant.gameScore); // Decrease score by 1 for Red team
                                 participant.gameScore-=100;
                                if(participant.score<0){
                                    participant.score=0;
                                }
                            }
                        }

                    }


                    else{

                        foreach (var participant in room.Participants)
                        {
                                participant.score += 50+participant.gameScore; // Increase score by 1 for Blue team
                                participant.gameScore+=50;

                        }

                    }



                    var participantsData = room.Participants
                        .ToDictionary(
                            participant => Convert.ToInt32(participant.userId), // Key: userId as integer
                            participant => new // Value: The rest of the participant's data
                            {
                                gameScore = participant.gameScore,
                                score = participant.score, // Assuming 'score' is the same as 'gameScore'
                                team = participant.team,
                            });

                    await _hubContext.Clients.Group(roomId).SendAsync("gameEnd", new {winner = winner,stats = participantsData });




                //MAKING THE TEAM INFO TO SEND IT TO DATABASE

                var teamsInfo = new List<object>
                {
                    new
                    {
                        Red = new List<object>
                        {
                            new
                            {
                                users = room.Participants
                                    .Where(player => player.team == "Red")
                                    .Select(player => new
                                    {
                                        user_id = player.userId,
                                        user_score = player.score
                                    }).ToList()
                            },
                            new
                            {
                                team_score = room.redTeamScore
                            }
                        }
                    },
                    new
                    {
                        Blue = new List<object>
                        {
                            new
                            {
                                users = room.Participants
                                    .Where(player => player.team == "Blue")
                                    .Select(player => new
                                    {
                                        user_id = player.userId,
                                        user_score = player.score
                                    }).ToList()
                            },
                            new
                            {
                                team_score = room.blueTeamScore
                            }
                        }
                    }
                };


                TimeZoneInfo kuwaitTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
                DateTime kuwaitTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kuwaitTimeZone);
                string createdAt = kuwaitTime.ToString("yyyy-MM-dd HH:mm:ss");


                bool saveResult = await saveGame(
                    token=token,
                    isPublic: "True",
                    createdAt: createdAt,
                    categories: subCategories,
                    mood: "mood1",
                    hostId: room.Host.userId,
                    teamInfo: teamsInfo,
                    subCategories: subCategories
                );

                Console.WriteLine("entered save game");

                    roomToQuestions.TryRemove(roomId, out _);


            }

                
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingQuestions: {ex.Message}");
                await group.SendAsync("Error", "An error occurred during the game.");
            }
        }


        public async Task<bool> saveGame(string token,string isPublic,string createdAt,List<string>categories,string mood ,string hostId,List<object> teamInfo,List<string>subCategories){

            var databaseServerUrl = $"http://localhost:8000/api/game/save/";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

            var jsonPayload = JsonSerializer.Serialize(new { is_public = isPublic ,created_at = createdAt,sub_categories= subCategories,mood=mood,host_id=hostId,teams_info=teamInfo});
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                // Send the request
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                requestMessage.Content = content;
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                    return true;
                }
                else
                {
                    // Log the error response
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    return false; // Return false for non-success status codes
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occurred during the HTTP request
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return false; // Return false if an exception occurs
            }

        }
    }
}