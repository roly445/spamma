@smoke
Feature: Anonymous access and login entry points
  As a visitor to a configured Spamma instance
  I need a clear way to sign in and a safe response when I cannot

  Scenario: Visitor can reach the login form from the home page
    Given I am visiting Spamma anonymously
    When I choose to sign in
    Then I see the login form

  Scenario: An unregistered address receives a generic magic-link confirmation
    Given I am on the login page
    When I request a magic link for an unregistered address
    Then I see a generic check-your-email confirmation

  Scenario: An invalid email address cannot request a magic link
    Given I am on the login page
    When I try to request a magic link with an invalid email address
    Then I stay on the login form without a confirmation

  Scenario: A login link without a token offers a route back to login
    Given I follow a login link without a token
    Then I am told the link is invalid or expired
    When I choose to return to login
    Then I see the login form

  Scenario: Setup login cannot be revisited after setup is complete
    Given initial setup has completed
    When I try to open the setup login page
    Then I am returned to the home page

  Scenario: An anonymous visitor cannot view the inbox
    Given I am visiting Spamma anonymously
    When I try to open the inbox
    Then I see the login form
