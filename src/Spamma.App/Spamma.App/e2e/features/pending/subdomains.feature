@pending
Feature: Subdomain management
  As a domain or subdomain moderator
  I need to manage email routing and user access within my scope

  Scenario: Subdomains can be searched and filtered
    Given I can access several subdomains
    When I search by name, status, or parent domain
    Then I see only matching subdomains
    And I can navigate between result pages

  Scenario: A verified domain can have a new subdomain
    Given I administer a verified domain
    When I add a valid subdomain
    Then it appears beneath that domain
    And I can open its details

  Scenario: A subdomain can be edited
    Given I moderate a subdomain
    When I update its editable name or description
    Then its details show the saved values

  Scenario: A subdomain can be suspended and restored
    Given I moderate an active subdomain
    When I suspend it with a reason
    Then it is shown as suspended
    When I unsuspend it
    Then it is shown as active again

  Scenario: A subdomain moderator can be assigned and removed
    Given I moderate a subdomain
    When I assign another user as a moderator
    Then that user appears in the moderators tab
    When I remove the assignment
    Then that user no longer appears in the moderators tab

  Scenario: A subdomain viewer can be assigned and removed
    Given I moderate a subdomain
    When I assign another user as a viewer
    Then that user appears in the viewers tab
    When I remove the assignment
    Then that user no longer appears in the viewers tab

  Scenario: MX records can be checked
    Given I can view a subdomain
    When I request an MX record check
    Then I see the result for that subdomain

  Scenario: An unrelated user cannot manage a subdomain
    Given a subdomain is outside my assignments
    When I open its direct URL or try a management action
    Then its details and management actions are not disclosed
