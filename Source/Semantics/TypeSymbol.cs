using System.Numerics;

record TypeSymbol(string Name, string CName, string? FormatSymbol)
{
    public static readonly StringSymbol String = new();
    public static readonly BoolSymbol Bool = new();
    public static readonly NullSymbol Null = new();
    public static readonly CharSymbol Char = new();
    public static readonly VoidSymbol Void = new();

    public static readonly NumericSymbol Byte =
        new("byte", "int8_t", 8, false, true, "%hhd");

    public static readonly NumericSymbol UByte =
        new("ubyte", "uint8_t", 8, false, false, "%hhu");

    public static readonly NumericSymbol Short =
        new("short", "int16_t", 16, false, true, "%hd");

    public static readonly NumericSymbol UShort =
        new("ushort", "uint16_t", 16, false, false, "%hu");

    public static readonly NumericSymbol Int =
        new("int", "int32_t", 32, false, true, "%d");

    public static readonly NumericSymbol UInt =
        new("uint", "uint32_t", 32, false, false, "%u");

    public static readonly NumericSymbol Long =
        new("long", "long long", 64, false, true, "%lld");

    public static readonly NumericSymbol ULong =
        new("ulong", "unsigned long long", 64, false, false, "%llu");

    public static readonly NumericSymbol Float =
        new("float", "float", 32, true, true, "%f");

    public static readonly NumericSymbol Double =
        new("double", "double", 64, true, true, "%f");

    public TypeSymbol RejectVoid(string message, Position position)
    {
        if (this is VoidSymbol voidSymbol)
            throw new Error(message, position);
        return this;
    }

    public bool TypeCheck(TypeSymbol other)
    {
        if (this is PointerSymbol pl && other is PointerSymbol pr)
            return pl.Inner.TypeCheck(pr.Inner);

        if (this is FunctionSymbol fl && other is FunctionSymbol fr)
            return fl.ParameterTypes.Count == fr.ParameterTypes.Count &&
                fl.ParameterTypes
                    .Zip(fr.ParameterTypes)
                    .All(pair => pair.First.TypeCheck(pair.Second))
                && fl.ReturnType.TypeCheck(fr.ReturnType);

        if (this is StructSymbol sl && other is StructSymbol sr)
            return ReferenceEquals(sl, sr);

        if (this is NumericSymbol nl && other is NumericSymbol nr)
        {
            if (nl.IsFloat && nr.IsFloat)
                return nl.Bits >= nr.Bits;

            if (!nl.IsFloat && !nr.IsFloat)
            {
                if (nl.IsSigned == nr.IsSigned)
                    return nl.Bits >= nr.Bits;

                if (nl.IsSigned && !nr.IsSigned)
                    return nl.Bits > nr.Bits;

                return false;
            }

            return false;
        }

        return this == other;
    }

    public sealed override string ToString()
    {
        return this switch
        {
            PointerSymbol pointer =>
                $"{pointer.Inner}`",

            FunctionSymbol function =>
                $"@({string.Join(", ", function.ParameterTypes)}) => {function.ReturnType}",

            StructSymbol str when str.TypeArguments.Count > 0 =>
                $"{str.Name}::<{string.Join(", ", str.TypeArguments)}>",

            _ => Name
        };
    }
}

record NumericSymbol(string Name, string CName, int Bits, bool IsFloat, bool IsSigned, string FormatSymbol) : TypeSymbol(Name, CName, FormatSymbol)
{
    public NumericSymbol Promote(NumericSymbol other)
    {
        if (IsFloat != other.IsFloat)
            return IsFloat ? this : other;

        if (Bits != other.Bits)
            return Bits > other.Bits ? this : other;

        if (!IsFloat && IsSigned != other.IsSigned)
            return IsSigned ? other : this;

        return this;
    }
}

record StringSymbol() : TypeSymbol("string", "char*", "%s");
record BoolSymbol() : TypeSymbol("bool", "int", "%d");
record NullSymbol() : TypeSymbol("null", "NULL", "%p");
record CharSymbol() : TypeSymbol("char", "char", "%c");
record VoidSymbol() : TypeSymbol("void", "void", null);
record PointerSymbol(TypeSymbol Inner) : TypeSymbol("", "", "%p");
record FunctionSymbol(TypeSymbol ReturnType, List<TypeSymbol> ParameterTypes) : TypeSymbol("", "", null);
record StructSymbol(string Name, string CName, List<BoundField> Fields, List<TypeSymbol> TypeArguments, bool IsUnion) : TypeSymbol(Name, CName, null);
record EnumSymbol(TypeSymbol Type, string Name, Dictionary<string, BigInteger> Values);