using Fluxera.Entity;

namespace Spamma.Api.Domain.MailMessage.Aggregate
{
    public sealed class MailMessage : AggregateRoot<MailMessage, Guid>
    {
        private readonly List<Address> _addresses;

        public MailMessage(Guid id, string subject, DateTimeOffset whenReceived, IReadOnlyList<Address> addresses)
        {
            this.ID = id;
            this.IsTransient = true;
            this.Subject = subject;
            this.WhenReceived = whenReceived;
            this._addresses = addresses.ToList();
        }

        private MailMessage()
        {
            this.IsTransient = false;
            this._addresses = new List<Address>();
        }

        public override bool IsTransient { get; }

        public string Subject { get; private set; }

        public DateTimeOffset WhenReceived { get; private set; }

        public IReadOnlyList<Address> Addresses => this._addresses.AsReadOnly();
    }
}