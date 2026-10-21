using System.Diagnostics;

class Lowerer
{
    public List<CIrInstruction> Instructions { get; } = [];

    public void LowererStmts(List<Stmt> stmts)
    {
        foreach (Stmt stmt in stmts)
            LowererStmt(stmt);
    }

    void LowererStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case BoundLetStmt boundLetStmt:
                {
                    var value = LowerExpr(boundLetStmt.Expr);
                    Instructions.Add(new CIrVariable(
                        boundLetStmt.TypeSymbol,
                        boundLetStmt.Name,
                        value
                    ));
                    return;
                }
        }

        throw new UnreachableException();
    }

    CIrValue LowerExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                return new CIrLiteral(intExpr.Value);

            case FloatExpr floatExpr:
                return new CIrLiteral(floatExpr.Value);

            case StringExpr stringExpr:
                return new CIrLiteral(stringExpr.Value);

            case CharExpr charExpr:
                return new CIrLiteral(charExpr.Value);

            case BoolExpr boolExpr:
                return new CIrLiteral(boolExpr.Value);

            case NameExpr nameExpr:
                return new CIrLocal(nameExpr.Name);

            case UnaryExpr unaryExpr:
                {
                    var right = LowerExpr(unaryExpr.Right);
                    return new CIrUnary(unaryExpr.Op, right);
                }

            case BinaryExpr binaryExpr:
                {
                    var left = LowerExpr(binaryExpr.Left);
                    var right = LowerExpr(binaryExpr.Right);
                    return new CIrBinary(binaryExpr.Op, left, right);
                }

            case ResolvedExpr resolvedExpr:
                return LowerExpr(resolvedExpr.Expr);
        }

        throw new UnreachableException(expr.GetType().Name);
    }
}