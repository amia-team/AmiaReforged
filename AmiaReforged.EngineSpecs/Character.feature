Feature: Characters, or Actors, are agents within the broader system. They have inventories, goals, personalities, flaws, and more.

    Scenario: A basic Player Character is created
        Given a Player with the Public CD Key "Q6ULTKYG"
        When the Player finishes Character creation
        Then their character should have a valid GUID

    Scenario: A returning player enters the game
        Given a Player with the Public CD Key "Q6ULTKYG"
        And there is already a Character with the GUID "ce315843-7e79-46c6-938e-8b8b7e438920"
        And the Player is playing the Character with the GUID "ce315843-7e79-46c6-938e-8b8b7e438920"
        Then their Character should be retrieved for use in the system
