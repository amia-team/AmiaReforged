Feature: Faction
As a Faction
I need to be able to add and remove members from my roster
So that I can remain organized and keep track of who is in my faction

    @mytag
    Scenario: A faction is added
        Given a Faction named "The Order of the Phoenix"
        And with the description "A secret society dedicated to fighting the Dark Lord"
        When a request is made to persist the Faction
        Then the Faction should be persisted

    Scenario: A faction is updated
        Given a Faction named "The Order of the Phoenix"
        And with the description "A secret society dedicated to fighting the Dark Lord"
        When a request is made to update the Faction with the name "Order of Kittens" and the description "A meowing society dedicated to fighting the Dark Lord"
        Then the Faction should be updated

    Scenario: A faction is deleted
        Given a Faction named "The Order of Deleted Folks"
        And with the description "A secret society dedicated to fighting the Dark Lord"
        When a request is made to persist the Faction
        And a request is made to delete the Faction
        Then the Faction should be deleted

    Scenario: Characters are added to a faction roster
        Given a Faction named "The Knights of The Round Table"
        And with the description "An order of knights dedicated to protecting the realm"
        And a list of Characters
        And a Character named 'Arthur' and last name 'Pendragon' is added to the list
        And a Character named 'Lancelot' and last name 'Du Lac' is added to the list
        When a request is made to add the characters to the faction
        Then the characters should be added to the faction roster

    Scenario: A faction is created with a prepopulated roster
        Given a Faction named "Bloop"
        And with the description "Wut"
        And the faction already has a list of members
        When a request is made to persist the Faction
        Then the Faction should be persisted with the list of members

    Scenario: A faction's roster contains only characters that exist
        Given a Faction named "Fake"
        And with the description "Wut"
        And the faction already has a list of members
        And the roster contains a character that does not exist
        When a request is made to persist the Faction
        Then the Faction should be persisted with the list of members

    Scenario: All factions are requested
        Given multiple factions with random names
        When a request is made to persist the Factions
        And a request is made to retrieve all Factions
        Then the Factions should be retrieved

    Scenario: A character is removed from the roster
        Given a Faction named "The Knights of The Round Table"
        And with the description "An order of knights dedicated to protecting the realm"
        And a list of Characters
        And a Character named 'Arthur' and last name 'Pendragon' is added to the list
        And a Character named 'Lancelot' and last name 'Du Lac' is added to the list
        When a request is made to add the characters to the faction
        And a request is made to remove the character from the faction
        Then the character should be removed from the faction roster
        
    Scenario: All player characters are retrieved from the roster
        Given a Faction named "The Imperium of Man"
        And with the description "Loyal servants of the Emperor of Mankind"
        And a list of Characters
        And a Character named 'Arthur' and last name 'Pendragon' is added to the list
        And a Character named 'Lancelot' and last name 'Du Lac' is added to the list
        And the most recently added Character is a player character
        When a request is made to add the characters to the faction
        Then the player characters should be retrieved from the faction roster upon request

    Scenario: All NPCs are retrieved from the roster
        Given a Faction named "The Imperium of Man"
        And with the description "Loyal servants of the Emperor of Mankind"
        And a list of Characters
        And a Character named 'Arthur' and last name 'Pendragon' is added to the list
        And a Character named 'Sir' and last name 'Gawain' is added to the list
        And a Character named 'Lancelot' and last name 'Du Lac' is added to the list
        And the most recently added Character is a player character
        When a request is made to add the characters to the faction
        Then the NPCs should be retrieved from the faction roster upon request