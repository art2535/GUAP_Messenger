using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class UserStatusService : IUserStatusService
    {
        private readonly UserStatusRepository _userStatusRepository;

        public UserStatusService(UserStatusRepository userStatusRepository)
        {
            _userStatusRepository = userStatusRepository;
        }

        public async Task UpdateStatusAsync(UserStatus userStatus, CancellationToken cancellationToken = default)
        {
            await _userStatusRepository.UpdateUserStatusAsync(userStatus, cancellationToken);
        }

        public async Task<UserStatus?> GetStatusByUserIdAsync(Guid userId, 
            CancellationToken cancellationToken = default)
        {
            return await _userStatusRepository.GetUserStatusByUserIdAsync(userId, cancellationToken);
        }
    }
}
