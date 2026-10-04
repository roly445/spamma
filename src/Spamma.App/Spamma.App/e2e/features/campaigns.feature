@campaign
Feature: Campaign browsing and management
  As a user with access to captured campaigns
  I need to inspect campaign messages without exposing other users' data

  Scenario: The campaign list shows an empty state
    Given no campaigns are available to me
    When I open Campaigns
    Then I see that no campaigns were found

  Scenario: A captured campaign can be inspected
    Given a campaign has been captured for my assigned subdomain
    When I open that campaign
    Then I can see its campaign details and sample message

  Scenario: Campaigns can be filtered, sorted, and paged
    Given several campaigns are available across my assigned subdomains
    When I filter by the second assigned subdomain
    Then the list contains only that subdomain's campaign
    When I select the first subdomain and sort by campaign name
    Then campaigns appear in descending name order
    And I can move to the next page of sorted results

  Scenario: A campaign can be deleted
    Given I am allowed to manage a captured campaign
    When I delete that campaign
    Then it no longer appears in the campaign list

  Scenario: A user cannot inspect a campaign outside their assignments
    Given a campaign belongs to another user's subdomain
    When I use the campaign list or a direct campaign link
    Then I cannot view its details or sample message

  Scenario: A campaign viewer cannot delete a campaign
    Given I can view but not manage a captured campaign
    When I open Campaigns
    Then I can inspect the campaign without a Delete action
