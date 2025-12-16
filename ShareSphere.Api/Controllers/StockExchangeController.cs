using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareSphere.Api.Models;
using ShareSphere.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace ShareSphere.Api.Controllers
{
    [ApiController]
    [Route("api/stockexchanges")]
    public class StockExchangeController : ControllerBase
    {
        private readonly IStockExchangeService _stockExchangeService;

        public StockExchangeController(IStockExchangeService stockExchangeService)
        {
            _stockExchangeService = stockExchangeService;
        }

        // DTO for creating/updating stock exchanges
        public record StockExchangeRequest(
            [Required, MinLength(2), MaxLength(100)] string Name,
            [Required, MinLength(2), MaxLength(100)] string Country,
            [Required, StringLength(3, MinimumLength = 3)] 
            [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid 3-letter ISO code")]
            string Currency
        );

        /// <summary>
        /// Gibt alle Stock Exchanges zurück
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var exchanges = await _stockExchangeService.GetAllAsync();
            return Ok(exchanges);
        }

        /// <summary>
        /// Gibt eine spezifische Stock Exchange nach ID zurück
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var exchange = await _stockExchangeService.GetByIdAsync(id);
            if (exchange == null)
                return NotFound(new { message = $"Stock exchange with ID {id} not found." });

            return Ok(exchange);
        }

        /// <summary>
        /// Erstellt eine neue Stock Exchange (für Admins und Users)
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockExchangeRequest request)
        {
            var stockExchange = new StockExchange
            {
                Name = request.Name,
                Country = request.Country,
                Currency = request.Currency
            };

            var created = await _stockExchangeService.CreateAsync(stockExchange);
            return CreatedAtAction(nameof(GetById), new { id = created.ExchangeId }, created);
        }

        /// <summary>
        /// Aktualisiert eine bestehende Stock Exchange (für Admins und Users)
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StockExchangeRequest request)
        {
            var stockExchange = new StockExchange
            {
                Name = request.Name,
                Country = request.Country,
                Currency = request.Currency
            };

            var updated = await _stockExchangeService.UpdateAsync(id, stockExchange);
            if (updated == null)
                return NotFound(new { message = $"Stock exchange with ID {id} not found." });

            return Ok(updated);
        }

        /// <summary>
        /// Löscht eine Stock Exchange (nur für Admins)
        /// </summary>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _stockExchangeService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Stock exchange with ID {id} not found." });

            return NoContent();
        }
    }
}