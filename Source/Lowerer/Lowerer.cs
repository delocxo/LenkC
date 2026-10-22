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

            case ExprStmt exprStmt:
                {
                    var value = LowerExpr(exprStmt.Expr);
                    Instructions.Add(new CIrValueStmt(value));
                    return;
                }

            case IfStmt ifStmt:
                {
                    for (int i = 0; i < ifStmt.IfBranches.Count; i++)
                    {
                        string name = i == 0 ? "if" : "else if";
                        var branch = ifStmt.IfBranches[i];
                        var condition = LowerExpr(branch.Expr);
                        Instructions.Add(new CIrControlFlowStart(name, condition));
                        LowererStmts(branch.Stmts);
                        Instructions.Add(new CIrControlFlowEnd());
                    }

                    if (ifStmt.ElseBranch != null)
                    {
                        Instructions.Add(new CIrControlFlowStart("else", null));
                        LowererStmts(ifStmt.ElseBranch);
                        Instructions.Add(new CIrControlFlowEnd());
                    }

                    return;
                }

            case WhileStmt whileStmt:
                {
                    var branch = whileStmt.Branch;
                    var condition = LowerExpr(branch.Expr);
                    Instructions.Add(new CIrControlFlowStart("while", condition));
                    LowererStmts(branch.Stmts);
                    Instructions.Add(new CIrControlFlowEnd());
                    return;
                }

            case BreakStmt:
                Instructions.Add(new CIrBreak());
                return;

            case ContinueStmt:
                Instructions.Add(new CIrContinue());
                return;
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

            case NullExpr:
                return new CIrNull();

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

            case AssignExpr assignExpr:
                {
                    var value = LowerExpr(assignExpr.Value);
                    return new CIrAssign(assignExpr.Name, value);
                }

            case PointerAssignExpr pointerAssign:
                {
                    var target = LowerExpr(pointerAssign.Target);
                    var value = LowerExpr(pointerAssign.Expr);
                    return new CIrPointerAssign(target, value);
                }

            case BoundNativeExpr boundNativeExpr:
                {
                    List<CIrNativePart> parts = boundNativeExpr.Parts
                        .Select<NativePart, CIrNativePart>(x =>
                            x switch
                            {
                                NativeExpr ne => new CIrNativeValue(LowerExpr(ne.Expr)),
                                NativeText nt => new CIrNativeText(nt.Text),
                                _ => throw new UnreachableException()
                            }
                        ).ToList();
                    return new CIrNative(parts);
                }

            case ResolvedExpr resolvedExpr:
                return LowerExpr(resolvedExpr.Expr);
        }

        throw new UnreachableException(expr.GetType().Name);
    }
}