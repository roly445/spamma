@credential
Feature: Self-service API keys and passkeys
  As a signed-in user
  I need to manage credentials for my own account

  Scenario: A new API key is revealed once
    Given I am signed in to manage credentials
    When I create an API key with a name and expiry
    Then I can copy the key value immediately
    And the key value is not shown again after I leave the page

  Scenario: API keys can be filtered by status
    Given my account has active, expired, and revoked API keys
    When I select each API key status filter
    Then I see only my API keys with that status

  Scenario: An API key can be revoked
    Given my account has an active API key
    When I confirm that I want to revoke it
    Then it is shown as revoked
    And the key is no longer active

  Scenario: A passkey can be registered
    Given my browser supports passkeys
    And I am signed in to manage credentials
    When I name a new passkey and complete browser registration
    Then the passkey appears in my account

  Scenario: Cancelled passkey registration creates no credential
    Given I am signed in to manage credentials
    When I cancel passkey registration
    Then no new passkey appears in my account

  Scenario: Passkeys can be filtered by status
    Given my account has active and revoked passkeys
    When I select each passkey status filter
    Then I see only my passkeys with that status

  Scenario: A passkey can be revoked
    Given my browser supports passkeys
    And I am signed in to manage credentials
    And I have registered a passkey
    When I revoke that passkey
    Then it is shown as a revoked passkey
    And it cannot be used for a later login

  Scenario: A user cannot view another user's credentials
    Given another user has API keys and passkeys
    When I open my credential pages
    Then I see only credentials belonging to my account
