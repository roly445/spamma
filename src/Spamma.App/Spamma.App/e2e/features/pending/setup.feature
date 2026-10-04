@pending
Feature: Initial application setup
  As an operator
  I need to configure Spamma before inviting users

  Scenario: First visit starts the setup journey
    Given Spamma has not been configured
    When I open the application
    Then I am shown the setup welcome page
    And I can proceed to security keys

  Scenario: Setup pages require setup authentication
    Given Spamma is still in setup mode
    And I do not have a setup session
    When I open a setup step directly
    Then I am asked for the setup password
    And an incorrect password does not grant access

  Scenario: Security keys can be generated and saved
    Given I am on the security keys setup step
    When I generate and save the keys
    Then the keys step is complete
    And I can proceed to hosting configuration

  Scenario: Regenerating saved security keys requires confirmation
    Given security keys have already been saved
    When I ask to regenerate them
    Then I must confirm the regeneration before the saved keys change

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

  Scenario: A Let's Encrypt certificate can be requested
    Given I am on the certificate setup step
    When I select Let's Encrypt and provide the required hostname and email address
    Then I can request a certificate
    And the result is reported to me

  Scenario: The initial administrator can be created
    Given the required setup steps have been saved
    When I provide a valid administrator name and email address
    Then the administrator account is created
    And I can review setup completion

  Scenario: Another administrator can be entered during setup
    Given I am on the administrator setup step
    When I choose to add another administrator
    Then I can enter another administrator's account details

  Scenario: Incomplete setup shows the missing steps
    Given a required setup step is incomplete
    When I open the setup completion page
    Then I see which step is missing
    And I can return to that step

  Scenario: Completed setup can be finalized
    Given every required setup step is complete
    When I finalize setup
    Then the application leaves setup mode
    And setup pages are no longer available to ordinary visitors
