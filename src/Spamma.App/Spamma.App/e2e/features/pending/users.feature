@pending
Feature: User administration
  As a user administrator
  I need to manage accounts and their access safely

  Scenario: The user list can be searched and filtered
    Given I have user administration permission
    When I search users by name or email and filter by status
    Then I see only matching users
    And I can navigate between result pages

  Scenario: A user account can be created
    Given I have user administration permission
    When I add a user with valid account details
    Then the user appears in the user list

  Scenario: A user account can be edited
    Given I have user administration permission
    When I edit an existing user's details
    Then the saved details appear in the user list

  Scenario: A user account can be suspended and restored
    Given I have user administration permission
    When I suspend an active user
    Then the user is shown as suspended
    When I unsuspend that user
    Then the user is shown as active

  Scenario: An administrator can review a user's passkeys
    Given I have user administration permission
    When I open passkeys for a user
    Then I can review the passkeys associated with that account

  Scenario: A non-administrator cannot open user management
    Given I am signed in without user administration permission
    When I navigate to the user management URL
    Then I cannot view the user list or change an account
