using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

class TagComparer : IEqualityComparer<TypeSymbol>
{
    public bool Equals(TypeSymbol? x, TypeSymbol? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        return x != null && y != null && x.TypeCheck(y, false);
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

        return obj.GetHashCode();
    }
}