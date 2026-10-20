using System.Text;
using MimeKit;
using MimeKit.Text;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

internal static class MessageBodyTextExtractor
{
    internal const int MaxIndexedCharacters = 32_768;
    private const int MaxHtmlSourceCharacters = 1_048_576;

    public static string Extract(MimeMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            return Normalize(message.TextBody);
        }

        var html = message.HtmlBody;
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        using var reader = new StringReader(html[..Math.Min(html.Length, MaxHtmlSourceCharacters)]);
        var tokenizer = new HtmlTokenizer(reader) { DecodeCharacterReferences = true };
        var text = new StringBuilder(Math.Min(html.Length, MaxIndexedCharacters));
        var ignoredElement = string.Empty;

        while (text.Length < MaxIndexedCharacters && tokenizer.ReadNextToken(out var token))
        {
            if (token is HtmlTagToken tag)
            {
                if (ignoredElement.Length > 0)
                {
                    if (tag.IsEndTag && tag.Name.Equals(ignoredElement, StringComparison.OrdinalIgnoreCase))
                    {
                        ignoredElement = string.Empty;
                    }

                    continue;
                }

                if (!tag.IsEndTag && (tag.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                                      tag.Name.Equals("style", StringComparison.OrdinalIgnoreCase)))
                {
                    ignoredElement = tag.Name;
                    continue;
                }

                AppendSpace(text);
            }
            else if (ignoredElement.Length == 0 && token is HtmlDataToken data)
            {
                AppendNormalized(text, data.Data);
            }
        }

        return text.ToString().Trim();
    }

    private static string Normalize(string text)
    {
        var normalized = new StringBuilder(Math.Min(text.Length, MaxIndexedCharacters));
        AppendNormalized(normalized, text);
        return normalized.ToString().Trim();
    }

    private static void AppendNormalized(StringBuilder result, string text)
    {
        foreach (var character in text)
        {
            if (result.Length >= MaxIndexedCharacters)
            {
                break;
            }

            if (char.IsWhiteSpace(character))
            {
                AppendSpace(result);
            }
            else if (!char.IsControl(character))
            {
                result.Append(character);
            }
        }
    }

    private static void AppendSpace(StringBuilder result)
    {
        if (result.Length > 0 && result.Length < MaxIndexedCharacters && result[^1] != ' ')
        {
            result.Append(' ');
        }
    }
}
