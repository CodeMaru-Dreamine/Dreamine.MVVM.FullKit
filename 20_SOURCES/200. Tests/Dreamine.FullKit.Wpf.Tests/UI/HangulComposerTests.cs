using System.Reflection;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

/// <summary>
/// \if KO
/// <para>Hangul Composer Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hangul composer tests functionality and related state.</para>
/// \endif
/// </summary>
public class HangulComposerTests
{
    /// <summary>
    /// \if KO
    /// <para>Compose Trailing Consonant 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the compose trailing consonant operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ComposeTrailingConsonant()
    {
        var composer = CreateComposer();

        AssertEdit("ㄱ", 0, "ㄱ", composer, "");
        AssertEdit("ㅏ", 1, "가", composer, "ㄱ");
        AssertEdit("ㅁ", 1, "감", composer, "가");
    }

    /// <summary>
    /// \if KO
    /// <para>Compose Double Final Consonant 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the compose double final consonant operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ComposeDoubleFinalConsonant()
    {
        var composer = CreateComposer();

        AssertEdit("ㄱ", 0, "ㄱ", composer, "");
        AssertEdit("ㅏ", 1, "가", composer, "ㄱ");
        AssertEdit("ㄹ", 1, "갈", composer, "가");
        AssertEdit("ㄱ", 1, "갉", composer, "갈");
    }

    /// <summary>
    /// \if KO
    /// <para>Composer 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the composer value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Composer 작업에서 생성한 <c>object</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> result produced by the create composer operation.</para>
    /// \endif
    /// </returns>
    private static object CreateComposer()
    {
        var type = typeof(DreamineVirtualKeyboard)
            .Assembly
            .GetType("Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard.HangulComposer", throwOnError: true)!;

        return Activator.CreateInstance(type, nonPublic: true)!;
    }

    /// <summary>
    /// \if KO
    /// <para>Assert Edit 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the assert edit operation.</para>
    /// \endif
    /// </summary>
    /// <param name="input">
    /// \if KO
    /// <para>input에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for input.</para>
    /// \endif
    /// </param>
    /// <param name="replaceCount">
    /// \if KO
    /// <para>replace Count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for replace count.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="composer">
    /// \if KO
    /// <para>composer에 사용할 <c>object</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> value used for composer.</para>
    /// \endif
    /// </param>
    /// <param name="textBeforeCaret">
    /// \if KO
    /// <para>text Before Caret에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text before caret.</para>
    /// \endif
    /// </param>
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
