Feature: FactionRelation
As a Faction
I need to be able to have a relation with another faction
So that I know who my friends and enemies are

    @mytag
    Scenario: A faction has a neutral relation with another faction
        Given a pair of Factions named "Faction A" and "Faction B"
        When I check the relation between the pair of Factions
        Then the relation is 0