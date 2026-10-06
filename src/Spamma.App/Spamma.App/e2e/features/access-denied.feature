@settings @access-denied
Feature: Helpful access denied page
  As a signed-in user with limited permissions
  I need a clear explanation and a safe way back when a page is restricted

  Scenario: Direct navigation explains the permission boundary
    Given I am signed in without global administration
    When I open the access denied page directly
    Then I see a branded permission explanation and administrator guidance
    And I can return to my inbox

  Scenario: An external ReturnUrl cannot redirect recovery actions
    Given I am signed in without global administration
    When I open access denied with an external ReturnUrl
    Then recovery actions stay inside Spamma
    And Go back returns to the page I came from

  Scenario: The access denied page works on a mobile keyboard
    Given I am signed in without global administration
    When I open access denied at mobile width
    Then the page fits the viewport and both actions are keyboard operable
