using BluQube.Commands;
using BluQube.Constants;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Infrastructure.Services;

public class ErrorMessageMapperService : IErrorMessageMapperService
{
    public string GetErrorMessage(CommandResult result)
    {
        if (result.Status == CommandResultStatus.Succeeded)
        {
            return string.Empty;
        }

        // Fallback to generic error message if no specific code is available
        return "An error occurred. Please try again.";
    }

    public string GetErrorMessageForCode(string errorCode)
    {
        // Map specific error codes to user-friendly messages
        return errorCode switch
        {
            "email_inbox.email_part_of_campaign" =>
                "This email is part of a campaign and cannot be modified.",
            "email_inbox.email_already_deleted" =>
                "This email has already been deleted.",
            "email_inbox.email_already_favorited" =>
                "This email is already marked as favorite.",
            "email_inbox.email_not_favorited" =>
                "This email is not marked as favorite.",
            "email_inbox.invalid_campaign_data" =>
                "Invalid campaign data.",
            "email_inbox.campaign_already_deleted" =>
                "This campaign has already been deleted.",
            "common.not_found" =>
                "The requested item was not found.",
            "common.saving_changes_failed" =>
                "Failed to save changes. Please try again.",

            // Default fallback
            _ => "An error occurred. Please try again.",
        };
    }
}
