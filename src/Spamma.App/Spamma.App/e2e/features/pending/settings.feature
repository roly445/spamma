@pending
Feature: Application settings and administrative access
  As an administrator
  I need to manage operational settings without bypassing confirmation

  Scenario: Catch-all setting displays its current state
    Given I can open application settings
    When I view the settings page
    Then I can see whether catch-all mode is enabled

  Scenario: Entering maintenance mode requires confirmation
    Given I can administer application settings
    When I choose to enter maintenance mode
    Then I see a warning before the mode changes
    And maintenance mode is enabled only after I confirm

  Scenario: A signed-in user sees only relevant administration links
    Given I am signed in with limited assignments
    When I open the settings menu
    Then I see links for account management
    And administration links requiring permissions I lack are hidden

  Scenario: Hiding a link does not grant direct URL access
    Given I am signed in without user administration permission
    When I enter the user management URL directly
    Then I cannot view or change user accounts
