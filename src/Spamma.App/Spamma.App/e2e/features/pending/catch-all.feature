@pending
Feature: Catch-all email routing
  As an administrator and an assigned user
  I need catch-all routing to be explicit and access controlled

  Scenario: Catch-all inbox explains when the feature is disabled
    Given catch-all mode is disabled
    When I open the catch-all inbox
    Then I see that catch-all mode is disabled

  Scenario: Catch-all mode can be enabled and disabled
    Given I can administer application settings
    When I enable catch-all mode
    Then catch-all routing is shown as enabled
    When I disable catch-all mode
    Then catch-all routing is shown as disabled

  Scenario: A sender address can be allowlisted
    Given I can manage catch-all sender addresses
    When I add a valid sender address
    Then it appears in the sender address list

  Scenario: A sender address can be assigned to a user
    Given a catch-all sender address exists
    When I assign a user to that address
    Then the user appears in its assigned users list
    When I remove that assignment
    Then the user no longer appears in its assigned users list

  Scenario: A sender address can be removed
    Given a catch-all sender address exists
    When I remove that address
    Then it no longer appears in the sender address list

  Scenario: An assigned user can inspect catch-all messages
    Given catch-all mode is enabled
    And a message from an allowlisted sender has been captured for me
    When I open the catch-all inbox
    Then I can find and inspect that message

  Scenario: Catch-all inbox search filters visible messages
    Given I can view several catch-all messages
    When I search for one of those messages
    Then matching messages are shown
    And unrelated messages are not shown

  Scenario: A user cannot inspect another user's catch-all messages
    Given another user has catch-all messages outside my assignments
    When I search the catch-all inbox or use a direct message link
    Then I cannot view those messages
