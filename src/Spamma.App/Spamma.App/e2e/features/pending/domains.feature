@pending
Feature: Domain management
  As a domain administrator
  I need to verify and manage domains and their assignments

  Scenario: Domains can be found by search and status
    Given I can administer several domains
    When I search and filter the domain list
    Then I see only matching domains
    And I can navigate between result pages

  Scenario: An administrator can add a domain
    Given I have domain administration permission
    When I add a valid domain
    Then it appears in the domain list as unverified
    And I can open its details

  Scenario: A user without domain administration cannot add a domain
    Given I can view an assigned domain but cannot administer domains
    When I open the domain list
    Then the add-domain action is unavailable

  Scenario: A domain can be verified through its DNS token
    Given I administer an unverified domain
    When I publish its verification TXT record and check verification
    Then the domain is marked verified
    And subdomain creation becomes available

  Scenario: Unverified domains cannot gain subdomains or moderators
    Given I administer an unverified domain
    When I open its details
    Then add-subdomain and add-moderator actions are unavailable

  Scenario: A domain can be edited
    Given I administer a domain
    When I change its editable details and save
    Then the updated details are shown

  Scenario: A domain can be suspended and restored
    Given I administer an active domain
    When I suspend it with a reason
    Then it is shown as suspended
    And management actions are disabled
    When I unsuspend it
    Then it is shown as active again

  Scenario: A domain moderator can be assigned and removed
    Given I administer a verified domain
    When I assign a user as a moderator
    Then the user appears in the domain's moderators tab
    When I remove the assignment
    Then the user no longer appears in that tab

  Scenario: An unrelated user cannot view a domain
    Given another user administers a domain outside my assignments
    When I open its direct URL
    Then its details and management actions are not disclosed
