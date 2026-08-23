using Asp.Versioning;
using Messenger.API.Responses;
using Messenger.API.Services;
using Messenger.Core.DTOs.Reactions;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace Messenger.API.Controllers
{
    /// <summary>
    /// Контроллер для управления реакциями на сообщения
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Tags("Reactions")]
    public class ReactionsController : ControllerBase
    {
        private readonly IReactionService _reactionService;
        private readonly IUserService _userService;

        public ReactionsController(IReactionService reactionService, IUserService userService)
        {
            _reactionService = reactionService;
            _userService = userService;
        }

        /// <summary>
        /// Получить все реакции на сообщение
        /// </summary>
        [HttpGet("{messageId}")]
        [EndpointName("GetReactionsByMessage")]
        [EndpointSummary("Получить все реакции на сообщение")]
        [EndpointDescription("Возвращает список всех реакций (эмодзи) на указанное сообщение, включая информацию о пользователе и тип реакции.")]
        [ProducesResponseType(typeof(GetReactionsSuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetReactionsByMessageAsync(
            [Description("Идентификатор сообщения (GUID)")] Guid messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var reactions = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new GetReactionsSuccessResponse
                {
                    IsSuccess = true,
                    Data = reactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Добавить реакцию на сообщение
        /// </summary>
        [HttpPost("{messageId}")]
        [EndpointName("AddReaction")]
        [EndpointSummary("Добавить реакцию на сообщение")]
        [EndpointDescription("Добавляет реакцию (эмодзи) от имени текущего авторизованного пользователя к указанному сообщению.")]
        [ProducesResponseType(typeof(AddReactionSuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddReactionAsync(
            [Description("Идентификатор сообщения (GUID)")] Guid messageId,
            [FromBody, Description("Данные реакции")] CreateReactionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (user, error) = await UserValidationService.GetCurrentUserOrErrorAsync(User, _userService);
                if (error != null)
                {
                    return error;
                }

                var reaction = new Reaction
                {
                    ReactionId = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = user!.UserId,
                    ReactionType = request.ReactionType
                };

                await _reactionService.AddReactionAsync(reaction, cancellationToken);

                return Ok(new AddReactionSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Статус пользователя обновлен"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Сообщение не найдено"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Удалить свою реакцию с сообщения
        /// </summary>
        [HttpDelete("{messageId}")]
        [EndpointName("DeleteReaction")]
        [EndpointSummary("Удалить свою реакцию с сообщения")]
        [EndpointDescription("Удаляет реакцию текущего авторизованного пользователя с указанного сообщения.")]
        [ProducesResponseType(typeof(DeleteReactionSuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteReactionAsync(
            [Description("Идентификатор сообщения (GUID)")] Guid messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reaction = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                if (reaction == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Реакция на сообщение не найдена"
                    });
                }

                await _reactionService.DeleteReactionAsync(messageId, cancellationToken);

                return Ok(new DeleteReactionSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Реакция успешно удалена"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }
    }
}
