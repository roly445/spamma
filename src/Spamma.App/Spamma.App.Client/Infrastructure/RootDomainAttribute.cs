using System.ComponentModel.DataAnnotations;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Infrastructure;

/// <summary>
/// Custom validation attribute for root domain validation.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
internal class RootDomainAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var domain = value as string;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return ValidationResult.Success;
        }

        var domainValidationService = validationContext.GetRequiredService<IDomainValidationService>();
        return domainValidationService.IsDomainValid(domain) ?
            ValidationResult.Success :
            new ValidationResult("Please enter a valid root domain (no subdomains).");
    }
}