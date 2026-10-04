@pending
Feature: Personal inbox and message inspection
  As a signed-in user
  I need to find and inspect email sent to my assigned addresses

  Scenario: An empty inbox explains that there are no messages
    Given I am signed in with access to a subdomain without messages
    When I open my inbox
    Then I see the empty inbox state

  Scenario: A received message appears in the assigned user's inbox
    Given a message has been received for my assigned subdomain
    When I open my inbox
    Then I can see that message in the list
    And I can select it to inspect its content and headers

  Scenario: Inbox search filters visible messages
    Given my inbox contains messages with different subjects or senders
    When I search for one of those messages
    Then matching messages are shown
    And unrelated messages are not shown

  Scenario: Inbox pagination preserves the current search
    Given my search has more than one page of results
    When I move to the next page
    Then I see the next matching messages
    And the search remains applied

  Scenario: A message can be marked and unmarked as a favorite
    Given I have opened a message in my inbox
    When I toggle its favorite status
    Then the message displays its updated favorite status

  Scenario: A message can be inspected in each available representation
    Given I have opened a message with HTML and plain text content
    When I switch between the message tabs
    Then I can inspect the HTML, text, and raw message representations

  Scenario Outline: A message can be downloaded in a supported format
    Given I have opened a message in my inbox
    When I save it as <format>
    Then I receive a file in <format> format

    Examples:
      | format |
      | EML    |
      | PDF    |
      | HTML   |
      | Text   |

  Scenario: A non-campaign message can be deleted
    Given I have opened a non-campaign message in my inbox
    When I delete it
    Then it is removed from my inbox

  Scenario: A user cannot inspect another user's message
    Given another user has a message outside my assignments
    When I use search or a direct message link
    Then I cannot view that message or its content
