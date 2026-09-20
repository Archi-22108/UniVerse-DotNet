using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers.Api
{
    [ApiController]
    [Route("api/runner")]
    [Produces("application/json")]
    public class RunnerApiController : ControllerBase
    {
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IUserRepository _userRepo;

        public RunnerApiController(IDeliveryRepository deliveryRepo, IUserRepository userRepo)
        {
            _deliveryRepo = deliveryRepo;
            _userRepo = userRepo;
        }

        [HttpPost("toggle-duty")]
        public async Task<IActionResult> ToggleDuty([FromBody] ToggleRunnerDutyDto dto)
        {
            int rows = await _userRepo.ToggleRunnerDutyAsync(dto.RunnerId, dto.IsActive);
            if (rows == 0)
            {
                return NotFound(new { message = "Runner not found or user is not a runner." });
            }
            return Ok(new { message = $"Runner duty status set to {(dto.IsActive ? "ACTIVE" : "OFF-DUTY")}." });
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<UserDto>>> GetAvailableRunners()
        {
            var runners = await _userRepo.GetActiveRunnersAsync();
            var dtos = new List<UserDto>();
            foreach (var r in runners)
            {
                dtos.Add(UserDto.FromEntity(r));
            }
            return Ok(dtos);
        }

        [HttpPost("accept/{requestId}")]
        public async Task<IActionResult> AcceptDelivery(string requestId, [FromBody] AcceptDeliveryDto dto)
        {
            var success = await _deliveryRepo.AssignRunnerAsync(requestId, dto.RunnerId);
            if (!success)
            {
                return Conflict(new { message = "Delivery request is no longer pending or does not exist." });
            }
            return Ok(new { message = "Delivery request successfully accepted by runner." });
        }

        [HttpPost("complete/{requestId}")]
        public async Task<IActionResult> CompleteDelivery(string requestId, [FromBody] CompleteDeliveryDto dto)
        {
            var result = await _deliveryRepo.CompleteDeliveryWithOtpAsync(requestId, dto.RunnerId, dto.DeliveryOtp);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new
            {
                message = result.Message,
                rewardEarned = result.Reward
            });
        }
    }
}
