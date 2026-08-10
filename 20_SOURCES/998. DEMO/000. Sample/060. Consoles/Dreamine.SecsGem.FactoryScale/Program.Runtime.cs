using Dreamine.SecsGem.FactoryScale.Cli;

namespace Dreamine.SecsGem.FactoryScale;

internal static partial class Program
{
    static partial void ConfigureExecutor(ref IFactoryCommandExecutor? executor) =>
        executor = new FactoryCommandExecutor();
}
