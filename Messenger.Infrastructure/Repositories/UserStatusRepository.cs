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
            var rowsAffected = await _context.UserStatuses
                .Where(us => us.UserId == userStatus.UserId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(us => us.Online, userStatus.Online)
                        .SetProperty(us => us.LastActivity, DateTime.Now),
                    cancellationToken);

            if (rowsAffected == 0)
            {
                var newStatus = new UserStatus
                {
                    UserId = userStatus.UserId,
                    Online = userStatus.Online,
                    LastActivity = DateTime.Now
                };

                _context.UserStatuses.Add(newStatus);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<UserStatus?> GetUserStatusByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserStatuses
                .FirstOrDefaultAsync(us => us.UserId == userId, cancellationToken);
        }
    }
}
