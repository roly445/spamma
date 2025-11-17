using System.Text;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MimeKit;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Components.UserControls;

/// <summary>
/// Code-behind for the EmailViewer component.
/// </summary>
public partial class EmailViewer(
    IQuerier querier, ICommander commander, IJSRuntime jsRuntime, INotificationService notificationService) : ComponentBase
{
    private MimeMessage? _mimeMessage;
    private List<EmailTab> _tabs = new();
    private EmailTab? _activeTab;
    private List<MimePart> _attachments = new();
    private string _rawSource = string.Empty;
    private bool _showSaveDropdown;
    private bool _isDeleting;
    private bool _isTogglingFavorite;

    private int? _currentViewportWidth = null;
    private string _currentViewportName = "Full Width";
    private int _customWidth = 600;

    private enum TabType
    {
        Text,
        Html,
        Raw,
    }

    private enum SaveFormat
    {
        Eml,
        Html,
        Text,
        Pdf,
    }

    [Parameter]
    public SearchEmailsQueryResult.EmailSummary? Email { get; set; }

    [Parameter]
    public EventCallback<SearchEmailsQueryResult.EmailSummary> OnEmailDeleted { get; set; }

    [Parameter]
    public EventCallback<SearchEmailsQueryResult.EmailSummary> OnEmailUpdated { get; set; }

    [Parameter]
    public bool HideCampaignInfo { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (this.Email != null)
        {
            var result = await querier.Send(new GetEmailMimeMessageByIdQuery(this.Email.EmailId));
            if (result.Status == QueryResultStatus.Succeeded)
            {
                var base64Content = result.Data.FileContent;
                var bytes = Convert.FromBase64String(base64Content);
                using var compressedStream = new MemoryStream(bytes);
                await using var decompressedStream = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Decompress);
                using var resultStream = new MemoryStream();
                await decompressedStream.CopyToAsync(resultStream);
                resultStream.Seek(0, SeekOrigin.Begin);

                resultStream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(resultStream, leaveOpen: true);
                this._rawSource = await reader.ReadToEndAsync();

                resultStream.Seek(0, SeekOrigin.Begin);
                var parser = new MimeParser(resultStream);
                this._mimeMessage = await parser.ParseMessageAsync();

                this.ProcessMimeMessage();
            }
        }
        else
        {
            this._mimeMessage = null;
            this._tabs.Clear();
            this._activeTab = null;
            this._attachments.Clear();
            this._rawSource = string.Empty;
        }

        await base.OnParametersSetAsync();
    }

    private static string GetInitials(InternetAddress address)
    {
        if (address is MailboxAddress mailbox && !string.IsNullOrEmpty(mailbox.Name))
        {
            var parts = mailbox.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }

            return parts[0][0].ToString().ToUpper();
        }

        var emailAddress = address.ToString();
        var atIndex = emailAddress.IndexOf('@');
        if (atIndex > 0)
        {
            return emailAddress[0].ToString().ToUpper();
        }

        return "?";
    }

    private static string GetSafeFileName(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return "email_message";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(subject.Where(c => !invalidChars.Contains(c)).ToArray());

        return safeName.Length > 50 ? safeName.Substring(0, 50).Trim() : safeName.Trim();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static long GetAttachmentSize(MimePart? attachment)
    {
        if (attachment == null)
        {
            return 0;
        }

        // Prefer header-provided size if present
        if (attachment.ContentDisposition?.Size > 0)
        {
            return attachment.ContentDisposition.Size.Value;
        }

        // Fallback: decode the content into a MemoryStream and measure
        if (attachment.Content is MimeContent mimeContent)
        {
            using var ms = new MemoryStream();
            mimeContent.DecodeTo(ms);
            return ms.Length;
        }

        return 0;
    }

    private void SetViewportSize(int? width, string name)
    {
        this._currentViewportWidth = width;
        this._currentViewportName = name;
        if (width.HasValue)
        {
            this._customWidth = width.Value;
        }

        this.StateHasChanged();
    }

    private Task ApplyCustomWidth()
    {
        if (this._customWidth is >= 240 and <= 2000)
        {
            this._currentViewportWidth = this._customWidth;
            this._currentViewportName = $"{this._customWidth}px";
            this.StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private string GetViewportButtonClasses(string viewportType)
    {
        var isActive = viewportType switch
        {
            "mobile" => this._currentViewportWidth == 320,
            "tablet" => this._currentViewportWidth == 768,
            "desktop" => this._currentViewportWidth == 1024,
            "full" => this._currentViewportWidth == null,
            _ => false,
        };

        var baseClasses = "inline-flex items-center px-3 py-1.5 text-xs font-medium rounded border transition-colors";

        if (isActive)
        {
            return $"{baseClasses} bg-blue-600 text-white border-blue-600";
        }
        else
        {
            return $"{baseClasses} bg-white text-gray-700 border-gray-300 hover:bg-gray-50 hover:border-gray-400";
        }
    }

    private string GetIframeContainerClasses()
    {
        if (this._currentViewportWidth.HasValue)
        {
            return "transition-all duration-300 ease-in-out";
        }

        return "w-full transition-all duration-300 ease-in-out";
    }

    private string GetIframeContainerStyle()
    {
        if (this._currentViewportWidth.HasValue)
        {
            return $"width: {this._currentViewportWidth}px; min-height: 100%;";
        }

        return "width: 100%; min-height: 100%;";
    }

    private async Task DeleteEmail()
    {
        if (this.Email == null || this._isDeleting)
        {
            return;
        }

        this._isDeleting = true;
        this.StateHasChanged();

        var result = await commander.Send(new DeleteEmailCommand(this.Email.EmailId));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("Email deleted successfully.");
            await this.OnEmailDeleted.InvokeAsync(this.Email);
        }
        else
        {
            notificationService.ShowError("Failed to delete email. Please try again.");
        }

        this._isDeleting = false;
        this.StateHasChanged();
    }

    private async Task ToggleFavorite()
    {
        if (this.Email == null || this._isTogglingFavorite)
        {
            return;
        }

        this._isTogglingFavorite = true;
        this.StateHasChanged();

        var result = await commander.Send(new ToggleEmailFavoriteCommand(this.Email.EmailId));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            // Update the local Email object to reflect the new favorite status
            this.Email = this.Email with { IsFavorite = !this.Email.IsFavorite };
            var message = this.Email.IsFavorite ? "Email marked as favorite." : "Email unmarked as favorite.";
            notificationService.ShowSuccess(message);
            await this.OnEmailUpdated.InvokeAsync(this.Email);
        }
        else
        {
            notificationService.ShowError("Failed to update email favorite status. Please try again.");
        }

        this._isTogglingFavorite = false;
        this.StateHasChanged();
    }

    private string GetFavoriteButtonClasses()
    {
        var baseClasses = "p-2 rounded-md transition-colors";
        if (this.Email?.IsFavorite == true)
        {
            return $"{baseClasses} text-yellow-600 hover:text-yellow-700 hover:bg-yellow-50";
        }

        return $"{baseClasses} text-gray-400 hover:text-yellow-600 hover:bg-yellow-50";
    }

    private string GetFavoriteTooltip()
    {
        return this.Email?.IsFavorite == true ? "Remove from favorites" : "Add to favorites";
    }

    private void ProcessMimeMessage()
    {
        this._tabs.Clear();
        this._attachments.Clear();
        this._activeTab = null;

        if (this._mimeMessage?.Body != null)
        {
            // Process the MIME structure
            this.ProcessMimeEntity(this._mimeMessage.Body);

            // Order tabs: HTML first (if available), then Text, then Raw
            this._tabs = this._tabs.OrderBy(tab => tab.Type switch
            {
                TabType.Html => 0,  // HTML gets priority 0 (first)
                TabType.Text => 1,  // Text gets priority 1 (second)
                TabType.Raw => 2,   // Raw gets priority 2 (third)
                _ => 3, // Any other types come last
            }).ToList();

            // Set the first available tab as active
            this._activeTab = this._tabs.FirstOrDefault();

            // Add raw source tab
            this._tabs.Add(new EmailTab { Name = "Raw", Type = TabType.Raw, Content = string.Empty });
        }
    }

    private void ProcessMimeEntity(MimeEntity entity)
    {
        if (entity is Multipart multipart)
        {
            foreach (var part in multipart)
            {
                this.ProcessMimeEntity(part);
            }
        }
        else if (entity is MimePart mimePart)
        {
            if (mimePart.IsAttachment)
            {
                this._attachments.Add(mimePart);
            }
            else if (mimePart is TextPart textPart)
            {
                var content = textPart.Text;
                var tabName = textPart.ContentType.MediaSubtype.ToUpper() switch
                {
                    "HTML" => "HTML",
                    "PLAIN" => "Text",
                    _ => textPart.ContentType.MediaSubtype.ToUpper(),
                };

                var tabType = textPart.ContentType.MediaSubtype.ToLower() switch
                {
                    "html" => TabType.Html,
                    _ => TabType.Text,
                };

                this._tabs.Add(new EmailTab
                {
                    Name = tabName,
                    Type = tabType,
                    Content = content,
                });
            }
        }
    }

    private string GetTabClasses(EmailTab tab)
    {
        var baseClasses = "whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm";

        if (tab == this._activeTab)
        {
            return $"{baseClasses} border-blue-500 text-blue-600";
        }

        return $"{baseClasses} border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300";
    }

    private void SetActiveTab(EmailTab tab)
    {
        this._activeTab = tab;
        this.StateHasChanged();
    }

    private async Task DownloadAttachment(MimePart attachment)
    {
        if (attachment.Content is MimeContent content)
        {
            using var stream = new MemoryStream();
            await content.DecodeToAsync(stream);
            var bytes = stream.ToArray();

            await jsRuntime.InvokeVoidAsync("downloadFile", attachment.FileName, Convert.ToBase64String(bytes));
        }
    }

    private async Task SaveMessage(SaveFormat format)
    {
        this._showSaveDropdown = false;

        if (this._mimeMessage == null)
        {
            return;
        }

        try
        {
            string filename;
            string content;
            string contentType;

            switch (format)
            {
                case SaveFormat.Eml:
                    filename = GetSafeFileName(this._mimeMessage.Subject) + ".eml";
                    content = this._rawSource;
                    contentType = "message/rfc822";
                    break;

                case SaveFormat.Html:
                    filename = GetSafeFileName(this._mimeMessage.Subject) + ".html";
                    content = this.GenerateHtmlContent();
                    contentType = "text/html";
                    break;

                case SaveFormat.Text:
                    filename = GetSafeFileName(this._mimeMessage.Subject) + ".txt";
                    content = this.GenerateTextContent();
                    contentType = "text/plain";
                    break;

                case SaveFormat.Pdf:
                    filename = GetSafeFileName(this._mimeMessage.Subject) + ".pdf";
                    content = this.GenerateHtmlContent();
                    await jsRuntime.InvokeVoidAsync("printToPdf", content, filename);
                    return;

                default:
                    return;
            }

            var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
            await jsRuntime.InvokeVoidAsync("downloadFile", filename, base64Content, contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving message: {ex.Message}");
        }
    }

    private string GenerateHtmlContent()
    {
        if (this._mimeMessage == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine($"<title>{this._mimeMessage.Subject}</title>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { border-bottom: 2px solid #ccc; padding-bottom: 10px; margin-bottom: 20px; }");
        sb.AppendLine(".field { margin-bottom: 8px; }");
        sb.AppendLine(".label { font-weight: bold; display: inline-block; width: 60px; }");
        sb.AppendLine(".content { margin-top: 20px; }");
        sb.AppendLine(".attachments { margin-top: 20px; padding: 10px; background-color: #f5f5f5; border-radius: 5px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>{this._mimeMessage.Subject}</h1>");
        sb.AppendLine($"<div class='field'><span class='label'>From:</span> {string.Join(", ", this._mimeMessage.From)}</div>");
        if (this._mimeMessage.To.Count > 0)
        {
            sb.AppendLine($"<div class='field'><span class='label'>To:</span> {string.Join(", ", this._mimeMessage.To)}</div>");
        }

        if (this._mimeMessage.Cc.Count > 0)
        {
            sb.AppendLine($"<div class='field'><span class='label'>Cc:</span> {string.Join(", ", this._mimeMessage.Cc)}</div>");
        }

        if (this._mimeMessage.Bcc.Count > 0)
        {
            sb.AppendLine($"<div class='field'><span class='label'>Bcc:</span> {string.Join(", ", this._mimeMessage.Bcc)}</div>");
        }

        sb.AppendLine($"<div class='field'><span class='label'>Date:</span> {this._mimeMessage.Date:MMM dd, yyyy at h:mm tt}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='content'>");
        var htmlTab = this._tabs.FirstOrDefault(t => t.Type == TabType.Html);
        if (htmlTab != null)
        {
            sb.AppendLine(htmlTab.Content);
        }
        else
        {
            var textTab = this._tabs.FirstOrDefault(t => t.Type == TabType.Text);
            if (textTab != null)
            {
                sb.AppendLine($"<pre>{System.Web.HttpUtility.HtmlEncode(textTab.Content)}</pre>");
            }
        }

        sb.AppendLine("</div>");

        if (this._attachments.Count > 0)
        {
            sb.AppendLine("<div class='attachments'>");
            sb.AppendLine($"<h3>Attachments ({this._attachments.Count})</h3>");
            sb.AppendLine("<ul>");
            foreach (var attachment in this._attachments)
            {
                sb.AppendLine($"<li>{attachment.FileName}</li>");
                sb.AppendLine($"<li>{attachment.FileName} ({FormatFileSize(GetAttachmentSize(attachment))})</li>");
            }

            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateTextContent()
    {
        if (this._mimeMessage == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine($"Subject: {this._mimeMessage.Subject}");
        sb.AppendLine($"From: {string.Join(", ", this._mimeMessage.From)}");
        if (this._mimeMessage.To.Count > 0)
        {
            sb.AppendLine($"To: {string.Join(", ", this._mimeMessage.To)}");
        }

        if (this._mimeMessage.Cc.Count > 0)
        {
            sb.AppendLine($"Cc: {string.Join(", ", this._mimeMessage.Cc)}");
        }

        if (this._mimeMessage.Bcc.Count > 0)
        {
            sb.AppendLine($"Bcc: {string.Join(", ", this._mimeMessage.Bcc)}");
        }

        sb.AppendLine($"Date: {this._mimeMessage.Date:MMM dd, yyyy at h:mm tt}");
        sb.AppendLine();
        sb.AppendLine(string.Empty.PadRight(50, '-'));
        sb.AppendLine();

        var textTab = this._tabs.FirstOrDefault(t => t.Type == TabType.Text);
        if (textTab != null)
        {
            sb.AppendLine(textTab.Content);
        }
        else
        {
            var htmlTab = this._tabs.FirstOrDefault(t => t.Type == TabType.Html);
            if (htmlTab != null)
            {
                sb.AppendLine("[HTML content - view in HTML format for full content]");
            }
        }

        if (this._attachments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(string.Empty.PadRight(50, '-'));
            sb.AppendLine($"Attachments ({this._attachments.Count}):");
            foreach (var attachment in this._attachments)
            {
                sb.AppendLine($"- {attachment.FileName}");
                sb.AppendLine($"- {attachment.FileName} ({FormatFileSize(GetAttachmentSize(attachment))})");
            }
        }

        return sb.ToString();
    }

    private sealed class EmailTab
    {
        public string Name { get; set; } = string.Empty;

        public TabType Type { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}