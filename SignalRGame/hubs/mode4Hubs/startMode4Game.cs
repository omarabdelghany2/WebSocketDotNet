using SignalRGame.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
        public async Task startMode4Game(StartMode4GameRequest request)
        {
            string token = request.token;
            string roomId = request.roomId;

            // 🔹 Get userId from token
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Invalid token." });
                return;
            }

            // 🔹 Validate room
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Room does not exist." });
                return;
            }

            // 🔹 Check if caller is the host
            if (room.Host.userId != userId)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Only the host can start the game." });
                return;
            }


            // // 🔹 Check participants count
            // if (room.Participants == null || room.Participants.Count < 2)
            // {
            //     await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "At least 2 players are required to start the game." });
            //     return;
            // }
            if (room.inGame)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Game already started." });
                return;
            }

            // 🔹 Set room & players into game
            room.inGame = true;
            room.questionTime = 15; // Mode4 default question time

            foreach (var player in room.Participants)
            {
                player.inGame = true;
            }

            // 🔹 Hardcoded Mode4 questions (for now)
            var comprehensionPassage = "Ali went to the market to buy some fruits. He bought apples, bananas, and oranges. After shopping, he met his friend and they went home together.";

            List<Question> questions = new List<Question>
            {
                new Question {
                    questionTitle = "What did Ali go to buy from the market?",
                    answers = new List<Answer>
                    {
                        new Answer { answerText = "Clothes", is_correct = false },
                        new Answer { answerText = "Fruits", is_correct = true },
                        new Answer { answerText = "Books", is_correct = false },
                        new Answer { answerText = "Shoes", is_correct = false }
                    }
                },
                new Question {
                    questionTitle = "Which fruits did Ali buy?",
                    answers = new List<Answer>
                    {
                        new Answer { answerText = "Apples, Bananas, Oranges", is_correct = true },
                        new Answer { answerText = "Grapes, Mangoes, Apples", is_correct = false },
                        new Answer { answerText = "Bananas, Grapes, Pears", is_correct = false },
                        new Answer { answerText = "None of the above", is_correct = false }
                    }
                },
                new Question {
                    questionTitle = "Who did Ali meet after shopping?",
                    answers = new List<Answer>
                    {
                        new Answer { answerText = "His teacher", is_correct = false },
                        new Answer { answerText = "His friend", is_correct = true },
                        new Answer { answerText = "His father", is_correct = false },
                        new Answer { answerText = "A shopkeeper", is_correct = false }
                    }
                },
                new Question {
                    questionTitle = "What did Ali do after meeting his friend?",
                    answers = new List<Answer>
                    {
                        new Answer { answerText = "Went home together", is_correct = true },
                        new Answer { answerText = "Went back to the market", is_correct = false },
                        new Answer { answerText = "Played football", is_correct = false },
                        new Answer { answerText = "Went to school", is_correct = false }
                    }
                },
                new Question {
                    questionTitle = "What is the main idea of the passage?",
                    answers = new List<Answer>
                    {
                        new Answer { answerText = "Ali went shopping and met his friend", is_correct = true },
                        new Answer { answerText = "Ali was studying at home", is_correct = false },
                        new Answer { answerText = "Ali bought new clothes", is_correct = false },
                        new Answer { answerText = "Ali traveled to another city", is_correct = false }
                    }
                }
            };


            // 🔹 Set correctAnswer & hide is_correct
            foreach (var q in questions)
            {
                var correctAnswer = q.answers.FirstOrDefault(a => a.is_correct);
                if (correctAnswer != null)
                {
                    q.correctAnswer = correctAnswer.answerText;
                }

                foreach (var a in q.answers)
                {
                    a.is_correct = false; // hide correctness
                }
            }

            // 🔹 Save to dictionary
            RoomToQuestions[roomId] = questions;

            // Countdown
            for (int j = 5; j >= 0; j--)
            {
                Console.WriteLine($"[SEND] loadingPage => {{ timer = {j} }}");
                await Clients.Group(roomId).SendAsync("loadingPage", new { timer = j });
                await Task.Delay(1000);
            }

            // 🔹 Changed event name from "gameStarted" → "mode4GameStarted"
            await Clients.Group(roomId).SendAsync("mode4GameStarted", new
            {
                error = false,
                errorMessage = "",
                questionsCount = questions.Count,
                passage = comprehensionPassage
            });

            // 🔹 Start sending questions in background
            // 🔹 Start sending questions in background
            _ = Task.Run(() =>
                _gameService.SendingQuestionsMode4(
                    token,
                    roomId,
                    RoomToQuestions,
                    RoomToCurrentQuestion,
                    Rooms,
                    room.questionTime,
                    UserRoomMapping
                ));



        }
    }

    public class StartMode4GameRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }

        public StartMode4GameRequest(string token, string roomId)
        {
            this.token = token;
            this.roomId = roomId;
        }
    }
}
