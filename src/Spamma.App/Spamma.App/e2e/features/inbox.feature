@inbox
Feature: Personal inbox and message inspection
  As a signed-in user
  I need to find and inspect email in subdomains I can view

  Scenario: An empty inbox explains that there are no messages
    Given I am signed in with access to a subdomain without messages
    When I open my inbox
    Then I see the empty inbox state

  Scenario: A received message appears in the assigned user's inbox
    Given a non-campaign message has been received for a subdomain I can view
    When I open my inbox
    Then I can see that message in the list
    And I can select it to inspect its sender, recipients, subject, content, and headers

  Scenario: Inbox search filters visible messages
    Given my inbox contains messages with different subjects and email addresses
    When I search by part of a subject or email address
    Then matching messages are shown
    And unrelated messages are not shown

  Scenario: An inbox search with no matches explains the result
    Given my inbox contains messages
    When I search for a term that matches none of them
    Then I see the no-emails-found state
    And I can change my search

  Scenario: Inbox pagination preserves the current search
    Given my search has more than one page of results
    When I move to the next page
    Then I see the next matching messages
    And the search remains applied

  Scenario: A message can be marked and unmarked as a favorite
    Given I have opened a non-campaign message in my inbox
    When I mark it as a favorite
    Then the message shows a filled favorite star in the viewer and inbox list
    When I remove it from favorites
    Then the favorite star is cleared in the viewer and inbox list

  Scenario: A message can be inspected in each available representation
    Given I have opened a message with HTML and plain text content
    When I switch between the HTML, Text, and Raw tabs
    Then I can inspect the corresponding content of that message

  Scenario Outline: A message can be downloaded as a file
    Given I have opened a message in my inbox
    When I save it as <format>
    Then I receive a file with the <extension> extension containing that message

    Examples:
      | format | extension |
      | EML    | .eml      |
      | HTML   | .html     |
      | Text   | .txt      |

  Scenario: A message can be printed or saved as PDF
    Given I have opened a message in my inbox
    When I choose Save as PDF
    Then the browser opens the message for printing

  Scenario: An attachment can be downloaded
    Given I have opened a message with an attachment
    When I download the attachment
    Then I receive the attachment with its original filename and content

  Scenario: A non-campaign message can be deleted
    Given I have opened a non-campaign message in my inbox
    When I delete it
    Then it is removed from my inbox
    And I cannot open its content again

  Scenario: Campaign messages are hidden by default and read-only in the inbox
    Given a campaign message has been received for a subdomain I can view
    When I open my inbox
    Then I do not see that message in the list
    When I enable Show campaign emails
    Then I can open the campaign message
    And I cannot favorite or delete that message
    When I follow its campaign link
    Then I see its campaign details

  Scenario: A user cannot inspect another user's message
    Given another user has a message outside my assignments
    When I search for that message
    Then it does not appear in my inbox
    When I request its content by message ID
    Then the request is denied without disclosing the message

  Scenario: Two signed-in users see only their assigned domains and messages
    Given two users have separate domains, messages, and a captured campaign
    When the first user signs in using a magic link
    Then the first user can inspect their message and campaign
    And the second user's domain and message are hidden
    When the second user signs in using a magic link
    Then the second user can inspect their own message
    And the first user's domain and message are hidden
