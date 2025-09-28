using SignalRGame.Models;  
using Microsoft.AspNetCore.SignalR;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
    public async Task answerGuestQuestion(answerGuestQuestionRequest request)
    {
        Console.WriteLine("entered guest answer ");
        string roomId = request.roomId;
        int timer = request.timer;
        string answer = request.answer;
        string userId = request.userId; 
        Console.WriteLine(userId);

        // Check if the room exists
        if (!Rooms.TryGetValue(roomId, out var room))
        {
            await Clients.Caller.SendAsync("Error", "Room does not exist."); 
            Console.WriteLine("room invalid " + roomId);
            return;
        }

        // get the current question
        if (!RoomToCurrentQuestion.TryGetValue(roomId, out var currentQuestion))
        {
            await Clients.Caller.SendAsync("Error", "Game Is End no Question to Answer.");
            Console.WriteLine("currentQuestion invalid ");
            return;
        }

        // find participant
        var participantIndex = room.Participants.FindIndex(p => p.userId == userId);

        if (participantIndex == -1)
        {
            await Clients.Caller.SendAsync("Error", "User not found in this room.");
            return;
        }

        if (room.Participants[participantIndex].answered)
        {
            Console.WriteLine("answered before ");
            return;
        }

        room.Participants[participantIndex].answered = true;
        room.answersCount++;

        // ✅ Process guest answer
        if (answer == currentQuestion.correctAnswer)
        {
            room.Participants[participantIndex].gameScore += (int)Math.Ceiling(timer * 10.0 / room.questionTime);
            if (room.Participants[participantIndex].team == "Blue")
                room.blueTeamRoundScore++;
            else
                room.redTeamRoundScore++;
        }
        else
        {
            room.Participants[participantIndex].gameScore -= (int)Math.Ceiling(timer * 10.0 / room.questionTime);
        }

        await Clients.Group(roomId).SendAsync("succefullyAnswered", new
        {
            profileName = room.Participants[participantIndex].profileName,
            userId = room.Participants[participantIndex].userId,
            team = room.Participants[participantIndex].team
        });

        // 🎲 ✅ Simulate bot answers
        var rng = new Random();
        foreach (var bot in room.Participants.Where(p => p.isBot && !p.answered))
        {
            // bot picks random answer
            var chosenAnswer = currentQuestion.answers[rng.Next(currentQuestion.answers.Count)].answerText;

            bot.answered = true;
            room.answersCount++;

            if (chosenAnswer == currentQuestion.correctAnswer)
            {
                bot.gameScore += (int)Math.Ceiling(timer * 10.0 / room.questionTime);
                if (bot.team == "Blue")
                    room.blueTeamRoundScore++;
                else
                    room.redTeamRoundScore++;
                Console.WriteLine($"{bot.profileName} answered correctly!");
            }
            else
            {
                bot.gameScore -= (int)Math.Ceiling(timer * 10.0 / room.questionTime);
                Console.WriteLine($"{bot.profileName} answered wrong!");
            }

            // notify clients that bot answered
            await Clients.Group(roomId).SendAsync("succefullyAnswered", new
            {
                profileName = bot.profileName,
                userId = bot.userId,
                team = bot.team
            });
        }
    }

    }

    public class answerGuestQuestionRequest
    {
        public string userId { get; set; }   // ✅ directly passed
        public string roomId { get; set; }
        public string answer { get; set; }
        public int timer { get; set; }
    }
}
