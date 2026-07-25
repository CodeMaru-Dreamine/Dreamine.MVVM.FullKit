using DreamineWeb.Models;

namespace DreamineWeb.Services;

public interface ILearningResourceStore
{
    Task<List<LearningResource>> GetAllAsync();
    Task SaveAsync(LearningResource resource);
    Task DeleteAsync(string id);
}
