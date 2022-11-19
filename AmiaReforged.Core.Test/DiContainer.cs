using System.ComponentModel.Design;
using AmiaReforged.System.Helpers;
using AmiaReforged.System.Services;
using BoDi;

namespace AmiaReforged.Core.Test;

public class DiContainer
{
    
    public static IObjectContainer Container { get; set; } = new ObjectContainer();
    
    public static IObjectContainer Configure ()
    {
        Container.Re
        return Container;
    }
}