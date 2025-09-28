using SignalRGame.Models;  
using Microsoft.AspNetCore.SignalR;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task answerQuestionMode4(answerQuestionMode4Request request)
        {
            Console.WriteLine("entered Mode4 answer ");
            string token = request.token;
            string roomId = request.roomId;
            int timer = request.timer;
            string answer = request.answer;

            // 🔹 Validate token
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("mode4Error", "Invalid token.");
                Console.WriteLine("invalid token in mode4");
                return;
            }

            // 🔹 Validate room
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("mode4Error", "Room does not exist.");
                Console.WriteLine("invalid room in mode4");
                return;
            }

            // 🔹 Validate current question
            if (!RoomToCurrentQuestion.TryGetValue(roomId, out var currentQuestion))
            {
                await Clients.Caller.SendAsync("mode4Error", "Game ended or no active question.");
                Console.WriteLine("no active question in mode4");
                return;
            }

            var participantIndex = room.Participants.FindIndex(p => p.userId == userId);
            if (participantIndex == -1)
            {
                await Clients.Caller.SendAsync("mode4Error", "You are not part of this room.");
                return;
            }

            var participant = room.Participants[participantIndex];

            if (participant.answered)
            {
                Console.WriteLine($"{participant.profileName} already answered.");
                return; // ignore multiple answers
            }

            participant.answered = true;
            room.answersCount += 1;

            // 🔹 Check correctness
            if (answer == currentQuestion.correctAnswer)
            {
                participant.gameScore += (int)Math.Ceiling(timer * 10.0 / room.questionTime);
                Console.WriteLine($"{participant.profileName} answered correctly in Mode4, score = {participant.gameScore}");
            }
            else
            {
                participant.gameScore -= (int)Math.Ceiling(timer * 10.0 / room.questionTime);
                Console.WriteLine($"{participant.profileName} answered wrong in Mode4, score = {participant.gameScore}");
            }

            // 🔹 Notify all clients in room
            await Clients.Group(roomId).SendAsync("SuccessfullyAnswered", new
            {
                profileName = participant.profileName,
                userId = Convert.ToInt32(participant.userId),
                score = participant.gameScore
            });
        }
    }

    public class answerQuestionMode4Request
    {
        public string token { get; set; }
        public string roomId { get; set; }
        public string answer { get; set; }
        public int timer { get; set; }
    }
}
