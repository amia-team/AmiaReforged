Feature: FactionRelation
As a Faction
I need to be able to have a relation with another faction
So that I know who my friends and enemies are

    @mytag
    Scenario: A newly added faction should have a neutral relationship with every faction
        Given a list of Factions with random names and descriptions
        And the list of Factions is persisted
        And a Faction named "Factioneers"
        And with the description "People who just make factions for no reason"
        When a request is made to persist the Faction
        Then the Faction should have a neutral relationship with every other Faction
        
    Scenario: A faction has a neutral relation with another faction
        Given a pair of Factions named "Faction A" and "Faction B"
        When I check the relation between the pair of Factions
        Then the relation should be 0
    
    Scenario: A faction relation for one does not change another
        Given a pair of Factions named "Faction Hates" and "Faction Does Not Care"
        When I set the relation of Faction A with Faction B to -100
        Then the relation of Faction B for Faction A should be 0
        
     Scenario: A faction relation can be set
        Given a pair of Factions named "Faction 1" and "Faction 2"
        When I set the relation of Faction A with Faction B to -100
        Then the relation of Faction A for Faction B should be -100
        