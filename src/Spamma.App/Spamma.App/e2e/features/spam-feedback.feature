@spam-feedback
Feature: Spam feedback
  As a subdomain operator
  I need to test complaint handling with individual ARF and webhook delivery outcomes

  Scenario: A moderator configures ARF and a signed webhook independently
    Given I moderate a verified feedback subdomain
    When I save its ARF and webhook destinations
    Then I see the one-time webhook signing secret
    And I can disable ARF without disabling the webhook

  Scenario: A viewer cannot change feedback destinations
    Given I can only view a feedback subdomain
    When I open its feedback settings
    Then feedback configuration is denied

  Scenario: A user reports a captured message once
    Given I am viewing a received message for feedback
    When I report the message as spam twice
    Then one manual report is shown for the message

  Scenario: A failed webhook delivery can be retried without losing the report
    Given an active feedback subdomain with an unreachable webhook
    When I report a captured message with that webhook
    Then the webhook failure is visible and I can retry it

  Scenario: A spam-report chaos address accepts campaign mail and sends ARF feedback
    Given an active spam-report chaos address with an ARF destination
    When I send campaign mail to the spam-report chaos address
    Then SMTP accepts it and the campaign is attributed to one spam report
    And the ARF feedback includes the original message

  Scenario: A failure chaos address still rejects mail without a spam report
    Given an active failure chaos address for feedback
    When I send mail to the failure chaos address
    Then SMTP rejects it without creating a spam report
