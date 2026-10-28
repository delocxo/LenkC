using System.Numerics;

partial class Semantics
{
    public TypeSymbol ResolveSymbol(PSymbol pSymbol)
    {
        switch (pSymbol)
        {
            case NamePSymbol namePSymbol:
                {
                    if (TypeRegistery.Registery.TryGetValue(namePSymbol.Name, out var value))
                    {
                        if (namePSymbol.TypeArguments.Count > 0)
                            throw new Error($"Type '{namePSymbol.Name}' does not take any generic arguments", namePSymbol.Position);
                        return value;
                    }
                    else if (s_structs.TryGetValue(namePSymbol.Name, out var unboundStruct))
                    {
                        if (namePSymbol.TypeArguments.Count != unboundStruct.TypeParameters.Count)
                            throw new Error($"Struct '{namePSymbol.Name}' expects {unboundStruct.TypeParameters.Count} type argument(s), got {namePSymbol.TypeArguments.Count}", unboundStruct.Position);

                        var typeArguments = namePSymbol.TypeArguments
                            .Select(x => ResolveSymbol(x).RejectVoid("Type argument cannot be void", x.Position))
                            .ToList();

                        List<BoundField> fields = [];
                        StructSymbol structSymbol = new(unboundStruct.Name, unboundStruct.CName, fields, typeArguments, unboundStruct.IsUnion);

                        string key = TypeKey(structSymbol);

                        if (BoundStructs.TryGetValue(key, out var bound))
                            return bound;

                        if (unboundStruct.MangleName)
                            structSymbol = structSymbol with { CName = key };

                        BoundStructs[key] = structSymbol;

                        var mapping = unboundStruct.TypeParameters
                            .Zip(typeArguments)
                            .ToDictionary(x => x.First, x => x.Second);

                        Dictionary<string, TypeSymbol?> previousTypes = [];
                        foreach (var pair in mapping)
                        {
                            TypeRegistery.Registery.TryGetValue(pair.Key, out var previous);
                            previousTypes.Add(pair.Key, previous);
                            TypeRegistery.Registery[pair.Key] = pair.Value;
                        }

                        foreach (var field in unboundStruct.Fields)
                        {
                            var symbol = ResolveSymbol(field.Symbol).RejectVoid($"'{field.Name}' cannont be void", field.Position);
                            fields.Add(new(symbol, field.Name));
                        }

                        foreach (var pair in previousTypes)
                            if (pair.Value == null)
                                TypeRegistery.Registery.Remove(pair.Key);
                            else
                                TypeRegistery.Registery[pair.Key] = pair.Value;

                        return structSymbol;
                    }
                    throw new Error($"Type '{namePSymbol.Name}' does not exist", pSymbol.Position);
                }

            case PointerPSymbol pointerPSymbol:
                return new PointerSymbol(ResolveSymbol(pointerPSymbol.InnerSymbol));

            case FunctionPSymbol functionPSymbol:
                {
                    var parameterTypes = functionPSymbol.Parameters
                        .Select(ResolveSymbol)
                        .ToList();

                    var returnType = ResolveSymbol(functionPSymbol.ReturnType);

                    return new FunctionSymbol(returnType, parameterTypes);
                }

            default:
                throw new NotImplementedException();
        }
    }

    TypeSymbol ResolveInteger(BigInteger value, Position position)
    {
        if (value <= int.MaxValue)
            return TypeSymbol.Int;
        else if (value <= long.MaxValue)
            return TypeSymbol.Long;
        else if (value <= ulong.MaxValue)
            return TypeSymbol.ULong;
        throw new Error("Integer literal to larger", position);
    }

    TypeSymbol ResolveFloat(double value)
    {
        return value <= float.MaxValue ? TypeSymbol.Float : TypeSymbol.Double;
    }

    NumericSymbol PromoteArithmetic(NumericSymbol left, NumericSymbol right)
    {
        if (!left.IsFloat && left.Bits < 32)
            left = TypeSymbol.Int;

        if (!right.IsFloat && right.Bits < 32)
            right = TypeSymbol.Int;

        return left.Promote(right);
    }

    TypeSymbol Promote(TypeSymbol typeSymbol)
    {
        if (typeSymbol is CharSymbol || typeSymbol is BoolSymbol)
            return TypeSymbol.Int;

        if (typeSymbol is NumericSymbol numericSymbol)
        {
            if (!numericSymbol.IsFloat && numericSymbol.Bits < 32)
                return TypeSymbol.Int;
            return numericSymbol;
        }

        return typeSymbol;
    }

    string TypeKey(TypeSymbol type) => type switch
    {
        PointerSymbol pointer => $"ptr_{TypeKey(pointer.Inner)}_end",
        FunctionSymbol function =>
            $"fn_{function.ParameterTypes.Count}_" +
            string.Join("_", function.ParameterTypes.Select(TypeKey)) +
            $"_ret_{TypeKey(function.ReturnType)}_end",
        StructSymbol structSymbol =>
            (structSymbol.IsUnion ? $"union_" : $"struct_") + structSymbol.Name + "_" + $"{structSymbol.TypeArguments.Count}_" +
            string.Join("_", structSymbol.TypeArguments.Select(TypeKey)) +
            "_end",
        _ => type.Name
    };

    public bool FitsInteger(BigInteger value, NumericSymbol numericSymbol)
    {
        if (numericSymbol.IsFloat)
            return false;

        BigInteger min = 0;
        BigInteger max = 0;

        if (numericSymbol.IsSigned)
        {
            int bits = numericSymbol.Bits - 1;

            min = -BigInteger.Pow(2, bits);
            max = BigInteger.Pow(2, bits) - 1;
        }
        else
        {
            max = BigInteger.Pow(2, numericSymbol.Bits) - 1;
        }

        return value >= min && value <= max;
    }

    public bool FitsFloat(double value, NumericSymbol type)
    {
        if (!type.IsFloat || !double.IsFinite(value))
            return false;

        return type.Bits switch
        {
            32 => Math.Abs(value) <= float.MaxValue,
            64 => true,
            _ => false
        };
    }
}