using Markdig;
using Microsoft.AspNetCore.Components;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>Markdown Helper 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates markdown helper functionality and related state.</para>
/// \endif
/// </summary>
public static class MarkdownHelper
{
    /// <summary>
    /// \if KO
    /// <para>pipeline 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the pipeline value.</para>
    /// \endif
    /// </summary>
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// \if KO
    /// <para>Markdown 또는 일반 텍스트를 HTML MarkupString으로 변환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to html operation.</para>
    /// \endif
    /// </summary>
    /// <param name="markdown">
    /// \if KO
    /// <para>markdown에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for markdown.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Html 작업에서 생성한 <c>MarkupString</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MarkupString</c> result produced by the to html operation.</para>
    /// \endif
    /// </returns>
    public static MarkupString ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return new MarkupString(string.Empty);
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        return new MarkupString(html);
    }

    /// <summary>
    /// \if KO
    /// <para>문자열이 HTML 태그를 포함하면 그대로, 아니면 Markdown으로 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to html auto operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Html Auto 작업에서 생성한 <c>MarkupString</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MarkupString</c> result produced by the to html auto operation.</para>
    /// \endif
    /// </returns>
    public static MarkupString ToHtmlAuto(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new MarkupString(string.Empty);
        // HTML 태그가 있으면 그대로 렌더링 (기존 HTML 데이터 호환)
        if (content.TrimStart().StartsWith('<'))
            return new MarkupString(content);
        return ToHtml(content);
    }
}
