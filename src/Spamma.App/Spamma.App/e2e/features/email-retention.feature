@domain
Feature: Email retention policy
  Domain defaults flow to subdomain inboxes until a moderator overrides them.

  Scenario: Domain age and count limits cascade and can be overridden by a subdomain
    Given I administer a verified domain with a subdomain for retention
    When I open the domain retention settings
    Then I see the default 24-hour age limit and no count limit
    When I set a 48-hour and 10-message domain retention policy
    Then the new domain retention policy is active
    When I open retention settings for the subdomain
    Then it inherits the named domain retention policy
    When I override the subdomain to keep 3 messages without an age limit
    Then the subdomain retention override is active
    When I restore inheritance for the subdomain
    Then the subdomain uses the domain retention policy again

  Scenario: An unrelated user cannot inspect a domain retention policy
    Given I am signed in without management rights to a retention domain
    When I open its retention URL directly
    Then the retention policy and domain name are not disclosed
