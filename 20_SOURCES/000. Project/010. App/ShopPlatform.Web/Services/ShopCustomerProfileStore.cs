using System.Text.Json;
using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>Shop Customer Profile Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop customer profile store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopCustomerProfileStore
{
    /// <summary>
    /// \if KO
    /// <para>root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root value.</para>
    /// \endif
    /// </summary>
    private readonly string _root;
    /// <summary>
    /// \if KO
    /// <para>Json Options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json options value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="ShopCustomerProfileStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ShopCustomerProfileStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    public ShopCustomerProfileStore(ShopOptions options)
    {
        _root = options.ResolvedDataPath;
    }

    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ShopCustomerProfile?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ShopCustomerProfile?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ShopCustomerProfile?> GetAsync(string slug, string userId)
    {
        var profiles = await LoadAsync(slug).ConfigureAwait(false);
        return profiles.FirstOrDefault(x => string.Equals(x.UserId, userId, StringComparison.Ordinal));
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>ShopCustomerProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopCustomerProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveAsync(string slug, ShopCustomerProfile profile)
    {
        var profiles = await LoadAsync(slug).ConfigureAwait(false);
        var index = profiles.FindIndex(x => string.Equals(x.UserId, profile.UserId, StringComparison.Ordinal));
        profile.UpdatedAt = DateTime.UtcNow;

        if (index >= 0)
        {
            profiles[index] = profile;
        }
        else
        {
            profiles.Add(profile);
        }

        Directory.CreateDirectory(ShopDir(slug));
        await File.WriteAllTextAsync(ProfilePath(slug), JsonSerializer.Serialize(profiles, JsonOptions)).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task&lt;List&lt;ShopCustomerProfile&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;ShopCustomerProfile&gt;&gt;</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    private async Task<List<ShopCustomerProfile>> LoadAsync(string slug)
    {
        var path = ProfilePath(slug);
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ShopCustomerProfile>>(stream, JsonOptions).ConfigureAwait(false) ?? [];
    }

    /// <summary>
    /// \if KO
    /// <para>Shop Dir 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the shop dir operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Shop Dir 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the shop dir operation.</para>
    /// \endif
    /// </returns>
    private string ShopDir(string slug) => Path.Combine(_root, slug);
    /// <summary>
    /// \if KO
    /// <para>Profile Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the profile path operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Profile Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the profile path operation.</para>
    /// \endif
    /// </returns>
    private string ProfilePath(string slug) => Path.Combine(ShopDir(slug), "customers.json");
}
