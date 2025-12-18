using ShareSphere.Api.Models;
using ShareSphere. Api.Models.Dtos;

namespace ShareSphere.Api.Services
{
    public interface IShareholderService
    {
        Task<IEnumerable<Shareholder>> GetAllAsync();
        Task<Shareholder?> GetByIdAsync(int shareholderId);
        Task<Shareholder?> GetByEmailAsync(string email);
        Task<Shareholder> CreateAsync(Shareholder shareholder);
        Task<Shareholder? > UpdateAsync(int shareholderId, Shareholder shareholder);
        Task<bool> DeleteAsync(int shareholderId);

                Task<ShareholderPortfolioDto?> GetShareholderPortfolioAsync(int shareholderId);

    }
}