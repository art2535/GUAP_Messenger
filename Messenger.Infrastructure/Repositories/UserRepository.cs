using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class UserRepository
    {
        private readonly GuapMessengerContext _context;

        public UserRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task<string?> GetRoleByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var roleName = await _context.Set<Dictionary<string, object>>("UserRole")
                .Where(ur => (Guid)ur["UserId"] == userId)
                .Select(ur => _context.Set<Role>()
                    .Where(r => r.RoleId == (Guid)ur["RoleId"]).Select(r => r.Name).FirstOrDefault())
                .FirstOrDefaultAsync(cancellationToken);

            return roleName;
        }

        public async Task AddUserToBlacklistAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            bool alreadyExists = await _context.Blacklists
                .AnyAsync(b => b.UserId == userId && b.BlockedUserId == blockedUserId, token);

            if (alreadyExists)
                return;

            var blockedEntry = new Blacklist
            {
                UserId = userId,
                BlockedUserId = blockedUserId,
                BlockDate = DateTime.Now
            };

            _context.Blacklists.Add(blockedEntry);
            await _context.SaveChangesAsync(token);
        }

        public async Task AssignUserRoleAsync(Guid userId, Guid roleId, CancellationToken token = default)
        {
            var exists = await _context.Set<Dictionary<string, object>>("UserRole")
                .AnyAsync(ur => (Guid)ur["UserId"] == userId && (Guid)ur["RoleId"] == roleId, token);

            if (exists)
                return;

            var entry = new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["RoleId"] = roleId
            };

            _context.Set<Dictionary<string, object>>("UserRole").Add(entry);

            await _context.SaveChangesAsync(token);
        }

        public async Task DeleteUserAsync(Guid userId, CancellationToken token = default)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(token);
            }
        }

        public async Task AddUserAsync(User user, CancellationToken token = default)
        {
            await _context.Users.AddAsync(user, token);
            await _context.SaveChangesAsync(token);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken token = default)
        {
            return await _context.Users.ToListAsync(token);
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(CancellationToken token = default)
        {
            return await _context.Roles.ToListAsync(token);
        }

        public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken token = default)
        {
            return await _context.Users
                .Include(u => u.Account)
                .Include(u => u.UserStatus)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == id, token);
        }

        public async Task RemoveUserFromBlacklistAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            var blockedUser = await _context.Blacklists
                .FirstOrDefaultAsync(blockedUser => blockedUser.UserId == userId 
                    && blockedUser.BlockedUserId == blockedUserId, token);
            if (blockedUser != null)
            {
                _context.Blacklists.Remove(blockedUser);
                await _context.SaveChangesAsync(token);
            }
        }

        public async Task UpdateUserAsync(User user, CancellationToken token = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(token);
        }
    }
}
