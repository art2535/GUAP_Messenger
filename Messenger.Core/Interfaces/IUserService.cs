using Messenger.Core.DTOs.Users;
using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IUserService
    {
        Task<(User? user, string? token, string? role)> RegisterAsync(string login, string password, string firstName, 
            string? middleName,  string lastName, string phone, DateTime birthDate, Guid? roleId = null, 
            CancellationToken token = default);
        Task<(string token, Guid userId, string role)> LoginAsync(string login, string password, CancellationToken token = default);
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken token = default);
        Task<bool> IsBlockedByAsync(Guid blockerId, Guid blockedId, CancellationToken token = default);
        Task<IEnumerable<User>> GetBlockedUsersAsync(Guid userId, CancellationToken token = default);
        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken token = default);
        Task BlockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default);
        Task UnblockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default);
        Task UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, string? avatarUrl = null, 
            CancellationToken token = default);
        Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken token = default);
        Task DeleteAccountAsync(Guid userId, CancellationToken token = default);
        Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken token = default);
        Task<IEnumerable<Role>> GetRolesAsync(CancellationToken token = default);
        Task<IEnumerable<UserSearch>> SearchUsersAsync(string query, CancellationToken token = default);
    }
}
