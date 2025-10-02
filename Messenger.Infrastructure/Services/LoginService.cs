﻿using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class LoginService : ILoginService
    {
        private readonly LoginRepository _loginRepository;

        public LoginService(LoginRepository loginRepository)
        {
            _loginRepository = loginRepository;
        }

        public async Task AddLoginAsync(Login login, CancellationToken cancellationToken = default)
        {
            await _loginRepository.AddLoginAsync(login, cancellationToken);
        }

        public async Task<IEnumerable<Login>> GetLoginsByUserIdAsync(Guid userId, 
            CancellationToken cancellationToken = default)
        {
            return await _loginRepository.GetLoginByUserIdAsync(userId, cancellationToken);
        }
    }
}
