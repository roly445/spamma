@settings
Feature: Application settings and administrative access
  As an administrator
  I need to manage operational settings without bypassing confirmation

  Scenario Outline: Catch-all setting displays its current state
    Given catch-all mode is <state> for a settings administrator
    When I view the settings page
    Then the catch-all setting shows <state>

    Examples:
      | state    |
      | enabled  |
      | disabled |

  @settings-finalize
  Scenario: Entering maintenance mode requires confirmation
    Given I can administer application settings
    When I choose to enter maintenance mode
    Then I see a warning before the mode changes
    And maintenance mode is enabled only after I confirm

  Scenario: A subdomain moderator sees only relevant administration links
    Given I moderate a subdomain without global administration
    When I open the settings menu
    Then I see my account and subdomain management links
    And global administration links are hidden

  Scenario Outline: Hiding a link does not grant direct URL access
    Given I am signed in without global administration
    When I enter the <page> URL directly
    Then I cannot view the <page> administration page

    Examples:
      | page              |
      | user management   |
      | application setup |
      | catch-all senders |

  Scenario: A restricted user cannot enter maintenance through the API
    Given I am signed in without global administration
    When I request maintenance mode directly
    Then the request is forbidden and the app remains available
