using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class ChatRepository
    {
        private readonly GuapMessengerContext _context;

        public ChatRepository(GuapMessengerContext context)
        {
            _context = context;
        }
        
        public async Task AddChatAsync(Chat chat, CancellationToken token = default)
        {
            await _context.Chats.AddAsync(chat, token);
            await _context.SaveChangesAsync(token);
        }

        public async Task AddParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
        {
            await _context.ChatParticipants.AddAsync(participant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateChatAsync(Chat chat, CancellationToken token = default)
        {
            _context.Chats.Update(chat);
            await _context.SaveChangesAsync(token);
        }

        public async Task DeleteParticipantAsync(ChatParticipant participant, CancellationToken token = default)
        {
            _context.ChatParticipants.Remove(participant);
            await _context.SaveChangesAsync(token);
        }

        public async Task DeleteChatAsync(Chat chat, CancellationToken token = default)
        {
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync(token);
        }

        public async Task<Chat?> GetChatByIdAsync(Guid chatId, CancellationToken token = default)
        {
            return await _context.Chats
                .FirstOrDefaultAsync(c => c.ChatId == chatId, token);
        }

        public async Task<IEnumerable<Chat>> GetChatsByUserIdAsync(Guid userId, CancellationToken token = default)
        {
            return await _context.Chats
                .Include(c => c.ChatParticipants)
                    .ThenInclude(cp => cp.User)
                        .ThenInclude(u => u.Account)
                .Include(c => c.Messages)
                .Where(c => c.ChatParticipants.Any(cp => cp.UserId == userId))
                .ToListAsync(token);
        }

        public async Task<IEnumerable<ChatParticipant>> GetParticipantsByChatAsync(Guid chatId, 
            CancellationToken token = default)
        {
            return await _context.ChatParticipants
                .Where(part => part.ChatId == chatId)
                .Include(p => p.User)
                    .ThenInclude(u => u.Account)
                .ToListAsync(token);
        }

        public async Task<ChatParticipant?> GetChatParticipantByChatAsync(Guid chatId, Guid userId,
            CancellationToken token = default)
        {
            return await _context.ChatParticipants
                .FirstOrDefaultAsync(part => part.ChatId == chatId && part.UserId == userId, token);
        }
    }
}
