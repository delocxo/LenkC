abstract record CIrInstruction;
record CIrVariable(TypeSymbol Type, string Name, CIrValue Value) : CIrInstruction;
record CIrValueStmt(CIrValue Value) : CIrInstruction;

abstract record CIrValue;
record CIrLiteral(object Value) : CIrValue;
record CIrLocal(string Name) : CIrValue;
record CIrUnary(TokenType Op, CIrValue Right) : CIrValue;
record CIrBinary(TokenType Op, CIrValue Left, CIrValue Right) : CIrValue;
record CIrAssign(string Name, CIrValue Value) : CIrValue;
record CIrPointerAssign(CIrValue Target, CIrValue Value) : CIrValue;