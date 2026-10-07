@smtp
Feature: SMTP reception and processing
  As a sender and recipient
  I need inbound email to be accepted and routed according to domain rules

  Scenario: Mail to an active assigned subdomain reaches its users
    Given I am signed in with access to an active verified subdomain
    When I deliver a standard message to that subdomain over SMTP
    Then SMTP accepts the message
    And I can inspect the delivered message in my inbox

  Scenario: Mail to an unknown domain is rejected
    Given I am signed in with access to an active verified subdomain
    When I deliver a message to an unconfigured domain over SMTP
    Then SMTP rejects the message with mailbox name not allowed
    And the rejected message is absent from my inbox

  Scenario: Concurrent messages are accepted independently
    Given I am signed in with access to an active verified subdomain
    When five senders deliver distinct messages concurrently over SMTP
    Then SMTP accepts every message
    And each message appears exactly once in my inbox

  Scenario Outline: Mail to a suspended domain or subdomain is rejected
    Given I am signed in with access to a suspended <resource>
    When I deliver a message to that subdomain over SMTP
    Then SMTP rejects the message with mailbox name not allowed
    And the rejected message is absent from my inbox

    Examples:
      | resource  |
      | domain    |
      | subdomain |

  Scenario: A campaign message is captured once
    Given I am signed in with access to an active verified subdomain
    When I deliver a message with a unique campaign identifier over SMTP
    Then SMTP accepts the message
    And the campaign shows one capture and its sample content

  Scenario: A chaos address applies its configured failure response
    Given I am signed in with access to an active chaos address
    When I deliver a message to the chaos address over SMTP
    Then SMTP returns the configured failure code
    And the sender can see the rejected response

  Scenario: Retrying the same temporarily rejected campaign message counts one affected message
    Given I am signed in with access to an active chaos address
    When I retry the same campaign message twice against a temporary chaos address
    Then the campaign shows one temporary failure and two SMTP attempts without an inbox message

  Scenario: A permanently rejected campaign message remains separate from captured mail
    Given I am signed in with access to an active chaos address
    When I send a campaign message to a permanent chaos address
    Then the campaign shows one permanent rejection without an inbox message

  Scenario: Accepted mail after a campaign bounce becomes the sample
    Given I am signed in with access to an active chaos address
    When a temporary campaign failure is followed by accepted mail
    Then the campaign shows the failure separately from its captured sample
