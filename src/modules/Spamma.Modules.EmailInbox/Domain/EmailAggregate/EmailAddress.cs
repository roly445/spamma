using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate;

public class EmailAddress
{
    internal EmailAddress(string address, string name, EmailAddressType emailAddressType)
    {
        this.Address = address;
        this.Name = name;
        this.EmailAddressType = emailAddressType;
    }

    public string Address { get; private set; }

    public string Name { get; private set; }

    public EmailAddressType EmailAddressType { get; private set; }
}