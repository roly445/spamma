namespace Spamma.Modules.EmailInbox.Client.Contracts;

public static class EmailInboxErrorCodes
{
    public const string EmailAlreadyDeleted = "email_inbox.email_already_deleted";
    public const string EmailAlreadyFavorited = "email_inbox.email_already_favorited";
    public const string EmailNotFavorited = "email_inbox.email_not_favorited";
    public const string InvalidCampaignData = "email_inbox.invalid_campaign_data";
    public const string CampaignAlreadyDeleted = "email_inbox.campaign_already_deleted";
    public const string EmailIsPartOfCampaign = "email_inbox.email_part_of_campaign";
}