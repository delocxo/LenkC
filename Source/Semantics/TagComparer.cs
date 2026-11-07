using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

class TagComparer : IEqualityComparer<TypeSymbol>
{
    public bool Equals(TypeSymbol? x, TypeSymbol? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;
        if (x is StructNameSymbol || y is StructNameSymbol)
            return x is StructNameSymbol left &&
                y is StructNameSymbol right &&
                left.Name == right.Name;
        return x.TypeCheck(y, false);
    }

    public int GetHashCode(TypeSymbol obj)
    {
        if (obj is StructSymbol)
            return RuntimeHelpers.GetHashCode(obj);

        if (obj is FunctionSymbol function)
        {
            HashCode hash = new();

            hash.Add(typeof(FunctionSymbol));
            hash.Add(function.IsVariadic);
            hash.Add(GetHashCode(function.ReturnType));
            hash.Add(function.ParameterTypes.Count);

            foreach (var param in function.ParameterTypes)
                hash.Add(GetHashCode(param));

            return hash.ToHashCode();
        }

        if (obj is StructNameSymbol definition)
            return HashCode.Combine(typeof(StructNameSymbol), definition.Name);

        return obj.GetHashCode();
    }
}