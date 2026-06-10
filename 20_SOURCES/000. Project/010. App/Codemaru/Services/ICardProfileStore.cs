using Codemaru.Models;

namespace Codemaru.Services;

/// <summary>
/// \brief Defines persistent storage for CardHybrid profile data.
/// </summary>
public interface ICardProfileStore
{
    Task<CardHybridUser?> LoadLastUserAsync(CancellationToken cancellationToken = default);

    Task SaveLastUserAsync(CardHybridUser user, CancellationToken cancellationToken = default);

    Task<CardHybridUser?> LoadUserAsync(string userId, CancellationToken cancellationToken = default);

    Task SaveUserAsync(CardHybridUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief Loads the previously saved CardHybrid snapshot.
    /// </summary>
    /// <param name="userId">The signed-in user ID.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The saved snapshot, or null when no saved data exists.</returns>
    Task<CardHybridSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief Saves the CardHybrid snapshot.
    /// </summary>
    /// <param name="userId">The signed-in user ID.</param>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task SaveAsync(string userId, CardHybridSnapshot snapshot, CancellationToken cancellationToken = default);
}
