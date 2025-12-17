using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareSphere.Api.Models;
using ShareSphere.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace ShareSphere.Api.Controllers
{
    [ApiController]
    [Route("api/trades")]
    public class TradeController : ControllerBase
    {
        private readonly ITradeService _tradeService;

        public TradeController(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        // DTO for creating/updating trades
        public record TradeRequest(
            [Required] int CompanyId,
            [Required] int BrokerId,
            [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero")] int Quantity,
            [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price per share must be greater than zero")] decimal PricePerShare,
            [Required] DateTime TradeDate,
            [Required] TradeType Type
        );

        /// <summary>
        /// Gibt alle Trades zurück
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var trades = await _tradeService.GetAllAsync();
            return Ok(trades);
        }

        /// <summary>
        /// Gibt einen spezifischen Trade nach ID zurück
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var trade = await _tradeService. GetByIdAsync(id);
            if (trade == null)
                return NotFound(new { message = $"Trade with ID {id} not found." });

            return Ok(trade);
        }

        /// <summary>
        /// Gibt alle Trades für ein bestimmtes Unternehmen zurück
        /// </summary>
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompanyId(int companyId)
        {
            var trades = await _tradeService.GetByCompanyIdAsync(companyId);
            return Ok(trades);
        }

        /// <summary>
        /// Gibt alle Trades für einen bestimmten Broker zurück
        /// </summary>
        [HttpGet("broker/{brokerId}")]
        public async Task<IActionResult> GetByBrokerId(int brokerId)
        {
            var trades = await _tradeService.GetByBrokerIdAsync(brokerId);
            return Ok(trades);
        }

        /// <summary>
        /// Erstellt einen neuen Trade (für Admins und Users)
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TradeRequest request)
        {
            var trade = new Trade
            {
                CompanyId = request.CompanyId,
                BrokerId = request.BrokerId,
                Quantity = request.Quantity,
                UnitPrice = request.PricePerShare,
                Timestamp = request.TradeDate,
                Type = request.Type
            };

            var created = await _tradeService.CreateAsync(trade);
            return CreatedAtAction(nameof(GetById), new { id = created. TradeId }, created);
        }

        /// <summary>
        /// Aktualisiert einen bestehenden Trade (für Admins und Users)
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TradeRequest request)
        {
            var trade = new Trade
            {
                CompanyId = request.CompanyId,
                BrokerId = request.BrokerId,
                Quantity = request.Quantity,
                UnitPrice = request.PricePerShare,
                Timestamp = request.TradeDate,
                Type = request.Type
            };

            var updated = await _tradeService.UpdateAsync(id, trade);
            if (updated == null)
                return NotFound(new { message = $"Trade with ID {id} not found." });

            return Ok(updated);
        }

        /// <summary>
        /// Löscht einen Trade (nur für Admins)
        /// </summary>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _tradeService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Trade with ID {id} not found." });

            return NoContent();
        }

        
    }
}