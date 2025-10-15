using Messenger.Core.DTOs.Reactions;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Messenger.API.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Контроллер для управления реакциями на сообщения")]
    public class ReactionsController : ControllerBase
    {
        private readonly IReactionService _reactionService;

        public ReactionsController(IReactionService reactionService)
        {
            _reactionService = reactionService;
        }

        [HttpGet("{messageId}")]
        [SwaggerOperation(
            Summary = "Получение всех реакций на сообщение",
            Description = "Возвращает список реакций (например, лайки, эмодзи) для указанного сообщения. " +
                          "Требуется авторизация по JWT-токену.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetReactionsByMessageAsync(Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var reactions = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = reactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("{messageId}")]
        [SwaggerOperation(
            Summary = "Добавление новой реакции к сообщению",
            Description = "Позволяет пользователю добавить реакцию (например, эмодзи) к сообщению. " +
                          "Идентификатор пользователя определяется из JWT-токена.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> AddReactionAsync(Guid messageId, [FromBody] CreateReactionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var reaction = new Reaction
                {
                    ReactionId = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    ReactionType = request.ReactionType
                };

                await _reactionService.AddReactionAsync(reaction, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Статус пользователя обновлен"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpDelete("{messageId}")]
        [SwaggerOperation(
            Summary = "Удаление реакции с сообщения",
            Description = "Удаляет реакцию текущего пользователя с указанного сообщения. " +
                          "Требуется авторизация по JWT-токену.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteReactionAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reaction = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                if (reaction == null)
                {
                    return NotFound(new
                    {
                        IsSuccess = false,
                        Error = "Реакция на сообщение не найдена"
                    });
                }

                await _reactionService.DeleteReactionAsync(messageId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Реакция успешно удалена"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }
    }
}
