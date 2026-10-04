@pending
Feature: Account authentication
  As an account holder
  I need to sign in and end my session safely

  Scenario: A registered user requests a magic link
    Given I have an active account
    When I request a magic link for my email address
    Then I see the same conditional check-your-email confirmation shown for an unregistered address
    And one login message containing a confirmation link is sent to my address

  Scenario: A suspended user cannot request a new magic link
    Given my account has been suspended
    When I request a magic link for my email address
    Then I see the conditional check-your-email confirmation
    And no login message is sent to my address

  Scenario: Suspension invalidates a magic link issued earlier
    Given I received an unused magic link while my account was active
    And my account has since been suspended
    When I try to confirm the login using that link
    Then I am told the link is invalid or expired
    And I cannot open my inbox

  Scenario: A valid magic link signs the user in
    Given I have received an unused magic link
    When I open the link and select Continue to Spamma
    Then I am signed in to my account
    And I can open my inbox

  Scenario: An expired magic link cannot sign the user in
    Given I have a magic link whose 15-minute lifetime has passed
    When I open the link and try to confirm the login
    Then I am told the link is invalid or expired
    And I cannot open my inbox
    And I can return to the login page

  Scenario: A magic link cannot be used twice
    Given I have already signed in using a magic link
    When I open the same link and try to confirm the login again in a new browser session
    Then I am told the link is invalid or expired
    And I cannot open my inbox in that session

  Scenario: A registered passkey signs the user in
    Given my account has an active passkey
    When I choose passkey login and complete the browser challenge
    Then I am signed in to my account
    And I can open my inbox

  Scenario: A failed passkey challenge leaves the user signed out
    Given my account has an active passkey
    When I cancel or fail the browser challenge
    Then I remain signed out
    And I can use another login method

  Scenario: A suspended account cannot sign in with a registered passkey
    Given my account has been suspended
    And it has an active passkey
    When I choose passkey login and complete the browser challenge
    Then I remain signed out
    And I cannot open my inbox

  Scenario: Logout ends the current session
    Given I am signed in
    When I log out
    Then I see the logout confirmation
    And I cannot reopen the inbox without signing in again
