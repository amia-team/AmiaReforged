using WorldSimulator.Domain.WorkPayloads;

namespace WorldSimulator.Tests.Domain;

/// <summary>
/// Unit tests for typed work item payloads
/// Following TDD principles to ensure validation and type safety
/// </summary>
[TestFixture]
public class WorkPayloadTests
{
    [TestFixture]
    public class DominionTurnPayloadTests
    {
        [Test]
        public void Validate_ShouldSucceed_WhenAllRequiredFieldsAreProvided()
        {
            // Arrange
            DominionTurnPayload payload = new DominionTurnPayload
            {
                DominionId = Guid.NewGuid(),
                DominionName = "Kingdom of Amia",
                TurnDate = DateTime.UtcNow,
                TerritoryIds = new[] { Guid.NewGuid() }
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void Validate_ShouldFail_WhenDominionIdIsEmpty()
        {
            // Arrange
            DominionTurnPayload payload = new DominionTurnPayload
            {
                DominionId = Guid.Empty,
                DominionName = "Test Kingdom",
                TurnDate = DateTime.UtcNow,
                TerritoryIds = new[] { Guid.NewGuid() }
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("DominionId cannot be empty");
        }

        [Test]
        public void Validate_ShouldFail_WhenDominionNameIsEmpty()
        {
            // Arrange
            DominionTurnPayload payload = new DominionTurnPayload
            {
                DominionId = Guid.NewGuid(),
                DominionName = "",
                TurnDate = DateTime.UtcNow,
                TerritoryIds = new[] { Guid.NewGuid() }
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("DominionName is required");
        }

        [Test]
        public void Validate_ShouldFail_WhenNoEntitiesProvided()
        {
            // Arrange
            DominionTurnPayload payload = new DominionTurnPayload
            {
                DominionId = Guid.NewGuid(),
                DominionName = "Test Kingdom",
                TurnDate = DateTime.UtcNow
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("At least one Territory, Region, or Settlement is required");
        }
    }

    [TestFixture]
    public class CivicStatsPayloadTests
    {
        [Test]
        public void Validate_ShouldSucceed_WithValidSettlement()
        {
            // Arrange
            CivicStatsPayload payload = new CivicStatsPayload
            {
                SettlementId = Guid.NewGuid(),
                SettlementName = "Cordor"
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ShouldFail_WhenLookbackPeriodIsNegative()
        {
            // Arrange
            CivicStatsPayload payload = new CivicStatsPayload
            {
                SettlementId = Guid.NewGuid(),
                SettlementName = "Cordor",
                LookbackPeriod = TimeSpan.FromDays(-1)
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("LookbackPeriod cannot be negative");
        }

        [Test]
        public void Validate_ShouldFail_WhenLookbackPeriodExceedsMaximum()
        {
            // Arrange
            CivicStatsPayload payload = new CivicStatsPayload
            {
                SettlementId = Guid.NewGuid(),
                SettlementName = "Cordor",
                LookbackPeriod = TimeSpan.FromDays(400)
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("LookbackPeriod cannot exceed 365 days");
        }
    }

    [TestFixture]
    public class PersonaActionPayloadTests
    {
        [Test]
        public void Validate_ShouldSucceed_WithValidAction()
        {
            // Arrange
            PersonaActionPayload payload = new PersonaActionPayload
            {
                PersonaId = Guid.NewGuid(),
                PersonaName = "Lord Blackwood",
                ActionType = "Intrigue",
                InfluenceCost = 100,
                TargetEntityId = Guid.NewGuid()
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ShouldFail_WhenInfluenceCostIsNegative()
        {
            // Arrange
            PersonaActionPayload payload = new PersonaActionPayload
            {
                PersonaId = Guid.NewGuid(),
                PersonaName = "Test Persona",
                ActionType = "Intrigue",
                InfluenceCost = -50,
                TargetEntityId = Guid.NewGuid()
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("InfluenceCost cannot be negative");
        }

        [Test]
        public void Validate_ShouldFail_WhenActionTypeIsEmpty()
        {
            // Arrange
            PersonaActionPayload payload = new PersonaActionPayload
            {
                PersonaId = Guid.NewGuid(),
                PersonaName = "Test Persona",
                ActionType = "",
                InfluenceCost = 100,
                TargetEntityId = Guid.NewGuid()
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("ActionType is required");
        }
    }

    [TestFixture]
    public class MarketPricingPayloadTests
    {
        [Test]
        public void Validate_ShouldSucceed_WhenRecalculateAllItemsIsTrue()
        {
            // Arrange
            MarketPricingPayload payload = new MarketPricingPayload
            {
                MarketId = Guid.NewGuid(),
                MarketName = "Grand Bazaar",
                RecalculateAllItems = true
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ShouldSucceed_WhenItemIdsAreProvided()
        {
            // Arrange
            MarketPricingPayload payload = new MarketPricingPayload
            {
                MarketId = Guid.NewGuid(),
                MarketName = "Grand Bazaar",
                ItemIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ShouldFail_WhenNeitherRecalculateAllNorItemIdsProvided()
        {
            // Arrange
            MarketPricingPayload payload = new MarketPricingPayload
            {
                MarketId = Guid.NewGuid(),
                MarketName = "Grand Bazaar",
                RecalculateAllItems = false
            };

            // Act
            ValidationResult result = payload.Validate();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Either RecalculateAllItems must be true or ItemIds must be provided");
        }
    }

    [TestFixture]
    public class SimulationWorkItemFactoryTests
    {
        [Test]
        public void Create_ShouldCreateWorkItemWithTypedPayload()
        {
            // Arrange
            DominionTurnPayload payload = new DominionTurnPayload
            {
                DominionId = Guid.NewGuid(),
                DominionName = "Kingdom of Amia",
                TurnDate = DateTime.UtcNow,
                TerritoryIds = new[] { Guid.NewGuid() }
            };

            // Act
            SimulationWorkItem workItem = SimulationWorkItem.Create(payload);

            // Assert
            workItem.Should().NotBeNull();
            workItem.WorkType.Should().Be("DominionTurn");
            workItem.Status.Should().Be(WorkItemStatus.Pending);
            workItem.Payload.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Create_ShouldThrowArgumentException_WhenPayloadIsInvalid()
        {
            // Arrange
            DominionTurnPayload invalidPayload = new DominionTurnPayload
            {
                DominionId = Guid.Empty,  // Invalid
                DominionName = "",         // Invalid
                TurnDate = default
            };

            // Act & Assert
            Action act = () => SimulationWorkItem.Create(invalidPayload);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Invalid payload*")
                .WithMessage("*DominionId cannot be empty*");
        }

        [Test]
        public void GetPayload_ShouldDeserializeToCorrectType()
        {
            // Arrange
            CivicStatsPayload originalPayload = new CivicStatsPayload
            {
                SettlementId = Guid.NewGuid(),
                SettlementName = "Cordor",
                LookbackPeriod = TimeSpan.FromDays(30)
            };
            SimulationWorkItem workItem = SimulationWorkItem.Create(originalPayload);

            // Act
            CivicStatsPayload deserializedPayload = workItem.GetPayload<CivicStatsPayload>();

            // Assert
            deserializedPayload.Should().NotBeNull();
            deserializedPayload.SettlementId.Should().Be(originalPayload.SettlementId);
            deserializedPayload.SettlementName.Should().Be(originalPayload.SettlementName);
            deserializedPayload.LookbackPeriod.Should().Be(originalPayload.LookbackPeriod);
        }

        [Test]
        public void TryGetPayload_ShouldReturnTrue_WhenDeserializationSucceeds()
        {
            // Arrange
            PersonaActionPayload originalPayload = new PersonaActionPayload
            {
                PersonaId = Guid.NewGuid(),
                PersonaName = "Lord Blackwood",
                ActionType = "Intrigue",
                InfluenceCost = 100,
                TargetEntityId = Guid.NewGuid()
            };
            SimulationWorkItem workItem = SimulationWorkItem.Create(originalPayload);

            // Act
            bool success = workItem.TryGetPayload<PersonaActionPayload>(out PersonaActionPayload? payload);

            // Assert
            success.Should().BeTrue();
            payload.Should().NotBeNull();
            payload!.PersonaName.Should().Be("Lord Blackwood");
        }

        [Test]
        public void TryGetPayload_ShouldReturnFalse_WhenDeserializationFails()
        {
            // Arrange
            DominionTurnPayload dominionPayload = new DominionTurnPayload
            {
                DominionId = Guid.NewGuid(),
                DominionName = "Test",
                TurnDate = DateTime.UtcNow,
                TerritoryIds = new[] { Guid.NewGuid() }
            };
            SimulationWorkItem workItem = SimulationWorkItem.Create(dominionPayload);

            // Act - Try to deserialize to wrong type
            bool success = workItem.TryGetPayload<CivicStatsPayload>(out CivicStatsPayload? payload);

            // Assert
            success.Should().BeFalse();
            payload.Should().BeNull();
        }
    }
}

