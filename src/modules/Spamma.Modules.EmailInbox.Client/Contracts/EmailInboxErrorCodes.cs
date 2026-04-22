namespace Spamma.Modules.EmailInbox.Client.Contracts;

public static class EmailInboxErrorCodes
{
    public const string EmailAlreadyDeleted = "email_inbox.email_already_deleted";
    public const string EmailAlreadyFavorited = "email_inbox.email_already_favorited";
    public const string EmailNotFavorited = "email_inbox.email_not_favorited";
    public const string InvalidCampaignData = "email_inbox.invalid_campaign_data";
    public const string CampaignAlreadyDeleted = "email_inbox.campaign_already_deleted";
    public const string EmailIsPartOfCampaign = "email_inbox.email_part_of_campaign";
    public const string CatchAllSenderAddressAlreadyRemoved = "email_inbox.catch_all_sender_address_already_removed";
    public const string CatchAllSenderAddressRemoved = "email_inbox.catch_all_sender_address_removed";
    public const string UserAlreadyAssignedToCatchAllSender = "email_inbox.user_already_assigned_to_catch_all_sender";
    public const string UserNotAssignedToCatchAllSender = "email_inbox.user_not_assigned_to_catch_all_sender";
}