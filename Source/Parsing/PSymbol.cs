abstract record PSymbol(Position Position);
record NamePSymbol(string Name, List<PSymbol> TypeArguments, Position Position) : PSymbol(Position);
record PointerPSymbol(PSymbol InnerSymbol, Position Position) : PSymbol(Position);
record ArrayPSymbol(PSymbol InnerSymbol, Position Position) : PSymbol(Position);
record FunctionPSymbol(PSymbol ReturnType, List<PSymbol> Parameters, Position Position) : PSymbol(Position);