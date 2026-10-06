@subdomain
Feature: Subdomain management
  As a domain or subdomain moderator
  I need to manage email routing and user access within my scope

  Scenario: Subdomains can be searched by name, status, parent domain, and page
    Given I administer several subdomains with different states and parents
    When I search for the fixture subdomain name and filter to active subdomains under its parent domain
    Then only the matching active subdomains appear
    And I can navigate to the next page of matching subdomains

  Scenario: A verified domain can have a new subdomain
    Given I administer a verified parent domain
    When I add a valid subdomain
    Then it appears beneath that domain
    And I can open the new subdomain details

  Scenario: Only a subdomain description can be edited after creation
    Given I moderate a subdomain
    When I update its description
    Then its details show the saved description and unchanged name

  Scenario: A subdomain can be suspended and restored
    Given I moderate an active subdomain
    When I suspend the subdomain with a reason
    Then the subdomain is shown as suspended
    When I restore the subdomain
    Then the subdomain is shown as active again

  Scenario: A subdomain moderator can be assigned and removed
    Given I moderate a subdomain and another user exists
    When I assign that user as a subdomain moderator
    Then that user appears in the subdomain moderators tab
    When I remove the subdomain assignment
    Then that user no longer appears in the subdomain moderators tab

  Scenario: A subdomain viewer can be assigned and removed
    Given I moderate a subdomain and another user exists
    When I assign that user as a subdomain viewer
    Then that user appears in the subdomain viewers tab
    When I remove the subdomain assignment
    Then that user no longer appears in the subdomain viewers tab

  Scenario: A matching MX record can be checked
    Given I can view a subdomain
    When I publish its MX record and request a check
    Then its MX status is valid

  Scenario: A missing MX record fails verification
    Given I can view a subdomain
    When I request a check without publishing its MX record
    Then its MX status is invalid

  Scenario: An unrelated user cannot manage a subdomain
    Given a subdomain is outside my assignments
    When I open the subdomain direct URL
    Then the unrelated subdomain details and management actions are not disclosed
