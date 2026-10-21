abstract record TypeSymbol(string Name, string CName, string FormatSymbol)
{
    public static readonly StringSymbol String = new();
    public static readonly BoolSymbol Bool = new();
    public static readonly NullSymbol Null = new();
    public static readonly CharSymbol Char = new();

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