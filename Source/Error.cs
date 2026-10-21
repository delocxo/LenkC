using System;
using System.Collections.Generic;
using System.Text;

struct Position
{
    public Position(int line, int column, string source)
    {
        Line = line;
        Column = column;
        Source = source;
    }

    public int Line { get; set; }
    public int Column { get; set; }
    public string Source { get; }
}

class Error : Exception
{
    Position? _position;

    public Error(string message, Position? position) : base(message)
    {
        _position = position;
    }

    public void Exit()
    {
        if (_position != null)
            Console.Error.WriteLine($"Error: {_position.Value.Line}:{_position.Value.Column}:{_position.Value.Source}: {Message}");
        else
            Console.Error.WriteLine($"Error: {Message}");
        Environment.Exit(1);
    }
}