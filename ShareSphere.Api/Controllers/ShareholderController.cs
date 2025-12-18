using Microsoft.AspNetCore. Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareSphere.Api.Models;
using ShareSphere.Api.Services;
using System.ComponentModel. DataAnnotations;
using ShareSphere.Api.Models.Dtos;

namespace ShareSphere.Api.Controllers
{
    [ApiController]
    [Route("api/shareholders")]
    public class ShareholderController : ControllerBase
    {
        private readonly IShareholderService _shareholderService;
        private readonly ISharePurchaseService _purchaseService;

// Constructor erweitern:
public ShareholderController(
    IShareholderService shareholderService,
    ISharePurchaseService purchaseService)
{
    _shareholderService = shareholderService;
    _purchaseService = purchaseService;
}

        // DTO for creating/updating shareholders
        public record ShareholderRequest(
            [Required, MinLength(1), MaxLength(100)] string Name,
            [Required, EmailAddress, MaxLength(100)] string Email,
            [Required, Range(0, double.MaxValue, ErrorMessage = "Portfolio value cannot be negative")] decimal PortfolioValue
        );

        /// <summary>
        /// Gibt alle Shareholders zurück
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shareholders = await _shareholderService.GetAllAsync();
            return Ok(shareholders);
        }

        /// <summary>
        /// Gibt einen spezifischen Shareholder nach ID zurück
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shareholder = await _shareholderService.GetByIdAsync(id);
            if (shareholder == null)
                return NotFound(new { message = $"Shareholder with ID {id} not found." });

            return Ok(shareholder);
        }

        /// <summary>
        /// Gibt einen Shareholder nach E-Mail-Adresse zurück
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var shareholder = await _shareholderService.GetByEmailAsync(email);
            if (shareholder == null)
                return NotFound(new { message = $"Shareholder with email '{email}' not found." });

            return Ok(shareholder);
        }

        /// <summary>
        /// Erstellt einen neuen Shareholder (für Admins und Users)
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShareholderRequest request)
        {
            var shareholder = new Shareholder
            {
                Name = request.Name,
                Email = request.Email,
                PortfolioValue = request.PortfolioValue
            };

            var created = await _shareholderService.CreateAsync(shareholder);
            return CreatedAtAction(nameof(GetById), new { id = created.ShareholderId }, created);
        }

        /// <summary>
        /// Aktualisiert einen bestehenden Shareholder (für Admins und Users)
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShareholderRequest request)
        {
            var shareholder = new Shareholder
            {
                Name = request.Name,
                Email = request.Email,
                PortfolioValue = request. PortfolioValue
            };

            var updated = await _shareholderService.UpdateAsync(id, shareholder);
            if (updated == null)
                return NotFound(new { message = $"Shareholder with ID {id} not found." });

            return Ok(updated);
        }

        /// <summary>
        /// Löscht einen Shareholder (nur für Admins)
        /// </summary>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _shareholderService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Shareholder with ID {id} not found." });

            return NoContent();
        }

      
// DTO für Share-Kauf
public record PurchaseShareRequest(
    [Required] int ShareId,
    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")] int Quantity,
    [Required] int BrokerId
);

/// <summary>
/// Kauft Shares für einen spezifischen Shareholder
/// </summary>
[Authorize(Roles = "admin,user")]
[HttpPost("{shareholderId}/purchase")]
public async Task<IActionResult> PurchaseShares(
    int shareholderId,
    [FromBody] PurchaseShareRequest request)
{
    var result = await _purchaseService.PurchaseSharesAsync(
        shareholderId,
        request.ShareId,
        request.Quantity,
        request.BrokerId
    );

    if (!result.Success)
    {
        return BadRequest(new { message = result.Message });
    }

    return Ok(new
    {
        message = result.Message,
        trade = result.Trade,
        portfolio = result.Portfolio
    });
    }  


        // DTO for share transactions
        public record ShareTransactionRequest(
            [Required] int ShareId,
            [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")] int Quantity,
            [Required] int BrokerId
        );

        /// <summary>
        /// Verkauft Shares eines Shareholders
        /// </summary>
        [Authorize(Roles = "admin,user")]
        [HttpPost("{id}/sell-shares")]
        public async Task<IActionResult> SellShares(int id, [FromBody] ShareTransactionRequest request)
        {
            var result = await _purchaseService.SellSharesAsync(
                id,
                request.ShareId,
                request.Quantity,
                request. BrokerId
            );

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                message = result.Message,
                trade = result.Trade,
                portfolio = result.Portfolio
            });
        }


        
        /// <summary>
        /// Gibt das komplette Portfolio eines Shareholders zurück mit: 
        /// - List of shares owned
        /// - Quantity per share
        /// - Current price per share
        /// - Total portfolio value
        /// </summary>
        /// <param name="id">Shareholder ID</param>
        /// <returns>Portfolio-Details</returns>
        [HttpGet("{id}/portfolio")]
        [ProducesResponseType(typeof(ShareholderPortfolioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPortfolio(int id)
        {
            var portfolio = await _shareholderService.GetShareholderPortfolioAsync(id);
            
            if (portfolio == null)
                return NotFound(new { message = $"Shareholder with ID {id} not found." });

            return Ok(portfolio);
        }
    
    }

    
}