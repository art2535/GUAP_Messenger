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

        public async Task AddUserToBlacklistAsync(Guid userId, Guid blockedUserId, CancellationToken token = default)
        {
            var blockedList = new Blacklist
            {
                UserId = userId,
                BlockedUserId = blockedUserId,
                BlockDate = DateTime.UtcNow,
            };

            await _context.Blacklists.AddAsync(blockedList, token);
            await _context.SaveChangesAsync(token);
        }

        public async Task AssignUserRoleAsync(Guid userId, Guid roleId, CancellationToken token = default)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == userId, token);

            var role = await _context.Roles.FindAsync(roleId, token);
            if (user != null && role != null)
            {
                user.Roles.Add(role);
                await _context.SaveChangesAsync(token);
            }
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
                .Include(user => user.Account)
                .Include(user => user.UserStatus)
                .Include(user => user.BlacklistBlockedUsers)
                    .ThenInclude(blocked => blocked.BlockedUser)
                .Include(user => user.Roles)
                .FirstOrDefaultAsync(user => user.UserId == id, token);
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
