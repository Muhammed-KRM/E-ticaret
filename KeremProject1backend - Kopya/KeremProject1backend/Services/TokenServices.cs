using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KeremProject1backend.Services
{
    public static class TokenServices
    {
        private static readonly string SecretKey = "your-256-bit-secret-key-here-should-be-32-bytes-long"; // Daha güçlü bir key kullanın.
        private static readonly int TokenExpirationMinutes = 180; // Token geçerlilik süresi (30 dakika).

        public static string GenerateToken(int userId, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)); // Anahtar boyutunun doğru olduğuna dikkat edin
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // Kullanıcı ID'si
            new Claim(ClaimTypes.Role, role) // Kullanıcı rolü
                }),
                Expires = DateTime.Now.AddMinutes(TokenExpirationMinutes), // Token süresi
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(SecretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // Saat farkını kaldırır.
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
