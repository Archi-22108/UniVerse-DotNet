using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers.Api
{
    [ApiController]
    [Route("api/requests")]
    [Produces("application/json")]
    public class RequestsApiController : ControllerBase
    {
        private readonly IDeliveryRepository _deliveryRepo;

        public RequestsApiController(IDeliveryRepository deliveryRepo)
        {
            _deliveryRepo = deliveryRepo;
        }

        [HttpGet]
        public async Task<ActionResult<List<DeliveryRequestDetailDto>>> GetRequests([FromQuery] string? status)
        {
            var requests = await _deliveryRepo.GetRequestsAsync(status);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryRequestDetailDto>> GetRequestById(string id)
        {
            var req = await _deliveryRepo.GetRequestByIdAsync(id);
            if (req == null)
            {
                return NotFound(new { message = $"Delivery request {id} not found." });
            }
            return Ok(req);
        }

        [HttpPost]
        public async Task<ActionResult<DeliveryRequestDetailDto>> CreateRequest([FromBody] CreateDeliveryRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var request = new DeliveryRequest
            {
                RequesterId = dto.RequesterId,
                PickupLocation = dto.PickupLocation,
                DropoffLocation = dto.DropoffLocation,
                Instructions = dto.Instructions,
                TotalEstimatedAmount = dto.Items?.Sum(i => i.EstimatedPrice * i.Quantity) ?? 0.0,
                DeliveryFee = dto.DeliveryFee,
                Status = "pending"
            };

            var items = dto.Items?.Select(i => new RequestItem
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Notes = i.Notes,
                EstimatedPrice = i.EstimatedPrice
            }).ToList() ?? new List<RequestItem>();

            var requestId = await _deliveryRepo.CreateRequestAsync(request, items);
            return await GetRequestById(requestId);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateDeliveryStatusDto dto)
        {
            var success = await _deliveryRepo.UpdateStatusAsync(id, dto.Status);
            if (!success)
            {
                return NotFound(new { message = "Request not found." });
            }
            return Ok(new { message = $"Status updated to {dto.Status} via ADO.NET." });
        }
    }
}
