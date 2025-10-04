using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IUserService
    {
        Task<(User? user, string? token)> RegisterAsync(string login, string password, string firstName, string? middleName, 
            string lastName, string phone, DateTime birthDate, Guid? roleId = null, CancellationToken token = default);
        Task<string> LoginAsync(string login, string password, CancellationToken token = default);
        Task<User?> LogoutAsync(string login, string password, CancellationToken cancellationToken = default);
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken token = default);
        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken token = default);
        Task BlockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default);
        Task UnblockUserAsync(Guid userId, Guid blockedUserId, CancellationToken token = default);
        Task UpdateProfileAsync(User user, string? avatarUrl = null, CancellationToken token = default);
        Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken token = default);
        Task DeleteAccountAsync(Guid userId, CancellationToken token = default);
        Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken token = default);
        Task<IEnumerable<Role>> GetRolesAsync(CancellationToken token = default);
    }
}
