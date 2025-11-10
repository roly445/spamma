namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public interface IBaseEmailCaptureJob
{
    Stream MimeStream { get;  }

    Guid DomainId { get;  }

    Guid SubdomainId { get; }
}