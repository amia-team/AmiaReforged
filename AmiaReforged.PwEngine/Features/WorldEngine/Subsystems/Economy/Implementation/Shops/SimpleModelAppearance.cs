namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public sealed class SimpleModelAppearance
{
    public SimpleModelAppearance(int modelType, int? simpleModelNumber)
    {
        if (modelType < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(modelType), modelType, "Model type must be non-negative.");
        }

        if (simpleModelNumber is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(simpleModelNumber), simpleModelNumber,
                "Simple model number must be non-negative when provided.");
        }

        ModelType = modelType;
        SimpleModelNumber = simpleModelNumber;
    }

    public int ModelType { get; }

    public int? SimpleModelNumber { get; }

    public void ApplyTo(IItemAppearanceWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.SetSimpleModel(ModelType, SimpleModelNumber);
    }
}
