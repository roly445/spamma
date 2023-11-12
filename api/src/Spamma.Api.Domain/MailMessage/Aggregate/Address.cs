using Fluxera.Entity;
using Spamma.Api.Core;

namespace Spamma.Api.Domain.MailMessage.Aggregate
{
    public sealed class Address : Entity<Address, Guid>
    {
        public Address(Guid id, string emailAddress, string displayName, AddressType addressType)
        {
            this.IsTransient = true;
            this.ID = id;
            this.EmailAddress = emailAddress;
            this.DisplayName = displayName;
            this.AddressType = addressType;
        }

        private Address()
        {
            this.IsTransient = false;
        }

        public override bool IsTransient { get; }

        public string EmailAddress { get; private set; }

        public string DisplayName { get; private set; }

        public AddressType AddressType { get; private set; }
    }
}