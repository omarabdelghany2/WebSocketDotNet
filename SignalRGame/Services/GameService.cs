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
        public GameService(IHubContext<GameHub> hubContext, HttpClient httpClient)
        {
            _hubContext = hubContext;

            _httpClient = httpClient;
        }

        public async Task SendingQuestions(string token, string roomId, ConcurrentDictionary<string, List<Question>> roomToQuestions, ConcurrentDictionary<string, Question> roomToCurrentQuestion, ConcurrentDictionary<string, Room> Rooms, List<string> subCategories, int questionTime, ConcurrentDictionary<string, string> UserRoomMapping)
        {
            string winner = "";
            // var group = _hubContext.Clients.Group(roomId);

            //get the room object
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "Room does not exist.");
                return;
            }

            try
            {


                if (!roomToQuestions.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
                {
                    await _hubContext.Clients.Group(roomId).SendAsync("Error", "No questions available for this room.");
                    return;
                }

                string roundWinner = "";
                // for (int i = 0; i < 2; i++)
                for (int i = 0; i < questions.Count; i++)
                {
                    room.answersCount = 0;



                    //make the answered variable by falase


                    foreach (var participant in room.Participants)
                    {
                        participant.answered = false;
                    }

                    //get the current question
                    var currentQuestion = questions[i];
                    roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    var answers = currentQuestion.answers;

                    // Shuffle the answers
                    var rng = new Random();
                    answers = answers.OrderBy(a => rng.Next()).ToList();

                    await _hubContext.Clients.Group(roomId).SendAsync("receiveQuestion", new { subCategory = currentQuestion.subCategory, questionTitle = currentQuestion.questionTitle, answers = answers });
                    //sendin the Timer Here 1 after every second
                    for (int j = questionTime; j >= 0; j--)
                    {
                        await _hubContext.Clients.Group(roomId).SendAsync("timer", new { timer = j }); // Send the current time on the "timer" channel
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
                        .Select(player => new { userId = player.userId, profileName = player.profileName, gameScore = player.gameScore })
                        .ToList();

                    var redTeam = room.Participants
                        .Where(player => player.team == "Red")
                        .Select(player => new { userId = player.userId, profileName = player.profileName, gameScore = player.gameScore })
                        .ToList();

                    // Send the countdown completion with scores
                    if (room.blueTeamRoundScore > room.redTeamRoundScore)
                    {
                        room.blueTeamScore += 100;
                        roundWinner = "blue";
                    }

                    else if (room.blueTeamRoundScore < room.redTeamRoundScore)
                    {
                        room.redTeamScore += 100;
                        roundWinner = "red";
                    }
                    else if (room.blueTeamRoundScore == 0 && room.redTeamRoundScore == 0)
                    {
                        roundWinner = "NegativeDraw";
                    }


                    else
                    {
                        room.redTeamScore += 100;
                        room.blueTeamScore += 100;
                        roundWinner = "PositiveDraw";
                    }
                    room.blueTeamRoundScore = 0;
                    room.redTeamRoundScore = 0;

                    await _hubContext.Clients.Group(roomId).SendAsync("countDownComplete", new
                    {
                        questioIndex = i + 1,
                        correctAnswer = currentQuestion.correctAnswer,
                        blueTeam,
                        redTeam,
                        blueTeamScore = room.blueTeamScore,
                        redTeamScore = room.redTeamScore,
                        roundWinner = roundWinner


                    });
                    await Task.Delay(1000); // Wait for 1 second
                    roomToCurrentQuestion.TryRemove(roomId, out _);
                }

                if (room.blueTeamScore > room.redTeamScore)
                {
                    winner = "Blue";
                }
                else if (room.blueTeamScore < room.redTeamScore)
                {
                    winner = "Red";
                }
                else
                {
                    winner = "Draw";
                }

                //gameEnd 









                //ADD THE SCORES OF THE WINNERS AND MINUS THE SCORES OF THE LOSERS

                if (winner == "Red")
                {

                    foreach (var participant in room.Participants)
                    {
                        if (participant.team == "Red")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score += 10 + scaledBonus;
                            participant.gameScore += 10;
                        }
                        else if (participant.team == "Blue")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score -= (10 + scaledBonus);
                            participant.gameScore -= 10;
                            if (participant.score < 0)
                            {
                                participant.score = 0;
                            }
                        }
                    }

                }

                else if (winner == "Blue")
                {

                    foreach (var participant in room.Participants)
                    {
                        if (participant.team == "Blue")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score += 10 + scaledBonus;
                            participant.gameScore += 10;
                        }
                        else if (participant.team == "Red")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score -= (10 + scaledBonus);
                            participant.gameScore -= 10;
                            if (participant.score < 0)
                            {
                                participant.score = 0;
                            }
                        }
                    }

                }


                else
                {

                    foreach (var participant in room.Participants)
                    {
                        int totalWindow = Math.Max(1, questions.Count * questionTime);
                        int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                        participant.score += 5 + scaledBonus;
                        participant.gameScore += 5;

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

                await _hubContext.Clients.Group(roomId).SendAsync("gameEnd", new { winner = winner, stats = participantsData });




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


                bool saveResult = await saveGameClassic(
                    token = token,
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

                // Ensure removal happens only after saving is completed successfully
                if (saveResult)
                {
                    Console.WriteLine("Game saved successfully. Now removing room...");
                    roomToQuestions.TryRemove(roomId, out _);
                    Rooms.TryRemove(roomId, out _);
                    UserRoomMapping.TryRemove(room.Host.userId, out _);
                    Console.WriteLine($"Room {roomId} removed.");
                }
                else
                {
                    Console.WriteLine("Game save failed. Room will not be removed.");
                }


            }


            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingQuestions: {ex.Message}");
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "An error occurred during the game.");
            }
        }




        public async Task SendingCustomQuestions(string token, string roomId, ConcurrentDictionary<string, List<Question>> roomToQuestions, ConcurrentDictionary<string, Question> roomToCurrentQuestion, ConcurrentDictionary<string, Room> Rooms, int questionTime, ConcurrentDictionary<string, string> UserRoomMapping)
        {
            string winner = "";

            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "Room does not exist.");
                return;
            }

            try
            {
                if (!roomToQuestions.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
                {
                    await _hubContext.Clients.Group(roomId).SendAsync("Error", "No questions available for this room.");
                    return;
                }

                for (int i = 0; i < questions.Count; i++)
                {
                    room.answersCount = 0;
                    foreach (var participant in room.Participants)
                        participant.answered = false;

                    var currentQuestion = questions[i];
                    roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    var rng = new Random();
                    var answers = currentQuestion.answers.OrderBy(a => rng.Next()).ToList();

                    await _hubContext.Clients.Group(roomId).SendAsync("receiveQuestion", new
                    {
                        subCategory = currentQuestion.subCategory,
                        questionTitle = currentQuestion.questionTitle,
                        answers = answers
                    });

                    for (int j = questionTime; j >= 0; j--)
                    {
                        await _hubContext.Clients.Group(roomId).SendAsync("timer", new { timer = j });

                        if (room.answersCount == room.Participants.Count)
                            break;

                        await Task.Delay(1000);
                    }

                    var blueTeam = room.Participants
                        .Where(p => p.team == "Blue")
                        .Select(p => new { userId = p.userId, profileName = p.profileName, gameScore = p.gameScore })
                        .ToList();

                    var redTeam = room.Participants
                        .Where(p => p.team == "Red")
                        .Select(p => new { userId = p.userId, profileName = p.profileName, gameScore = p.gameScore })
                        .ToList();

                    string roundWinner = "";
                    if (room.blueTeamRoundScore > room.redTeamRoundScore)
                    {
                        room.blueTeamScore += 100;
                        roundWinner = "blue";
                    }
                    else if (room.blueTeamRoundScore < room.redTeamRoundScore)
                    {
                        room.redTeamScore += 100;
                        roundWinner = "red";
                    }
                    else if (room.blueTeamRoundScore == 0 && room.redTeamRoundScore == 0)
                    {
                        roundWinner = "NegativeDraw";
                    }
                    else
                    {
                        room.redTeamScore += 100;
                        room.blueTeamScore += 100;
                        roundWinner = "PositiveDraw";
                    }

                    room.blueTeamRoundScore = 0;
                    room.redTeamRoundScore = 0;

                    await _hubContext.Clients.Group(roomId).SendAsync("countDownComplete", new
                    {
                        questioIndex = i + 1,
                        correctAnswer = currentQuestion.correctAnswer,
                        blueTeam,
                        redTeam,
                        blueTeamScore = room.blueTeamScore,
                        redTeamScore = room.redTeamScore,
                        roundWinner
                    });

                    await Task.Delay(1000);
                    roomToCurrentQuestion.TryRemove(roomId, out _);
                }

                // Determine final winner
                if (room.blueTeamScore > room.redTeamScore)
                    winner = "Blue";
                else if (room.blueTeamScore < room.redTeamScore)
                    winner = "Red";
                else
                    winner = "Draw";

                // Update player scores based on winner
                foreach (var participant in room.Participants)
                {
                    if (winner == "Red")
                    {
                        if (participant.team == "Red")
                        {
                            participant.score += 100 + participant.gameScore;
                            participant.gameScore += 100;
                        }
                        else
                        {
                            participant.score -= 100 + participant.gameScore;
                            participant.gameScore -= 100;
                            if (participant.score < 0) participant.score = 0;
                        }
                    }
                    else if (winner == "Blue")
                    {
                        if (participant.team == "Blue")
                        {
                            participant.score += 100 + participant.gameScore;
                            participant.gameScore += 100;
                        }
                        else
                        {
                            participant.score -= 100 + participant.gameScore;
                            participant.gameScore -= 100;
                            if (participant.score < 0) participant.score = 0;
                        }
                    }
                    else // Draw
                    {
                        participant.score += 50 + participant.gameScore;
                        participant.gameScore += 50;
                    }
                }

                var participantsData = room.Participants.ToDictionary(
                    p => Convert.ToInt32(p.userId),
                    p => new
                    {
                        gameScore = p.gameScore,
                        score = p.score,
                        team = p.team
                    });

                await _hubContext.Clients.Group(roomId).SendAsync("gameEnd", new
                {
                    winner,
                    stats = participantsData
                });

                // ✅ Cleanup memory (same as classic)
                roomToQuestions.TryRemove(roomId, out _);
                Rooms.TryRemove(roomId, out _);
                UserRoomMapping.TryRemove(room.Host.userId, out _);
                Console.WriteLine($"Custom game finished and cleaned up: Room {roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingCustomQuestions: {ex.Message}");
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "An error occurred during the custom game.");
            }
        }


        public async Task<bool> saveGameClassic(string token, string isPublic, string createdAt, List<string> categories, string mood, string hostId, List<object> teamInfo, List<string> subCategories)
        {

            var databaseServerUrl = $"http://localhost:8004/api/game/";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

            var jsonPayload = JsonSerializer.Serialize(new { is_public = isPublic, created_at = createdAt, sub_categories = subCategories, mood = mood, host_id = hostId, teams_info = teamInfo });
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



        public async Task SendingQuestionsModeTwo(string token, string roomId, ConcurrentDictionary<string, List<QuestionMillionaire>> roomToQuestionsModeTwo, ConcurrentDictionary<string, QuestionMillionaire> roomToCurrentQuestionModeTwo, ConcurrentDictionary<string, Room> Rooms, ConcurrentDictionary<string, string> UserRoomMapping)
        {
            string winner = "";
            // var group = _hubContext.Clients.Group(roomId);

            //get the room object
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "Room does not exist.");
                return;
            }

            try
            {


                if (!roomToQuestionsModeTwo.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
                {
                    await _hubContext.Clients.Group(roomId).SendAsync("Error", "No questions available for this room.");
                    return;
                }

                string roundWinner = "";
                // for (int i = 0; i < 5; i++)
                for (int i = 0; i < questions.Count; i++)
                {
                    room.answersCount = 0;



                    //make the answered variable by falase
                    room.Host.answered = false;
                    //make that he didnt choosed the correct answer for the first Time
                    room.Host.answereCorrectModeTwo = false;

                    //get the current question
                    var currentQuestion = questions[i];
                    roomToCurrentQuestionModeTwo.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    var answers = currentQuestion.answers;

                    // Shuffle the answers
                    var rng = new Random();
                    answers = answers.OrderBy(a => rng.Next()).ToList();

                    await _hubContext.Clients.Group(roomId).SendAsync("receiveQuestionModeTwo", new { questionTitle = currentQuestion.questionTitle, answers = answers });
                    //sending the Timer Here 1 after every second
                    for (int j = 100; j >= 0; j--)
                    {
                        await _hubContext.Clients.Group(roomId).SendAsync("timerModeTwo", new { timer = (j / 100.0) * 100.0 }); // Send the current time on the "timer" channel
                        if (room.answersCount > 0)
                        {
                            break; // The host answered succefully
                        }
                        await Task.Delay(100); // Wait for 1 second
                    }




                    await _hubContext.Clients.Group(roomId).SendAsync("countDownCompleteModeTwo", new
                    {
                        questionIndex = ((i + 1) / (double)questions.Count) * 100.0,
                        correctAnswer = currentQuestion.correctAnswer

                    });
                    await Task.Delay(1000); // Wait for 1 second
                    roomToCurrentQuestionModeTwo.TryRemove(roomId, out _);

                    //RULES OF GAME MODE 2 THAT IF HE ANSWERED WRONG MAKE HIM GO OUT
                    if (room.Host.answereCorrectModeTwo == false)
                    {
                        break;

                    }
                }



                //gameEnd 

                await _hubContext.Clients.Group(roomId).SendAsync("gameEndModeTwo", new { score = room.Host.gameScore });

                bool saveResult = await saveGameMillionaire(
                    token = token,
                    score: room.Host.gameScore
                );

                Console.WriteLine("entered save game");


                // Ensure removal happens only after saving is completed successfully
                if (saveResult)
                {
                    Console.WriteLine("Game saved successfully. Now removing room...");
                    roomToQuestionsModeTwo.TryRemove(roomId, out _);
                    Rooms.TryRemove(roomId, out _);
                    UserRoomMapping.TryRemove(room.Host.userId, out _);
                    Console.WriteLine($"Room {roomId} removed.");
                }
                else
                {
                    Console.WriteLine("Game save failed. Room will not be removed.");
                }






            }


            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingQuestions: {ex.Message}");
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "An error occurred during the game.");
            }
        }







        public async Task<bool> saveGameMillionaire(string token, int score)
        {

            var databaseServerUrl = $"http://localhost:8004/api/millionaire/game/";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

            var jsonPayload = JsonSerializer.Serialize(new { score = score });
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








public async Task SendingQuestionsMode4(
    string token,
    string roomId,
    ConcurrentDictionary<string, List<Question>> roomToQuestions,
    ConcurrentDictionary<string, Question> roomToCurrentQuestion,
    ConcurrentDictionary<string, Room> Rooms,
    int questionTime,
    ConcurrentDictionary<string, string> UserRoomMapping)
{
    string winner = "";

    if (!Rooms.TryGetValue(roomId, out var room))
    {
        Console.WriteLine($"[ERROR] Room {roomId} does not exist.");
        await _hubContext.Clients.Group(roomId).SendAsync("Error", "Room does not exist.");
        return;
    }

    try
    {
        if (!roomToQuestions.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
        {
            Console.WriteLine($"[ERROR] No questions available for room {roomId}.");
            await _hubContext.Clients.Group(roomId).SendAsync("Error", "No questions available for this room.");
            return;
        }

        // passage countdown loop
        for (int j = 5; j >= 0; j--)
        {
            Console.WriteLine($"[SEND] PassageTimer => {{ timer = {j} }}");
            await _hubContext.Clients.Group(roomId).SendAsync("PassageTimer", new { timer = j });
            await Task.Delay(1000);
        }

        for (int i = 0; i < questions.Count; i++)
        {
            room.answersCount = 0;

            // reset answered flag for all participants
            foreach (var participant in room.Participants)
            {
                participant.answered = false;
            }

            // current question
            var currentQuestion = questions[i];
            roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

            // shuffle answers
            var rng = new Random();
            var answers = currentQuestion.answers.OrderBy(a => rng.Next()).ToList();

            var questionPayload = new
            {
                questionTitle = currentQuestion.questionTitle,
                answers = answers
            };

            Console.WriteLine($"[SEND] receiveQuestion => {System.Text.Json.JsonSerializer.Serialize(questionPayload)}");
            await _hubContext.Clients.Group(roomId).SendAsync("receiveQuestion", questionPayload);

            // countdown loop
            for (int j = questionTime; j >= 0; j--)
            {
                Console.WriteLine($"[SEND] timer => {{ timer = {j} }}");
                await _hubContext.Clients.Group(roomId).SendAsync("timer", new { timer = j });

                if (room.answersCount == room.Participants.Count)
                {
                    Console.WriteLine("[INFO] All players answered. Skipping timer.");
                    break; // all players answered
                }

                await Task.Delay(1000);
            }

            // after question ends → show correct answer & scores
            var playersData = room.Participants
                .Select(p => new
                {
                    userId = p.userId,
                    profileName = p.profileName,
                    gameScore = p.gameScore,
                    totalScore = p.score
                })
                .ToList();

            var completePayload = new
            {
                questionIndex = i + 1,
                correctAnswer = currentQuestion.correctAnswer,
                players = playersData
            };

            Console.WriteLine($"[SEND] countDownComplete => {System.Text.Json.JsonSerializer.Serialize(completePayload)}");
            await _hubContext.Clients.Group(roomId).SendAsync("countDownCompleteMode4", completePayload);

            await Task.Delay(1000);

            roomToCurrentQuestion.TryRemove(roomId, out _);
        }

        // winner = player with highest gameScore
        var topPlayer = room.Participants.OrderByDescending(p => p.gameScore).FirstOrDefault();
        if (topPlayer != null)
        {
            winner = topPlayer.profileName;
        }

        // final scores: add gameScore to total score
        foreach (var participant in room.Participants)
        {
            participant.score += participant.gameScore;
        }

        var finalStats = room.Participants.ToDictionary(
            p => Convert.ToInt32(p.userId),
            p => new
            {
                gameScore = p.gameScore,
                score = p.score,
                profileName = p.profileName
            });

        var gameEndPayload = new
        {
            winner = winner,
            stats = finalStats
        };

        Console.WriteLine($"[SEND] gameEnd => {System.Text.Json.JsonSerializer.Serialize(gameEndPayload)}");
        await _hubContext.Clients.Group(roomId).SendAsync("gameEndMode4", gameEndPayload);

        // cleanup
        roomToQuestions.TryRemove(roomId, out _);
        Rooms.TryRemove(roomId, out _);
        UserRoomMapping.TryRemove(room.Host.userId, out _);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] in SendingQuestionsMode4: {ex}");
        await _hubContext.Clients.Group(roomId).SendAsync("Error", "An error occurred during the game.");
    }
}


        public async Task SendingGuestQuestions(string token, string roomId, ConcurrentDictionary<string, List<Question>> roomToQuestions, ConcurrentDictionary<string, Question> roomToCurrentQuestion, ConcurrentDictionary<string, Room> Rooms, int questionTime, ConcurrentDictionary<string, string> UserRoomMapping)  
        {
            string winner = "";

            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "Room does not exist.");
                return;
            }

            try
            {
                if (!roomToQuestions.TryGetValue(roomId, out var questions) || questions == null || questions.Count == 0)
                {
                    await _hubContext.Clients.Group(roomId).SendAsync("Error", "No questions available for this room.");
                    return;
                }

                for (int i = 0; i < questions.Count; i++)
                // for (int i = 0; i < 5; i++)
                {
                    room.answersCount = 0;
                    foreach (var participant in room.Participants)
                        participant.answered = false;

                    var currentQuestion = questions[i];
                    roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    var rng = new Random();
                    var answers = currentQuestion.answers.OrderBy(a => rng.Next()).ToList();

                    await _hubContext.Clients.Group(roomId).SendAsync("receiveQuestion", new
                    {
                        subCategory = currentQuestion.subCategory,
                        questionTitle = currentQuestion.questionTitle,
                        answers = answers
                    });

                    for (int j = questionTime; j >= 0; j--)
                    {
                        await _hubContext.Clients.Group(roomId).SendAsync("timer", new { timer = j });

                        if (room.answersCount == room.Participants.Count)
                            break;

                        await Task.Delay(1000);
                    }

                    var blueTeam = room.Participants
                        .Where(p => p.team == "Blue")
                        .Select(p => new { userId = p.userId, profileName = p.profileName, gameScore = p.gameScore })
                        .ToList();

                    var redTeam = room.Participants
                        .Where(p => p.team == "Red")
                        .Select(p => new { userId = p.userId, profileName = p.profileName, gameScore = p.gameScore })
                        .ToList();

                    string roundWinner = "";
                    if (room.blueTeamRoundScore > room.redTeamRoundScore)
                    {
                        room.blueTeamScore += 100;
                        roundWinner = "blue";
                    }
                    else if (room.blueTeamRoundScore < room.redTeamRoundScore)
                    {
                        room.redTeamScore += 100;
                        roundWinner = "red";
                    }
                    else if (room.blueTeamRoundScore == 0 && room.redTeamRoundScore == 0)
                    {
                        roundWinner = "NegativeDraw";
                    }
                    else
                    {
                        room.redTeamScore += 100;
                        room.blueTeamScore += 100;
                        roundWinner = "PositiveDraw";
                    }

                    room.blueTeamRoundScore = 0;
                    room.redTeamRoundScore = 0;

                    await _hubContext.Clients.Group(roomId).SendAsync("countDownComplete", new
                    {
                        questioIndex = i + 1,
                        correctAnswer = currentQuestion.correctAnswer,
                        blueTeam,
                        redTeam,
                        blueTeamScore = room.blueTeamScore,
                        redTeamScore = room.redTeamScore,
                        roundWinner
                    });









                    await Task.Delay(1000);
                    roomToCurrentQuestion.TryRemove(roomId, out _);
                }

                // Determine final winner
                if (room.blueTeamScore > room.redTeamScore)
                    winner = "Blue";
                else if (room.blueTeamScore < room.redTeamScore)
                    winner = "Red";
                else
                    winner = "Draw";




                //ADD THE SCORES OF THE WINNERS AND MINUS THE SCORES OF THE LOSERS

                if (winner == "Red")
                {

                    foreach (var participant in room.Participants)
                    {
                        if (participant.team == "Red")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score += 10 + scaledBonus;
                            participant.gameScore += 10;
                        }
                        else if (participant.team == "Blue")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score -= (10 + scaledBonus);
                            participant.gameScore -= 10;
                            if (participant.score < 0)
                            {
                                participant.score = 0;
                            }
                        }
                    }

                }

                else if (winner == "Blue")
                {

                    foreach (var participant in room.Participants)
                    {
                        if (participant.team == "Blue")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score += 10 + scaledBonus;
                            participant.gameScore += 10;
                        }
                        else if (participant.team == "Red")
                        {
                            int totalWindow = Math.Max(1, questions.Count * questionTime);
                            int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                            participant.score -= (10 + scaledBonus);
                            participant.gameScore -= 10;
                            if (participant.score < 0)
                            {
                                participant.score = 0;
                            }
                        }
                    }

                }


                else
                {

                    foreach (var participant in room.Participants)
                    {
                        int totalWindow = Math.Max(1, questions.Count * questionTime);
                        int scaledBonus = (int)Math.Round(10.0 * participant.gameScore / totalWindow);
                        participant.score += 5 + scaledBonus;
                        participant.gameScore += 5;

                    }

                }

                var participantsData = room.Participants.ToDictionary(
                    p => p.userId,   // keep as string key
                    p => new
                    {
                        gameScore = p.gameScore,
                        score = p.score,
                        team = p.team
                    });

                await _hubContext.Clients.Group(roomId).SendAsync("gameEndMode4", new
                {
                    winner,
                    stats = participantsData
                });

                // ✅ Cleanup memory (same as classic)
                roomToQuestions.TryRemove(roomId, out _);
                Rooms.TryRemove(roomId, out _);
                UserRoomMapping.TryRemove(room.Host.userId, out _);
                Console.WriteLine($"Custom game finished and cleaned up: Room {roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingCustomQuestions: {ex.Message}");
                await _hubContext.Clients.Group(roomId).SendAsync("Error", "An error occurred during the custom game.");
            }
        }

    



    
    }
}
