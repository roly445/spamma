@pending
Feature: SMTP reception and processing
  As a sender and recipient
  I need inbound email to be accepted and routed according to domain rules

  Scenario: Mail to an active assigned subdomain reaches its users
    Given an active verified domain has an active subdomain and assigned user
    When I send an email to that subdomain through SMTP
    Then the server accepts the message
    And the assigned user can inspect it in the inbox

  Scenario: Mail to an unknown domain is rejected
    Given the recipient domain is not configured
    When I send an email to it through SMTP
    Then the server rejects the message
    And no user sees it in an inbox

  Scenario: Concurrent messages are accepted independently
    Given an active subdomain can receive mail
    When several senders deliver messages at the same time
    Then each accepted message becomes available to its assigned users
    And no message is duplicated

  Scenario: Mail to a suspended domain or subdomain is rejected
    Given a recipient domain or subdomain is suspended
    When I send an email to it through SMTP
    Then the server rejects the message
    And no user sees it in an inbox

  Scenario: A campaign message is captured once
    Given a valid recipient subdomain is active
    When I send a message with a campaign identifier
    Then the message appears in that campaign
    And its sample content can be inspected

  Scenario: A chaos address applies its configured failure response
    Given a chaos address is active for a subdomain
    When I send a message to that address
    Then SMTP returns the configured failure response
    And the response is visible to the sender

  Scenario: Accepted mail survives a processing restart
    Given SMTP has durably accepted a message
    When message processing restarts before completing delivery
    Then the message is eventually available to the assigned user
    And it is not duplicated
