using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class UserStatusRepository
    {
        private readonly GuapMessengerContext _context;

        public UserStatusRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task UpdateUserStatusAsync(UserStatus userStatus, CancellationToken cancellationToken = default)
        {
            _context.UserStatuses.Update(userStatus);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<UserStatus?> GetUserStatusByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserStatuses
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);
        }
    }
}
