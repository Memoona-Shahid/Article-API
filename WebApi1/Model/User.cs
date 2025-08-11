using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApi1.Model
{
    public class User
    {
        [Key]
        [JsonIgnore] public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        [JsonIgnore] public string token { get; set; } = string.Empty;
        [JsonIgnore] public string RefreshToken { get; set; } = string.Empty;
        [JsonIgnore] public DateTime RefreshTokenExpiryTime { get; set; }

    }
}
