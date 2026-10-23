class Local
{
    public Local(string name, TypeSymbol typeSymbol)
    {
        Name = name;
        TypeSymbol = typeSymbol;
    }

    public string Name { get; }
    public TypeSymbol TypeSymbol { get; }
}

static class Scopes
{
    static Stack<Dictionary<string, Local>> _scopes = new([new()]);

    public static bool AtGlobalLevel => _scopes.Count == 1 /* || FunctionLevel > 0*/;
    public static int FunctionLevel = 0;

    public static Local Declare(string name, TypeSymbol typeSymbol, Position position)
    {
        if (_scopes.Peek().ContainsKey(name))
            throw new Error($"'{name}' has already been declared", position);

        Local local = new Local(name, typeSymbol);
        _scopes.Peek().Add(name, local);

        return local;
    }

    public static bool TryResolve(string name, out Local? local)
    {
        foreach (var scope in _scopes)
            if (scope.TryGetValue(name, out local))
                return true;
        local = null;
        return false;
    }

    public static Local Resolve(string name, Position position)
    {
        if (!TryResolve(name, out var local))
            throw new Error($"'{name}' does not exist", position);
        return local!;
    }

    public static void BeginScope() => _scopes.Push(new());
    public static void EndScope() => _scopes.Pop();
}