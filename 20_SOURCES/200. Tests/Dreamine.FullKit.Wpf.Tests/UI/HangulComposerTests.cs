using System.Reflection;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

public class HangulComposerTests
{
    [Fact]
    public void ComposeTrailingConsonant()
    {
        var composer = CreateComposer();

        AssertEdit("ㄱ", 0, "ㄱ", composer, "");
        AssertEdit("ㅏ", 1, "가", composer, "ㄱ");
        AssertEdit("ㅁ", 1, "감", composer, "가");
    }

    [Fact]
    public void ComposeDoubleFinalConsonant()
    {
        var composer = CreateComposer();

        AssertEdit("ㄱ", 0, "ㄱ", composer, "");
        AssertEdit("ㅏ", 1, "가", composer, "ㄱ");
        AssertEdit("ㄹ", 1, "갈", composer, "가");
        AssertEdit("ㄱ", 1, "갉", composer, "갈");
    }

    private static object CreateComposer()
    {
        var type = typeof(DreamineVirtualKeyboard)
            .Assembly
            .GetType("Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard.HangulComposer", throwOnError: true)!;

        return Activator.CreateInstance(type, nonPublic: true)!;
    }

    private static void AssertEdit(string input, int replaceCount, string text, object composer, string textBeforeCaret)
    {
        var result = composer
            .GetType()
            .GetMethod("Input", [typeof(string), typeof(string)])!
            .Invoke(composer, [input, textBeforeCaret])!;

        Assert.Equal(replaceCount, (int)result.GetType().GetProperty("ReplaceCount")!.GetValue(result)!);
        Assert.Equal(text, (string)result.GetType().GetProperty("Text")!.GetValue(result)!);
    }
}
