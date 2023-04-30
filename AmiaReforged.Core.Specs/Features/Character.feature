Feature: Character
As a user
My Character's identity should be persisted
So that I can interact with the dynamic world engine

    @mytag
    Scenario: A Character is added
        Given a player with the CDKey 'T3ST3D420'
        And a Character with the first name 'Test' and last name 'Testerson'
        When a request is made to persist the Character
        Then the Character should be persisted

    Scenario: A Character is updated
        Given a player with the CDKey 'T3ST3D420'
        And a Character with the first name 'Test' and last name 'Testerson'
        When a request is made to persist the Character
        And a request is made to update the Character with the first name 'Updated' and last name 'Test'
        Then the Character's name should be updated

    Scenario: A Character is deleted
        Given a player with the CDKey 'T3ST3D420'
        And a Character with the first name 'Test' and last name 'Testerson'
        When a request is made to persist the Character
        And a request is made to delete the Character
        Then the Character should be deleted

    Scenario: Should determine if a character exists
        Given a player with the CDKey 'T3ST3D420'
        And a Character with the first name 'Test' and last name 'Testerson'
        When a request is made to persist the Character
        Then a request to determine if the Character exists should be 'true'

    Scenario: All Characters are retrieved
        Given a list of Characters
        And a Character named 'Test' and last name 'Testerson' is added to the list
        And a Character named 'Flarb' and last name 'Gnarb' is added to the list
        When all of the Characters are added to the database
        Then the list of all Characters should be retrievable

    Scenario: All Player Characters are retrieved
        Given a list of Characters
        And a Character named 'Player' and last name 'Playerius' is added to the list
        And a Character named 'Playa' and last name 'Slayah' is added to the list
        And all Characters in the list are Player Characters
        When all of the Characters are added to the database
        Then the list of all player Characters should be retrievable

    Scenario: All Non-Player Characters are retrieved
        Given a list of Characters
        And a Character named 'NPC' and last name 'NPCius' is added to the list
        And a Character named 'NPCa' and last name 'Slayah' is added to the list
        And all Characters in the list are Non-Player Characters
        When all of the Characters are added to the database
        Then the list of all non-player Characters should be retrievable

    Scenario: Multiple Characters are deleted
        Given a list of Characters
        And a Character named 'Deletus' and last name 'Deletium' is added to the list
        And a Character named 'Delorti' and last name 'Ballorti' is added to the list
        When all of the Characters are added to the database
        And a request is made to delete all Characters in the list
        Then the list of Characters should be deleted