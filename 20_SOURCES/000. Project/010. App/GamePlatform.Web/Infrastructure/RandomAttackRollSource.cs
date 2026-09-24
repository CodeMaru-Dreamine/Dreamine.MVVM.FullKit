using System.Security.Cryptography;
using GamePlatform.Application;

namespace GamePlatform.Infrastructure;

/// <summary>서버 암호화 난수로 데미지 변동과 치명타 롤을 공급합니다.</summary>
public sealed class RandomAttackRollSource : IAttackRollSource
{
    /// <inheritdoc />
    public int NextRoll() => RandomNumberGenerator.GetInt32(100);
}
