using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task answerQuestion(answerQuestionRequest request)
        
        {



            Console.WriteLine("entered answer ");
            string token= request.token;
            string roomId=request.roomId;
            int timer=request.timer;
            string answer=request.answer;
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            Console.WriteLine(userId);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                Console.WriteLine("invlaid Token ");
                return;
            }


            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room does not exist."); 
                Console.WriteLine(roomId);   
                Console.WriteLine("room invalid ");
                return;
            }
            Console.WriteLine(roomId);

            //get the current question

            if (!RoomToCurrentQuestion.TryGetValue(roomId, out var currentQuestion))
            {
                await Clients.Caller.SendAsync("Error", "Game Is End no Question to Answer.");
                Console.WriteLine("currentQuestion invalid ");
                return;
            }

            //check is the answer is correct and add the score to him in the room
            var participantIndex = room.Participants.FindIndex(p => p.userId == userId);


            if(room.Participants[participantIndex].answered==true){
                Console.WriteLine("answered before ");
                
                return;
            }
             room.Participants[participantIndex].answered=true;

                //increasing the room anseerscount
            room.answersCount+=1;   
            Console.WriteLine(currentQuestion.correctAnswer);
            if(answer == currentQuestion.correctAnswer)
            {

                Console.WriteLine("entered correct answer ");

                if (participantIndex != -1)
                {
                    //check if he answered before
                   
                        
                    Console.WriteLine("entered the increase of score");
                    room.Participants[participantIndex].gameScore+=(int)Math.Ceiling(timer * 10.0 / room.questionTime);
                    Console.WriteLine($"added the score {room.Participants[participantIndex].profileName}");

                    //add the teamRoundScore here

                    if(room.Participants[participantIndex].team=="Blue"){
                        room.blueTeamRoundScore+=1;
                    }
                    else{
                        room.redTeamRoundScore+=1;
                    }
                    Console.WriteLine($"the score is  {room.Participants[participantIndex].gameScore}");
                    
                    
                }
                // if(userId == room.Host.userId) // so its the answer of the host
                // {
                //     room.Host.gameScore+=(int)Math.Ceiling(timer * 10.0 / room.questionTime);
                //     Console.WriteLine($"added the score again{room.Participants[participantIndex].profileName} because its a host");
                // }

            }
            else
            {

                Console.WriteLine("entered wrong answer ");

                if (participantIndex != -1)
                {
                    //check if he answered before
                   
                        
                    Console.WriteLine("entered the decrease of score");
                    room.Participants[participantIndex].gameScore-=(int)Math.Ceiling(timer * 10.0 / room.questionTime);

                    //add the teamRoundScore here

                    // if(room.Participants[participantIndex].team=="Blue"){
                    //     room.blueTeamRoundScore+=1;
                    // }
                    // else{
                    //     room.redTeamRoundScore+=1;
                    // }
                    Console.WriteLine($"the score is  {room.Participants[participantIndex].gameScore}");
                    
                    
                }
                // if(userId == room.Host.userId) // so its the answer of the host
                // {
                //     room.Host.gameScore-=(int)Math.Ceiling(timer * 10.0 / room.questionTime);
                // }

            }
            
            await Clients.Group(roomId).SendAsync("succefullyAnswered", new{profileName=room.Participants[participantIndex].profileName,userId=Convert.ToInt32(room.Participants[participantIndex].userId) ,team =room.Participants[participantIndex].team});

        }
    }


    public class answerQuestionRequest{

        public string token { get; set; }
        public string roomId { get; set; }

        public string answer { get; set; }

        public int timer { get; set;} 
    }
}