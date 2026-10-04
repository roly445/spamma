@setup
Feature: Initial setup access and completion guard
  As an operator
  I need to enter setup securely and see what remains to configure

  Scenario: First visit starts the authenticated setup journey
    Given Spamma is in setup mode
    When I open the application for setup
    Then I am asked for the setup password
    When I enter the valid setup password
    Then I am shown the setup welcome page
    And I can proceed to security keys

  Scenario: Setup pages require setup authentication
    Given Spamma is in setup mode
    When I open a setup step without a session
    Then I am asked for the setup password
    When I enter an incorrect setup password
    Then I remain outside the setup wizard

  Scenario: Incomplete setup identifies missing hosting settings
    Given I have entered the setup wizard
    And security keys, outbound email and an administrator are configured
    When I open the setup completion page
    Then I see that hosting configuration is missing
    And I cannot finalize setup
