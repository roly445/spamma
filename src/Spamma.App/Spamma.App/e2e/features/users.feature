@user
Feature: User administration
  As a user administrator
  I need to manage accounts and their access safely

  Scenario: Search and paginate users by name and status
    Given I administer users with active, inactive, and suspended accounts
    When I search for active users by name
    Then only matching active users appear on the first page
    And I can navigate to the remaining matching users

  Scenario: Create a user account
    Given I have user administration permission
    When I add a user with valid details without sending an invitation
    Then the new account appears as inactive

  Scenario: Edit a user without clearing their existing roles
    Given I administer a user with a domain management role
    When I edit that user's name and email
    Then the saved details appear and the domain management role remains

  Scenario: Suspend and restore a user account
    Given I administer an active user account
    When I suspend the user for an administrative reason
    Then the account is shown as suspended
    When I restore the user
    Then the account is shown as active again

  Scenario: Review passkeys for a user account
    Given I administer a user with active and revoked passkeys
    When I review that user's passkeys
    Then only that account's passkeys and their statuses appear

  Scenario: A non-administrator cannot open user management
    Given I am signed in without user administration permission
    When I navigate to the user management URL
    Then I cannot view the user list or change an account
