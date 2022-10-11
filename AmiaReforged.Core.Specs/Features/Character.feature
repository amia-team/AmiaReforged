Feature: Character
A Character is a PC or NPC that can enter and exit from Factions and Territories.

	@mytag
		Scenario: A Character is made by a player
		Given a player
		When the player makes a character
		Then an entry for the character is made denoting the character as player controlled

	@mytag
		Scenario: A Character is made by a Dungeon Master player
		Given a Dungeon Master
		When an NPC is recorded as a Character
		Then a record for the character should be made denoting the character as an non-player controlled

	@mytag
	Scenario: A Character is modified
		Given a character
		When a character modification is pushed to the system
		Then the record for the character should be updated

	@mytag
	Scenario: An existing character is flagged as temporal
		Given a pre-existing character
		When an expiration time is set through a character modification
		Then the character should be flagged as temporal