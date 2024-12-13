using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
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
                    Console.WriteLine($"the score is  {room.Participants[participantIndex].Score}");
                    await Clients.Group(roomId).SendAsync("correctAnswer ", userId);
                    
                }
                else if(userId == room.Host.UserId) // so its the answer of the host
                {
                    room.Host.Score+=1;
                    
                    Console.WriteLine($"The Score is {room.Host.Score}.");
                    await Clients.Group(roomId).SendAsync("correctAnswer ",userId);
                }

            }

            // Notify the Room that the UserId answered Succefully
            await Clients.Group(roomId).SendAsync("SuccefullyAnswered", userId);
        }
    }
}