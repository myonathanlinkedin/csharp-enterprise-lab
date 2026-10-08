using System.Collections.Generic;

using System;
using GleamToErlang;

class Program
{
    static void Main()
    {
        RunTests();
        Console.WriteLine("All tests passed.");
    }

    static void RunTests()
    {
        TestParser();
        TestTranslator();
    }

    static void TestParser()
    {
        string source = @"
module my_module
def add(x: Int, y: Int) = x + y
def is_even(n: Int) = n rem 2 == 0
";
        var module = Parser.Parse(source);
        Assert(module.Name == "my_module", "Module name parsed incorrectly.");
        Assert(module.Functions.Count == 2, "Function count mismatch.");

        var addFn = module.Functions[0];
        Assert(addFn.Name == "add", "Function name mismatch.");
        Assert(addFn.Params.Count == 2, "Parameter count mismatch.");
        Assert(addFn.Params[0].Name == "x", "Parameter name mismatch.");
        Assert(addFn.Params[0].Type == GleamType.Int, "Parameter type mismatch.");
        Assert(addFn.Body == "x + y", "Function body mismatch.");
    }

    static void TestTranslator()
    {
        var module = new GleamModule("test_mod",
            new List<GleamFunction>
            {
                new GleamFunction("inc", new List<Parameter>{ new Parameter("n", GleamType.Int) }, "n + 1"),
                new GleamFunction("double", new List<Parameter>{ new Parameter("x", GleamType.Int) }, "x * 2")
            });

        string erl = Translator.Translate(module);
        string expected = @"-module(test_mod).
-export([
    inc/1,
    double/1
]).

inc(n) -> n + 1.
double(x) -> x * 2.
";
        Assert(erl.Trim() == expected.Trim(), "Translation output mismatch.");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Assertion failed: {message}");
    }
}
