abstract record PSymbol(Position Position);
record NamePSymbol(string Name, Position Position) : PSymbol(Position);
record ArrayPSymbol(PSymbol InnerSymbol, Position Position) : PSymbol(Position);