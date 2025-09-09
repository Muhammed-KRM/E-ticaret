namespace KeremProject1backend.Models.Responses
{
    public class TestResponses
    {
    }

    // TestAuth endpoint'i için response modeli
    public class TestAuthResponse
    {
        public int UserId { get; set; }
        public string Role { get; set; } = null!;
    }
}
