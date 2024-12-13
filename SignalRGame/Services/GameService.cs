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

        public async Task SendingQuestions(string roomId, ConcurrentDictionary<string, List<Question>> roomToQuestions,ConcurrentDictionary<string, Question> roomToCurrentQuestion)
        {
            var group = _hubContext.Clients.Group(roomId);

            try
            {
                Console.WriteLine($"SendingQuestions started for room {roomId}.");

                if (!roomToQuestions.TryGetValue("room1", out var questions) || questions == null || questions.Count == 0)
                {
                    await group.SendAsync("Error", "No questions available for this room.");
                    return;
                }

                for (int i = 0; i < questions.Count; i++)
                {
                    var currentQuestion = questions[i];
                    roomToCurrentQuestion.AddOrUpdate(roomId, currentQuestion, (_, __) => currentQuestion);

                    await group.SendAsync("ReceiveQuestion", currentQuestion.QuestionTitle, currentQuestion.Answers);

                    await Task.Delay(10000);
                }

                roomToCurrentQuestion.TryRemove(roomId, out _);
                await group.SendAsync("CountdownComplete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendingQuestions: {ex.Message}");
                await group.SendAsync("Error", "An error occurred during the game.");
            }
        }
    }
}