record PSymbol(Position Position)
{
    public bool TypeCheck(PSymbol other)
    {
        if (this is NamePSymbol leftNP && other is NamePSymbol rightNP)
        {
            return leftNP.Name == rightNP.Name &&
                leftNP.TypeArguments.Count == rightNP.TypeArguments.Count &&
                leftNP.TypeArguments
                    .Zip(rightNP.TypeArguments)
                    .All(x => x.First.TypeCheck(x.Second));
        }
        if (this is PointerPSymbol leftP && other is PointerPSymbol rightP)
        {
            return leftP.InnerSymbol.TypeCheck(rightP.InnerSymbol);
        }
        if (this is FunctionPSymbol leftF && other is FunctionPSymbol rightF)
        {
            return leftF.ReturnType.TypeCheck(rightF.ReturnType) &&
                leftF.Parameters.Count == rightF.Parameters.Count &&
                leftF.Parameters.Zip(rightF.Parameters).All(pair =>
                    pair.First.IsVariadc == pair.Second.IsVariadc &&
                    (pair.First.IsVariadc ||
                    pair.First.Symbol.TypeCheck(pair.Second.Symbol))
                );
        }
        return false;
    }
}
record NamePSymbol(string Name, List<PSymbol> TypeArguments, Position Position) : PSymbol(Position);
record PointerPSymbol(PSymbol InnerSymbol, Position Position) : PSymbol(Position);
// record ArrayPSymbol(PSymbol InnerSymbol, Position Position) : PSymbol(Position);
record FunctionParameterPSymbol(PSymbol Symbol, bool IsVariadc);
record FunctionPSymbol(PSymbol ReturnType, List<FunctionParameterPSymbol> Parameters, Position Position) : PSymbol(Position);
record StructPSymbol(string Name, Position Position) : PSymbol(Position);