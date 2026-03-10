using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape;

[ServiceBinding(typeof(ShapeFactory))]
public class ShapeFactory
{
    private readonly Dictionary<ShapeType, IShape> _shapes;

    public ShapeFactory(IEnumerable<IShape> shapes) => _shapes = shapes.ToDictionary(s => s.ShapeType);

    public IShape? GetShapeType(ShapeType shapeType)
    {
        return _shapes.GetValueOrDefault(shapeType);
    }
}
