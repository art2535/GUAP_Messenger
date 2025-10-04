using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Messenger.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Messenger.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepository;
        private readonly GuapMessengerContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public UserService(UserRepository userRepository, GuapMessengerContext context, 
            IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _userRepository = userRepository;
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken token = default)
        {
            await _userRepository.AssignUserRoleAsync(userId, roleId, token);
        }

        public async Task BlockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            await _userRepository.AddUserToBlacklistAsync(userId, blockedUserId, token);
        }

        public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken token = default)
        {
            var identityUser = await _userManager.FindByIdAsync(userId.ToString());
            var result = await _userManager.ChangePasswordAsync(identityUser, oldPassword, newPassword);

            if (!result.Succeeded)
            {
                throw new Exception("Пароль не изменен!");
            }
        }

        public async Task DeleteAccountAsync(Guid userId, CancellationToken token = default)
        {
            var identityUser = await _userManager.FindByIdAsync(userId.ToString());

            await _userManager.DeleteAsync(identityUser);
            await _userRepository.DeleteUserAsync(userId, token);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken token = default)
        {
            return await _userRepository.GetAllUsersAsync(token);
        }

        public async Task<IEnumerable<Role>> GetRolesAsync(CancellationToken token = default)
        {
            return await _userRepository.GetUserRolesAsync(token);
        }

        public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken token = default)
        {
            return await _userRepository.GetUserByIdAsync(id, token);
        }

        public async Task<string> LoginAsync(string login, string password, CancellationToken token = default)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login && u.Password == password, token);

                if (user == null)
                {
                    throw new Exception("Авторизация не прошла");
                }

                return await GenerateJwtToken(user, token);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<string> GenerateJwtToken(User user, CancellationToken token = default)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Login)
            };

            var userRoles = await _context.Users
                .Where(u => u.UserId == user.UserId)
                .SelectMany(u => u.Roles)
                .ToListAsync(token);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        public async Task<(User? user, string? token)> RegisterAsync(string login, string password, string firstName, 
            string? middleName, string lastName, string phone, DateTime birthDate, Guid? roleId = null, 
            CancellationToken token = default)
        {
            var registerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password, token);
            
            if (registerUser == null)
            {
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Login = login,
                    Password = password,
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = phone,
                    BirthDate = DateOnly.FromDateTime(birthDate),
                    RegistrationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Account = new AccountSetting
                    {
                        SettingId = Guid.NewGuid(),
                        AccountId = Guid.NewGuid()
                    },
                    UserStatus = new UserStatus
                    {
                        UserId = Guid.NewGuid(),
                        Online = false
                    }
                };

                await _userRepository.AddUserAsync(user, token);
                if (roleId.HasValue)
                {
                    await _userRepository.AssignUserRoleAsync(user.UserId, roleId.Value, token);
                }

                string jwtToken = await GenerateJwtToken(user, token);

                return (user, jwtToken);
            }

            return (registerUser, null);
        }

        public async Task UnblockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            await _userRepository.RemoveUserFromBlacklistAsync(userId, blockedUserId, token);
        }

        public async Task UpdateProfileAsync(User user, string? avatarUrl = null, CancellationToken token = default)
        {
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                user.Account.Avatar = avatarUrl;
            }
            await _userRepository.UpdateUserAsync(user, token);
        }

        public async Task<User?> LogoutAsync(string login, string password, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password, cancellationToken);
        }
    }
}
