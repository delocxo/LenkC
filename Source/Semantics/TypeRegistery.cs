static class TypeRegistery
{
    public static Dictionary<string, TypeSymbol> Registery = new()
    {
    };

    public static void Register(TypeSymbol typeSymbol, Position? position = null)
    {
        if (!Registery.TryAdd(typeSymbol.Name, typeSymbol))
            throw new Error($"Type {typeSymbol.Name} is already an existing type", position);
    }

    public static void RegisterPrimitives()
    {
        Register(TypeSymbol.String);
        Register(TypeSymbol.Bool);
        Register(TypeSymbol.Char);

        Register(TypeSymbol.Byte);
        Register(TypeSymbol.UByte);

        Register(TypeSymbol.Short);
        Register(TypeSymbol.UShort);

        Register(TypeSymbol.Int);
        Register(TypeSymbol.UInt);

        Register(TypeSymbol.Long);
        Register(TypeSymbol.ULong);

        Register(TypeSymbol.Float);
        Register(TypeSymbol.Double);
    }
}