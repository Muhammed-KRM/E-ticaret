using System.Security.Claims;
using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Responses;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace KeremProject1backend.Services
{
    public class SessionServices
    { }

    public class Session
    {
        public bool _isActive { get; set; }
        public int _sessionno { get; set; }
        public string? _sessiontoken { get; set; }
        public DateTime _LoginTime { get; set; }
        public AllUsersModels _user { get; set; } = null!;

    }

    public class SessionService
    {

        private static object _loginlockobject = new object();
        private static int _lastsessionnogiven;
        private static int _ActivityThresholdinSecond;
        private static int _MaxSessionNumber;
        private static int _MaxConcurrentSessionNumber;
        private static int _SessionTimeoutinSecond;
        private static Session[] _sessionarrays = null!;
        private static long[] _LastAccessSecond = null!;
        private static bool[] _isSessioninActiveState = null!;
        private static DateTime[] _ActivityStartTime = null!;
        private static DateTime[] _LastAccessTime = null!;
        private static DateTime[] _SessionStartTime = null!;
        private static long _SecondCounter;
        public static bool isAuthorized(Session session, UserRoleinAuthorization requiredRole)
        {
            if (session == null || session._user == null)
            {
                return false;
            }

            // Eğer kullanıcının rolü gerekli rol veya daha yüksek bir rolse yetkilidir
            return session._user.UserRoleinAuthorization <= requiredRole;
        }

        public static Session GetSession(int sessionno)
        {
            return _sessionarrays[sessionno];
        }

        public static Session? TestToken(string token, UsersContext usersContext)
        {
            var principal = TokenServices.ValidateToken(token);
            if (principal == null)
            {
                return null; // Geçersiz token.
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = principal.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return null; // Eksik claim.
            }

            return GetSessionByUserId(int.Parse(userId), (UserRoleinAuthorization)Enum.Parse(typeof(UserRoleinAuthorization), role), usersContext);
        }

        private static Session? GetSessionByUserId(int userId, UserRoleinAuthorization role, UsersContext usersContext)
        {
            var user = usersContext.AllUsersModel.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                // logger?.LogWarning($"User not found in database for session creation: UserId={userId}");
                return null;
            }

            if(user.UserRoleinAuthorization != role)
            {
                // logger?.LogWarning($"Role mismatch for user {userId}. Token Role: {role}, DB Role: {user.UserRoleinAuthorization}");
            }

            return new Session
            {
                _isActive = true,
                _user = user
            };
        }
    }
}
