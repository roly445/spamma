using System.Text;
using FluentAssertions;
using MimeKit;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class MessageBodyTextExtractorTests
{
    [Fact]
    public void Extract_PrefersPlainTextAndExcludesAttachments()
    {
        var builder = new BodyBuilder
        {
            TextBody = "Visible plain text\nnext line",
            HtmlBody = "<p>Different HTML text</p>",
        };
        builder.Attachments.Add("secret.txt", Encoding.UTF8.GetBytes("Attachment secret"));
        var message = new MimeMessage { Body = builder.ToMessageBody() };

        MessageBodyTextExtractor.Extract(message).Should().Be("Visible plain text next line");
    }

    [Fact]
    public void Extract_HtmlOnly_DecodesEntitiesAndSkipsScriptsAndStyles()
    {
        var message = new MimeMessage
        {
            Body = new TextPart("html")
            {
                Text = "<style>.secret { color: red }</style><p>Billing &amp; invoices</p><script>hidden_token()</script><div>Ready</div>",
            },
        };

        MessageBodyTextExtractor.Extract(message).Should().Be("Billing & invoices Ready");
    }

    [Fact]
    public void Extract_LargeBody_IsBounded()
    {
        var message = new MimeMessage { Body = new TextPart("plain") { Text = new string('x', 40_000) } };

        MessageBodyTextExtractor.Extract(message).Should().HaveLength(MessageBodyTextExtractor.MaxIndexedCharacters);
    }

    [Fact]
    public void Extract_ControlCharacters_DoNotReachPostgreSql()
    {
        var message = new MimeMessage { Body = new TextPart("plain") { Text = "safe\0\u0001 text" } };

        MessageBodyTextExtractor.Extract(message).Should().Be("safe text");
    }
}
