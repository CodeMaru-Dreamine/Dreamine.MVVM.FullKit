using DreamineWeb.Models;

namespace DreamineWeb.Services;

public interface IPlaygroundStore
{
    Task<List<PlaygroundDemo>> GetAllAsync();
    Task<PlaygroundDemo?> GetAsync(string id);
    Task SaveAsync(PlaygroundDemo demo);
    Task DeleteAsync(string id);
}
