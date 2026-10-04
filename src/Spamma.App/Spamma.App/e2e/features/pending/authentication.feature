@pending
Feature: Account authentication
  As an account holder
  I need to sign in and end my session safely

  Scenario: A registered user requests a magic link
    Given I have an active account
    When I request a magic link for my email address
    Then I see the check-your-email confirmation
    And a login message is sent to that address

  Scenario: A suspended user cannot establish a new session
    Given my account has been suspended
    When I try to sign in
    Then I do not gain access to the inbox

  Scenario: A valid magic link signs the user in
    Given I have received an unused magic link
    When I confirm the login using that link
    Then I am signed in to my account
    And I can open my inbox

  Scenario: An expired or previously used magic link cannot sign the user in
    Given I have an expired or previously used magic link
    When I try to confirm the login
    Then I am not signed in
    And I can return to the login page

  Scenario: A registered passkey signs the user in
    Given my account has an active passkey
    When I choose passkey login and complete the browser challenge
    Then I am signed in to my account

  Scenario: A failed passkey challenge leaves the user signed out
    Given my account has an active passkey
    When I cancel or fail the browser challenge
    Then I remain signed out
    And I can use another login method

  Scenario: Logout ends the current session
    Given I am signed in
    When I log out
    Then I see the logout confirmation
    And I cannot reopen the inbox without signing in again
