@domain
Feature: Domain management
  As a domain administrator or assigned moderator
  I need to verify and manage domains without exposing unrelated domains

  Scenario: Domains can be found by name, status, verification, and page
    Given I administer several domains with different states
    When I search for the fixture domain name and filter to pending unverified domains
    Then only the matching pending domains appear
    And I can navigate to the next page of matching domains

  Scenario: An administrator can add a domain
    Given I have domain administration permission
    When I add a valid domain with no contact email
    Then it appears as pending and unverified
    And I can open its details

  Scenario: A domain moderator cannot add a domain
    Given I moderate a domain without system domain administration permission
    When I open the domain list
    Then the add-domain action is unavailable

  Scenario: A domain can be verified through its DNS token
    Given I administer an unverified domain
    When I publish its verification TXT record and check verification
    Then the domain is verified and subdomain creation is enabled

  Scenario: A missing DNS token does not verify a domain
    Given I administer an unverified domain
    When I check verification without publishing its TXT record
    Then the domain remains unverified

  Scenario: Unverified domains cannot gain subdomains or moderators
    Given I administer an unverified domain
    When I open its details
    Then add-subdomain and add-moderator actions are disabled

  Scenario: A domain's contact and description can be edited
    Given I administer a verified domain
    When I change its contact and description
    Then the updated details are shown

  Scenario: A domain can be suspended and restored
    Given I administer a verified domain
    When I suspend it with a reason
    Then it is suspended and management actions are disabled
    When I restore it
    Then it is active and management actions are enabled

  Scenario: A domain moderator can be assigned and removed
    Given I administer a verified domain and another user exists
    When I assign that user as a domain moderator
    Then the user appears in the domain's moderators tab
    When I remove the assignment
    Then the user no longer appears in the moderators tab

  Scenario: An unrelated user cannot view a domain
    Given another user administers a domain outside my assignments
    When I open its direct URL
    Then its details and management actions are not disclosed
