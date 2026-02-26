using Messenger.Core.DTOs.Users;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Messenger.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
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

        public async Task<(string token, Guid userId, string role)> LoginAsync(string login, string password, CancellationToken token = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Login == login, token)
                ?? throw new UnauthorizedAccessException($"Пользователь с логином {login} не найден");

            if (!ValidationService.VerifyPassword(password, user.Password))
            {
                throw new UnauthorizedAccessException("Неверный логин или пароль");
            }

            string? role = await _userRepository.GetRoleByUserIdAsync(user.UserId, token);

            role ??= "Пользователь";

            string jwtToken = await new JwtService(_configuration, _context)
                .GenerateJwtTokenAsync(user, token);

            return (jwtToken, user.UserId, role);
        }

        public async Task<(User? user, string? token, string? role)> RegisterAsync(string login, string password, string firstName, 
            string? middleName, string lastName, string phone, DateTime birthDate, Guid? roleId = null, 
            CancellationToken token = default)
        {
            var existingUser = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Login == login, token);

            if (existingUser != null)
            {
                return (existingUser, null, null);
            }

            login = ValidationService.ValidateEmail(login);
            password = ValidationService.ValidatePassword(password);
            var birthDateOnly = ValidationService.ValidateBirthdate(birthDate);

            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
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
                    AccountId = userId
                },
                UserStatus = new UserStatus
                {
                    UserId = userId,
                    Online = true
                }
            };

            await _userRepository.AddUserAsync(user, token);

            if (roleId.HasValue)
            {
                await _userRepository.AssignUserRoleAsync(userId, roleId.Value, token);
            }

            Guid roleToAssign;

            if (roleId.HasValue)
            {
                roleToAssign = roleId.Value;
            }
            else
            {
                var defaultRole = await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Name == "Пользователь", token)
                    ?? throw new InvalidOperationException("Роль по умолчанию 'Пользователь' не найдена в базе данных");

                roleToAssign = defaultRole.RoleId;
            }

            await _userRepository.AssignUserRoleAsync(userId, roleToAssign, token);

            string jwtToken = await new JwtService(_configuration, _context)
                .GenerateJwtTokenAsync(user, token);

            var userRoleName = await _userRepository.GetRoleByUserIdAsync(userId, token)
                ?? "Пользователь";

            return (user, jwtToken, userRoleName);
        }

        public async Task<string> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken token = default)
        {
            var user = await _context.Users
                .Include(u => u.Account)
                .FirstOrDefaultAsync(u => u.UserId == userId, token);

            if (user == null)
                throw new UnauthorizedAccessException("Пользователь не найден");

            if (user.Account == null)
            {
                user.Account = new AccountSetting
                {
                    SettingId = Guid.NewGuid(),
                    AccountId = userId
                };
                _context.AccountSettings.Add(user.Account);
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(user.Account.Avatar))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Account.Avatar.TrimStart('/'));
                if (File.Exists(oldFilePath) && !user.Account.Avatar.Contains("default"))
                {
                    File.Delete(oldFilePath);
                }
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Неподдерживаемый формат изображения");

            var fileName = $"{userId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, token);
            }

            var avatarUrl = $"https://localhost:7001/avatars/{fileName}";

            user.Account.Avatar = avatarUrl;
            await _context.SaveChangesAsync(token);

            return avatarUrl;
        }

        public async Task DeleteAvatarAsync(Guid userId, CancellationToken token = default)
        {
            var user = await _context.Users
                .Include(u => u.Account)
                .FirstOrDefaultAsync(u => u.UserId == userId, token);

            if (user == null || user.Account == null || string.IsNullOrEmpty(user.Account.Avatar))
                return;

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Account.Avatar.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            user.Account.Avatar = null;
            await _context.SaveChangesAsync(token);
        }

        public async Task UnblockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            await _userRepository.RemoveUserFromBlacklistAsync(userId, blockedUserId, token);
        }

        public async Task<IEnumerable<UserSearch>> SearchUsersAsync(string search, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(search))
                return Enumerable.Empty<UserSearch>();

            search = search.ToLower().Trim();
            string searchPattern = $"%{search}%";

            var usersData = await _context.Users
                .AsNoTracking()
                .Include(u => u.Account)
                .Where(u =>
                    EF.Functions.Like((u.FirstName ?? "").ToLower(), searchPattern) ||
                    EF.Functions.Like((u.LastName ?? "").ToLower(), searchPattern) ||
                    EF.Functions.Like((u.Login ?? "").ToLower(), searchPattern) ||
                    EF.Functions.Like(((u.FirstName + " " + u.LastName) ?? "").ToLower(), searchPattern) ||
                    EF.Functions.Like(((u.LastName + " " + u.FirstName) ?? "").ToLower(), searchPattern)
                )
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(15)
                .ToListAsync(token);

            return usersData
                .Select(u => new UserSearch
                {
                    Id = u.UserId,
                    Name = string.Join(" ", new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrEmpty(s))),
                    Avatar = u.Account?.Avatar
                })
                .ToList();
        }

        public async Task<IEnumerable<User>> GetBlockedUsersAsync(Guid userId, CancellationToken token = default)
        {
            return await _context.Blacklists
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .Include(b => b.BlockedUser)
                    .ThenInclude(u => u.Account)
                .Select(b => b.BlockedUser!)
                .ToListAsync(token);
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

        public async Task<bool> IsBlockedByAsync(Guid blockerId, Guid blockedId, CancellationToken token = default)
        {
            return await _context.Blacklists
                .AnyAsync(ub => ub.UserId == blockerId && ub.BlockedUserId == blockedId, token);
        }
    }
}
