using Markdig;
using Microsoft.AspNetCore.Components;

namespace ShopPlatform.Services;

public static class MarkdownHelper
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Markdown 또는 일반 텍스트를 HTML MarkupString으로 변환합니다.</summary>
    public static MarkupString ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return new MarkupString(string.Empty);
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        return new MarkupString(html);
    }

    /// <summary>문자열이 HTML 태그를 포함하면 그대로, 아니면 Markdown으로 처리합니다.</summary>
    public static MarkupString ToHtmlAuto(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new MarkupString(string.Empty);
        // HTML 태그가 있으면 그대로 렌더링 (기존 HTML 데이터 호환)
        if (content.TrimStart().StartsWith('<'))
            return new MarkupString(content);
        return ToHtml(content);
    }
}
