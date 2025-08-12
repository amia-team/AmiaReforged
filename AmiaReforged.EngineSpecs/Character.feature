Feature: Characters, or Actors, are agents within the broader system. They have inventories, goals, personalities, flaws, and more.

    Scenario: A basic Player Character is created
        Given a Player with the Public CD Key "Q6ULTKYG"
        When the Player finishes Character creation
        Then their character should have a valid GUID

    Scenario: A Dungeon Master creates a Character
        Given a Dungeon Master with the Key "Q6ULTKYG"
        When the Dungeon Master creates a Character named "Watcher of the Gate"
        Then their character should have a valid GUID
        And the Character should be owned by the Dungeon Master with the Key "Q6ULTKYG"

    Scenario: A System-owned Character (NPC) is created
        Given the System with the tag "<SYSTEM_TAG>"
        When a System Character named "Town Crier" is created with the tag "<SYSTEM_TAG>"
        Then their character should have a valid GUID
        And the Character should be owned by the System with the tag "<SYSTEM_TAG>"

    Scenario: Assign an existing Character to a Player
        Given there is already a Character with the GUID "ce315843-7e79-46c6-938e-8b8b7e438920"
        And the Character is owned by the System with the tag "<SYSTEM_TAG>"
        And a Player with the Public CD Key "<PLAYER_KEY>"
        When the Character is assigned to the Player with the Public CD Key "<PLAYER_KEY>"
        Then the Character should be owned by the Player with the Public CD Key "<PLAYER_KEY>"

    Scenario: Transfer a Character from a Player to a Dungeon Master
        Given a Player with the Public CD Key "Q6ULTKYG"
        And there is already a Character with the GUID "ce315843-7e79-46c6-938e-8b8b7e438920"
        And the Character is owned by the Player with the Public CD Key "Q6ULTKYG"
        And a Dungeon Master with the Key "<DM_KEY>"
        When the Character is assigned to the Dungeon Master with the Key "<DM_KEY>"
        Then the Character should be owned by the Dungeon Master with the Key "<DM_KEY>"
        And the Character should not be owned by any Player
        And the Character should not be owned by the System

    Scenario: Rename a Character
        Given there is already a Character with the GUID "<CHAR_GUID>"
        And the Character's name is "Old Name"
        When the Character is renamed to "New Name"
        Then the Character's name should be "New Name"

    Scenario: Deactivate a Character
        Given there is already a Character with the GUID "<CHAR_GUID>"
        And the Character is active
        When the Character is deactivated
        Then the Character should be marked inactive

    Scenario: A Player lists their Characters
        Given a Player with the Public CD Key "<PLAYER_KEY>"
        And the Player has Characters named:
          | Name        |
          | Aria Swift  |
          | Borin Stone |
        When the Player requests their Character list
        Then the Player should receive 2 Characters
        And the list should contain a Character named "Aria Swift"
        And the list should contain a Character named "Borin Stone"

    Scenario: Retrieve an existing Character by GUID
        Given a Player with the Public CD Key "Q6ULTKYG"
        And there is already a Character with the GUID "ce315843-7e79-46c6-938e-8b8b7e438920"
        And the Character is owned by the Player with the Public CD Key "Q6ULTKYG"
        When the Player requests the Character with the GUID "ce315843-7e79-46c6-938e-8b8b7e438920"
        Then their Character should be retrieved for use in the system

    Scenario: Attempt to retrieve a non-existent Character
        Given a Player with the Public CD Key "<PLAYER_KEY>"
        And no Character exists with the GUID "<MISSING_GUID>"
        When the Player requests the Character with the GUID "<MISSING_GUID>"
        Then the request should fail because the Character was not found

    Scenario: Creating multiple Characters yields unique identifiers
        Given a Player with the Public CD Key "<PLAYER_KEY>"
        When the Player creates a Character named "First"
        And the Player creates a Character named "Second"
        Then both Characters should have valid GUIDs
        And the GUIDs should be unique

    Scenario: System-to-Player handoff preserves GUID
        Given the System with the tag "<SYSTEM_TAG>"
        And a System Character named "Quest Giver" is created with the tag "<SYSTEM_TAG>"
        And the Character is recorded with the GUID "<CHAR_GUID>"
        And a Player with the Public CD Key "<PLAYER_KEY>"
        When the Character is assigned to the Player with the Public CD Key "<PLAYER_KEY>"
        Then the Character should be owned by the Player with the Public CD Key "<PLAYER_KEY>"
        And the Character's GUID should still be "<CHAR_GUID>"
