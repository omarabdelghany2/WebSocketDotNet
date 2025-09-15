namespace BackEnd.middlewareService.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Profile_Name { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public int Score { get; set; }
        public int Balance { get; set; }
        public bool Is_Staff { get; set; }
        public bool Is_Subscribed { get; set; }
        public string Coins { get; set; }
        public List<Avatar> Avatar { get; set; } = new();
    }

        public class Avatar
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string url { get; set; } = "";
    }
}