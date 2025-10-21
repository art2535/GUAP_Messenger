using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Messenger.Infrastructure.Repositories;
using Messenger.Core.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

        public async Task<(string token, string role)> LoginAsync(string login, string password, CancellationToken token = default)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Login == login, token)
                ?? throw new UnauthorizedAccessException($"Пользователь с логином {login} не найден");

            bool isPasswordValid = ValidationService.VerifyPassword(password, user.Password);

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Неверный логин или пароль");
            }

            var role = user.Roles.FirstOrDefault()?.Name ?? "User";

            string jwtToken = await new JwtService(_configuration, _context)
                .GenerateJwtTokenAsync(user, token);

            return (jwtToken, role);
        }

        public async Task<(User? user, string? token, string? role)> RegisterAsync(string login, string password, string firstName, 
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

                string jwtToken = await new JwtService(_configuration, _context).GenerateJwtTokenAsync(user, token);

                var userRole = await _userRepository.GetRoleByUserIdAsync(user.UserId);

                return (user, jwtToken, userRole);
            }

            return (registerUser, null, null);
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
