using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Messenger.Infrastructure.Repositories;
using Messenger.Core.DTOs.Users;
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

        public UserService(UserRepository userRepository, GuapMessengerContext context, 
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _context = context;
            _configuration = configuration;
        }

        public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken token = default)
        {
            await _userRepository.AssignUserRoleAsync(userId, roleId, token);
        }

        public async Task BlockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            await _userRepository.AddUserToBlacklistAsync(userId, blockedUserId, token);
        }

        public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, 
            CancellationToken token = default)
        {
            var user = await _userRepository.GetUserByIdAsync(userId, token)
                ?? throw new UnauthorizedAccessException("Пользователь не найден");

            if (!ValidationService.VerifyPassword(oldPassword, user.Password))
            {
                throw new Exception("Старый пароль указан неверно");
            }

            newPassword = ValidationService.ValidatePassword(newPassword);
            user.Password = ValidationService.HashPassword(newPassword);

            await _userRepository.UpdateUserAsync(user, token);
        }

        public async Task DeleteAccountAsync(Guid userId, CancellationToken token = default)
        {
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
            var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login, token)
                    ?? throw new UnauthorizedAccessException($"Пользователь с email {login} не найден");

            bool isPasswordValid = ValidationService.VerifyPassword(password, user.Password);

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Неверный логин или пароль");
            }

            return await GenerateJwtToken(user, token);
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
                login = ValidationService.ValidateEmail(login);
                var birthDateOnly = ValidationService.ValidateBirthdate(birthDate);
                password = ValidationService.ValidatePassword(password);
                
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Login = login,
                    Password = ValidationService.HashPassword(password),
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    Phone = phone,
                    BirthDate = birthDateOnly,
                    RegistrationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Account = new AccountSetting
                    {
                        SettingId = Guid.NewGuid(),
                        AccountId = Guid.NewGuid()
                    },
                    UserStatus = new UserStatus
                    {
                        UserId = Guid.NewGuid(),
                        Online = true
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

        public async Task UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, 
            string? avatarUrl = null, CancellationToken token = default)
        {
            var user = await _context.Users
                .Include(u => u.Account)
                .FirstOrDefaultAsync(u => u.UserId == userId, token);

            if (user == null)
                throw new UnauthorizedAccessException("Пользователь не найден");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.MiddleName = request.MiddleName;
            user.Login = request.Login;
            user.Phone = request.Phone;

            if (!string.IsNullOrEmpty(request.Theme) && user.Account != null)
                user.Account.Theme = request.Theme;

            if (!string.IsNullOrEmpty(avatarUrl) && user.Account != null)
                user.Account.Avatar = avatarUrl;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(token);
        }
    }
}
