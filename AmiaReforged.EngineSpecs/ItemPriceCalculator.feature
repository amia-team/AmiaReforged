Feature: Item Price Calculation

    Rule: The price for items is based on its base cost, times its demand based on its current locality,
    plus its base cost multiplied by its quality and material cost modifiers.

        Scenario: Item is bought in a region with standard demand (1.0)
            Given an Sale Transaction with an Item named "Oak Log"
            And the Base Cost is 50
            And the Item is made of Oak
            And the Item quality is Average
            And a locality demand of 1.0
