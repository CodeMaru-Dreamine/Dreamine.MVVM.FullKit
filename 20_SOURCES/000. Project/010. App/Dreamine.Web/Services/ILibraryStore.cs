using DreamineWeb.Models;

namespace DreamineWeb.Services;

public interface ILibraryStore
{
    Task<List<LibraryInfo>> GetAllAsync();
    Task<LibraryInfo?> GetAsync(string id);
    Task SaveAsync(LibraryInfo lib);
    Task DeleteAsync(string id);
}
