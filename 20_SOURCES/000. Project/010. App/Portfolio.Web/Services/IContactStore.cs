using PortfolioApp.Models;

namespace PortfolioApp.Services;

public interface IContactStore
{
    Task<List<ContactMessage>> GetAllAsync(string slug);
    Task SaveAsync(string slug, ContactMessage msg);
    Task DeleteAsync(string slug, string msgId);
    Task MarkReadAsync(string slug, string msgId);
}
