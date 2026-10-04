@pending
Feature: Chaos address management
  As an authorized moderator
  I need to configure addresses that exercise SMTP failure handling

  Scenario: Chaos addresses can be searched and filtered
    Given I can moderate several chaos addresses
    When I search by address and filter by subdomain or status
    Then I see only matching addresses

  Scenario: A chaos address can be created
    Given I can moderate a subdomain
    When I create a chaos address for that subdomain
    Then it appears in the chaos address list

  Scenario: Disabling a chaos address requires confirmation
    Given I moderate an enabled chaos address
    When I choose to disable it
    Then I see the consequences before confirming
    And the address is disabled only after I confirm

  Scenario: A disabled chaos address can be enabled again
    Given I moderate a disabled chaos address
    When I confirm that it should be enabled
    Then it is shown as enabled

  Scenario: Deleting a chaos address requires confirmation
    Given I moderate a chaos address
    When I choose to delete it
    Then I see that its email history will be deleted
    And the address is removed only after I confirm

  Scenario: A user without chaos moderation permission cannot change addresses
    Given I am signed in without chaos moderation permission
    When I navigate to the chaos addresses URL
    Then I cannot create, enable, disable, or delete chaos addresses
