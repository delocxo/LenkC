using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

abstract record Expr(Position Position);
record IntExpr(BigInteger Value, Position Position) : Expr(Position);
record FloatExpr(double Value, Position Position) : Expr(Position);
record StringExpr(string Value, Position Position) : Expr(Position);
record CharExpr(char Value, Position Position) : Expr(Position);
record BoolExpr(bool Value, Position Position) : Expr(Position);
record NullExpr(Position Position) : Expr(Position);
record NameExpr(string Name, List<PSymbol> TypeArguments, Position Position) : Expr(Position);
record UnaryExpr(Expr Right, TokenType Op, Position Position) : Expr(Position);
record BinaryExpr(Expr Left, Expr Right, TokenType Op, Position Position) : Expr(Position);
record AssignExpr(string Name, Expr Value, Position Position) : Expr(Position);
record PointerAssignExpr(UnaryExpr Target, Expr Expr, Position Position) : Expr(Position);
record UnboundNativeExpr(PSymbol Symbol, string Source, Position Position) : Expr(Position);
abstract record NativePart;
record NativeText(string Text) : NativePart;
record NativeExpr(Expr Expr) : NativePart;
record BoundNativeExpr(TypeSymbol Symbol, List<NativePart> Parts, Position Position) : Expr(Position);
record UnboundCastExpr(PSymbol Symbol, Expr Value, Position Position) : Expr(Position);
record BoundCastExpr(TypeSymbol Symbol, Expr Value, Position Position) : Expr(Position);
record CallExpr(Expr Target, List<Expr> Arguments, Position Position) : Expr(Position);
record UnboundCNameExpr(PSymbol Symbol, Position Position) : Expr(Position);
record BoundCNameExpr(TypeSymbol TypeSymbol, Position Position) : Expr(Position);
record ResolvedExpr(Expr Expr, TypeSymbol TypeSymbol) : Expr(Expr.Position);

abstract class Stmt
{
    public Stmt(Position position)
    {
        Position = position;
    }

    public Position Position { get; }
}


class UnboundLetStmt : Stmt
{
    public UnboundLetStmt(PSymbol pSymbol, string name, Expr expr, Position position) : base(position)
    {
        PSymbol = pSymbol;
        Name = name;
        Expr = expr;
    }

    public PSymbol PSymbol { get; }
    public string Name { get; }
    public Expr Expr { get; }
}

class BoundLetStmt : Stmt
{
    public BoundLetStmt(TypeSymbol typeSymbol, string name, ResolvedExpr expr, Position position) : base(position)
    {
        TypeSymbol = typeSymbol;
        Name = name;
        Expr = expr;
    }

    public TypeSymbol TypeSymbol { get; }
    public string Name { get; }
    public ResolvedExpr Expr { get; }
}

class InferStmt : Stmt
{
    public InferStmt(string name, Expr expr, Position position) : base(position)
    {
        Name = name;
        Expr = expr;
    }

    public string Name { get; }
    public Expr Expr { get; }
}

class ExprStmt : Stmt
{
    public ExprStmt(Expr expr, Position position) : base(position)
    {
        Expr = expr;
    }

    public Expr Expr { get; }
}

class IfBranch
{
    public IfBranch(Expr expr, List<Stmt> stmts)
    {
        Expr = expr;
        Stmts = stmts;
    }

    public Expr Expr { get; set; }
    public List<Stmt> Stmts { get; set; }
}

class IfStmt : Stmt
{
    public IfStmt(List<IfBranch> ifBranches, List<Stmt>? elseBranch, Position position) : base(position)
    {
        IfBranches = ifBranches;
        ElseBranch = elseBranch;
    }

    public List<IfBranch> IfBranches { get; }
    public List<Stmt>? ElseBranch { get; set; }
}

class WhileStmt : Stmt
{
    public WhileStmt(IfBranch branch, Position position) : base(position)
    {
        Branch = branch;
    }

    public IfBranch Branch { get; }
}

class BreakStmt : Stmt
{
    public BreakStmt(Position position) : base(position)
    {
    }
}

class ContinueStmt : Stmt
{
    public ContinueStmt(Position position) : base(position)
    {
    }
}

class UnboundField
{
    public UnboundField(PSymbol symbol, string name, Position position)
    {
        Symbol = symbol;
        Name = name;
        Position = position;
    }

    public PSymbol Symbol { get; }
    public string Name { get; }
    public Position Position { get; }
}

class BoundField
{
    public BoundField(TypeSymbol type, string name)
    {
        Type = type;
        Name = name;
    }

    public TypeSymbol Type { get; }
    public string Name { get; }
}

class UnboundFunctionStmt : Stmt
{
    public UnboundFunctionStmt(PSymbol returnType, string name, List<string> typeParameters, List<UnboundField> parameters, List<Stmt> body, Position position) : base(position)
    {
        ReturnType = returnType;
        Name = name;
        TypeParameters = typeParameters;
        Parameters = parameters;
        Body = body;
    }

    public PSymbol ReturnType { get; }
    public string Name { get; }
    public List<string> TypeParameters { get; }
    public List<UnboundField> Parameters { get; }
    public List<Stmt> Body { get; }
}

class BoundFunctionStmt : Stmt
{
    public BoundFunctionStmt(TypeSymbol returnType, string name, List<BoundField> parameters, List<Stmt> body, Position position) : base(position)
    {
        ReturnType = returnType;
        Name = name;
        Parameters = parameters;
        Body = body;
    }

    public TypeSymbol ReturnType { get; }
    public string Name { get; }
    public List<BoundField> Parameters { get; }
    public List<Stmt> Body { get; }
}

class ReturnStmt : Stmt
{
    public ReturnStmt(Expr? expr, Position position) : base(position)
    {
        Expr = expr;
    }

    public Expr? Expr { get; }
}