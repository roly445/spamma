@pending
Feature: Durable SMTP processing across restart
  As a recipient
  I need accepted mail to survive a processing restart

  # Tracked by #21. This requires stopping the subscriber after durable SMTP
  # acknowledgement, then starting a second host against the same stores.
  Scenario: Accepted mail survives a processing restart
    Given SMTP has durably accepted a message
    When message processing restarts before completing delivery
    Then the message is eventually available to the assigned user
    And it is not duplicated
