﻿using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class LoginRepository
    {
        private readonly GuapMessengerContext _context;

        public LoginRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task AddLoginAsync(Login login, CancellationToken cancellationToken = default)
        {
            await _context.Logins.AddAsync(login, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Login>> GetLoginByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Logins
                .Where(l => l.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateLoginAsync(Login login, CancellationToken cancellationToken = default)
        {
            _context.Logins.Update(login);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
