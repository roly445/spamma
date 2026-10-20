@domain
Feature: SMTP submission policy
  Domain and subdomain administrators can control authenticated submission without exposing secrets later.

  Scenario: A domain credential is shown once and inherited by its subdomain until overridden
    Given I administer a verified domain with a subdomain for SMTP submission
    When I require an SMTP credential for the domain
    Then I receive the new SMTP credential once
    When I reopen the SMTP submission settings
    Then the effective domain policy requires authentication without showing the password
    When I open SMTP submission settings for the subdomain
    Then the subdomain inherits the domain policy
    When I disable SMTP authentication on the subdomain
    Then the subdomain permits anonymous TLS submission
    When I require a separate SMTP credential on the subdomain
    Then the subdomain shows its own credential and effective override
