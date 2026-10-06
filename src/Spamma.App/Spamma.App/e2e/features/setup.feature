@setup
Feature: Initial application setup
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

  Scenario: Security keys can be generated and saved
    Given I am on the security keys setup step
    When I generate and save the keys
    Then the keys step is complete
    And I can proceed to hosting configuration

  Scenario: Regenerating saved security keys requires confirmation
    Given security keys have already been saved
    When I ask to regenerate them
    Then I must confirm regeneration before the saved key changes

  Scenario: Hosting configuration can be saved
    Given I am on the hosting setup step
    When I provide valid server and email routing settings
    Then the hosting configuration is saved
    And I can proceed to email configuration

  Scenario: Outbound email configuration can be saved
    Given I am on the email setup step
    When I provide valid SMTP settings
    Then the email configuration is saved
    And I can proceed to certificate configuration

  Scenario: An email preset populates editable SMTP settings
    Given I am on the email setup step
    When I choose an available email provider preset
    Then its SMTP settings populate the form
    And I can edit them before saving

  Scenario: Certificate setup can be skipped
    Given I am on the certificate setup step
    When I choose to skip certificate generation
    Then I can proceed to administrator creation

  Scenario: A certificate request reports an ACME failure
    Given I am on the certificate setup step
    And the test ACME service will reject the request
    When I select Let's Encrypt and request a certificate
    Then the certificate failure is reported to me

  Scenario: A certificate request reports ACME success
    Given I am on the certificate setup step
    And the test ACME service will accept the request
    When I select Let's Encrypt and request a certificate
    Then I proceed to administrator creation

  Scenario: The initial administrator can be created
    Given the required setup settings have been saved
    When I create the initial administrator
    Then the administrator account is recorded
    And a welcome email is sent to the administrator
    And I can review setup completion

  Scenario: An existing administrator can be skipped during setup
    Given an administrator has already been created
    When I open the administrator setup step
    Then I can skip to setup completion

  @setup-finalize
  Scenario: Completed setup can be finalized
    Given every required setup setting is present
    When I finalize setup
    Then the application leaves setup mode
    And setup pages are no longer available to ordinary visitors
