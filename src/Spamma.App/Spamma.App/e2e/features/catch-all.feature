@catch-all
Feature: Catch-all email routing
  As an administrator or assigned user
  I need catch-all routing to be explicit and access controlled

  Scenario: Catch-all inbox explains when the feature is disabled
    Given catch-all mode is disabled for a signed-in user
    When I open the catch-all inbox
    Then I see that catch-all mode is disabled and where to enable it

  Scenario: An administrator can enable and disable catch-all mode
    Given I administer catch-all settings
    When I enable catch-all mode
    Then catch-all routing is shown as enabled
    When I disable catch-all mode
    Then catch-all routing is shown as disabled

  Scenario: A sender address can be allowlisted
    Given I administer catch-all sender addresses
    When I add a valid sender address
    Then it appears in the sender address list

  Scenario: An invalid sender address cannot be allowlisted
    Given I administer catch-all sender addresses
    When I try to add an invalid sender address
    Then the address is rejected and the list is unchanged

  Scenario: A sender address can be assigned to and unassigned from a user
    Given I administer an existing catch-all sender address and another user
    When I assign that user to the address
    Then the user appears in its assigned users list
    When I remove that assignment
    Then the user no longer appears in its assigned users list

  Scenario: A sender address can be removed
    Given I administer an existing catch-all sender address
    When I remove that sender address
    Then it no longer appears in the sender address list

  Scenario: An assigned user can inspect catch-all messages
    Given I am assigned to an allowlisted sender with a captured message
    When I open the catch-all inbox and select the message
    Then I can inspect its sender, recipient, subject, and content

  Scenario: Catch-all inbox search filters visible messages
    Given I can view catch-all messages with different subjects
    When I search for one catch-all message
    Then only the matching catch-all message is shown

  Scenario: A user cannot inspect another user's catch-all messages
    Given another user's catch-all sender has a captured message
    When I search the catch-all inbox for that message
    Then it is not shown in my catch-all inbox
    When I request the other catch-all message by ID
    Then the other catch-all message is denied

  Scenario: A non-administrator cannot change catch-all routing
    Given I am signed in without catch-all administration permission
    When I open the settings and sender management URLs
    Then I cannot change catch-all mode or sender addresses
