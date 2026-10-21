using System.Diagnostics;
using System.Numerics;

class Semantics
{
    public List<Stmt> Check(List<Stmt> stmts)
    {
        List<Stmt> newStmts = [];
        foreach (var stmt in stmts)
            newStmts.Add(CheckStmt(stmt));
        return newStmts;
    }

    Stmt CheckStmt(Stmt stmt)
    {
        if (stmt is UnboundLetStmt unboundLetStmt)
        {
            var type = ResolveSymbol(unboundLetStmt.PSymbol)
                .RejectVoid("Cannot use void as a variable type", unboundLetStmt.Position); ;
            var resolvedExpr = CheckExpr(unboundLetStmt.Expr);
            resolvedExpr.TypeSymbol.RejectVoid("Expression cannot be void", unboundLetStmt.Position);
            Scopes.Declare(unboundLetStmt.Name, type, unboundLetStmt.Position);
            return new BoundLetStmt(
                type,
                unboundLetStmt.Name,
                resolvedExpr,
                unboundLetStmt.Position
            );
        }
        else if (stmt is InferStmt inferStmt)
        {
            var resolvedExpr = CheckExpr(inferStmt.Expr);
            resolvedExpr.TypeSymbol.RejectVoid("Expression cannot be void", inferStmt.Position);
            Scopes.Declare(inferStmt.Name, resolvedExpr.TypeSymbol, inferStmt.Position);
            return new BoundLetStmt(
                resolvedExpr.TypeSymbol,
                inferStmt.Name,
                resolvedExpr,
                inferStmt.Position
            );
        }
        else if (stmt is ExprStmt exprStmt)
        {
            if (exprStmt.Expr is not (AssignExpr or PointerAssignExpr))
                throw new Error("Expression statement can only be an assignment or call", exprStmt.Position);
            return new ExprStmt(
                CheckExpr(exprStmt.Expr).Expr,
                exprStmt.Position
            );
        }

        throw new UnreachableException($"Invalid statement: {stmt.GetType().Name}");
    }

    ResolvedExpr CheckExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                return new(intExpr, ResolveInteger(intExpr.Value, intExpr.Position));

            case FloatExpr floatExpr:
                return new(floatExpr, ResolveFloat(floatExpr.Value));

            case StringExpr stringExpr:
                return new(stringExpr, TypeSymbol.String);

            case CharExpr charExpr:
                return new(charExpr, TypeSymbol.Char);

            case BoolExpr boolExpr:
                return new(boolExpr, TypeSymbol.Bool);

            case NameExpr nameExpr:
                return new(
                    nameExpr,
                    Scopes.Resolve(
                        nameExpr.Name,
                        nameExpr.Position
                    ).TypeSymbol
                );

            case UnaryExpr unaryExpr:
                {
                    var resolved = CheckExpr(unaryExpr.Right);
                    TypeSymbol type = resolved.TypeSymbol;
                    if (unaryExpr.Op == TokenType.Bang)
                        type = TypeSymbol.Bool;
                    else if (unaryExpr.Op == TokenType.BitwiseAnd)
                        type = new PointerSymbol(type);
                    else if (unaryExpr.Op == TokenType.BackTick)
                    {
                        if (type is PointerSymbol pointer)
                        {
                            if (pointer.Inner is VoidSymbol)
                                throw new Error("Cannot deference a void pointer", unaryExpr.Position);
                            type = pointer.Inner;
                        }
                        else if (type is StringSymbol stringSymbol)
                            type = TypeSymbol.Char;
                        else
                            throw new Error("Cannot deference a string or non-pointer", unaryExpr.Position);
                    }
                    else if (type is NumericSymbol n && !n.IsFloat && n.Bits < 32)
                        type = TypeSymbol.Int;
                    return new(unaryExpr with { Right = resolved.Expr }, type);
                }

            case BinaryExpr binaryExpr:
                {
                    var left = CheckExpr(binaryExpr.Left);
                    var right = CheckExpr(binaryExpr.Right);

                    if (left.TypeSymbol == TypeSymbol.Char || left.TypeSymbol == TypeSymbol.Bool)
                        left = left with { TypeSymbol = TypeSymbol.Int };

                    if (right.TypeSymbol == TypeSymbol.Char || right.TypeSymbol == TypeSymbol.Bool)
                        right = right with { TypeSymbol = TypeSymbol.Int };

                    var newExpr = binaryExpr with
                    {
                        Left = left.Expr,
                        Right = right.Expr
                    };

                    if (binaryExpr.Op is
                        TokenType.And or TokenType.Or or
                        TokenType.IsEqual or TokenType.NotEqual or
                        TokenType.Less or TokenType.Greater or
                        TokenType.LessEq or TokenType.GreaterEq)
                    {
                        return new(newExpr, TypeSymbol.Bool);
                    }

                    if (left.TypeSymbol is PointerSymbol pointer &&
                        right.TypeSymbol is NumericSymbol number &&
                        !number.IsFloat &&
                        binaryExpr.Op is TokenType.Add or TokenType.Sub)
                    {
                        return new(newExpr, pointer);
                    }

                    if (right.TypeSymbol is PointerSymbol pointerRight &&
                        left.TypeSymbol is NumericSymbol numberLeft &&
                        !numberLeft.IsFloat &&
                        binaryExpr.Op == TokenType.Add)
                    {
                        return new(newExpr, pointerRight);
                    }

                    if (left.TypeSymbol is PointerSymbol &&
                        right.TypeSymbol is PointerSymbol &&
                        binaryExpr.Op == TokenType.Sub)
                    {
                        return new(newExpr, TypeSymbol.Long);
                    }

                    if (left.TypeSymbol is NumericSymbol l && right.TypeSymbol is NumericSymbol r)
                    {
                        if (binaryExpr.Op is TokenType.BitwiseLeftShift or TokenType.BitwiseRightShift)
                        {
                            if (!l.IsFloat && l.Bits < 32)
                                l = TypeSymbol.Int;
                            return new ResolvedExpr(newExpr, l);
                        }
                        return new(newExpr, PromoteArithmetic(l, r));
                    }

                    throw new Error("Binary error", expr.Position);
                }

            case AssignExpr assignExpr:
                {
                    Scopes.Resolve(assignExpr.Name, assignExpr.Position);
                    var resolved = CheckExpr(assignExpr.Value);
                    var newExpr = new AssignExpr(assignExpr.Name, resolved.Expr, assignExpr.Position);
                    return new(newExpr, resolved.TypeSymbol);
                }

            case PointerAssignExpr pointerAssignExpr:
                {
                    var resolved = CheckExpr(pointerAssignExpr.Target);
                    var value = CheckExpr(pointerAssignExpr.Expr);
                    var newExpr = pointerAssignExpr with
                    {
                        Target = (UnaryExpr)resolved.Expr,
                        Expr = value.Expr
                    };
                    return new(newExpr, resolved.TypeSymbol);
                }
        }

        throw new UnreachableException($"Invalid expression: {expr.GetType().Name}");
    }

    TypeSymbol ResolveSymbol(PSymbol pSymbol)
    {
        switch (pSymbol)
        {
            case NamePSymbol namePSymbol:
                {
                    if (!TypeRegistery.Registery.TryGetValue(namePSymbol.Name, out var value))
                        throw new Error($"Type '{namePSymbol.Name}' does not exist", pSymbol.Position);
                    return value;
                }

            case PointerPSymbol pointerPSymbol:
                return new PointerSymbol(ResolveSymbol(pointerPSymbol.InnerSymbol));

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
}