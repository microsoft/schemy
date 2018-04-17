// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Schemy
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Reflection;

    public class Interpreter
    {
        private readonly Environment environment;
        private readonly Dictionary<Symbol, Procedure> macroTable;
        private readonly IFileSystemAccessor fsAccessor;

        public delegate IDictionary<Symbol, object>  CreateSymbolTableDelegate(Interpreter interpreter);

        /// <summary>
        /// Initializes a new instance of the <see cref="Interpreter"/> class.
        /// </summary>
        /// <param name="environmentInitializers">Array of environment initializers</param>
        /// <param name="fsAccessor">The file system accessor</param>
        public Interpreter(IEnumerable<CreateSymbolTableDelegate> environmentInitializers = null, IFileSystemAccessor fsAccessor = null)
        {
            this.fsAccessor = fsAccessor;
            if (this.fsAccessor == null)
            {
                this.fsAccessor = new DisabledFileSystemAccessor();
            }

            // populate an empty environment for the initializer to potentially work with
            this.environment = Environment.CreateEmpty();
            this.macroTable = new Dictionary<Symbol, Procedure>();

            environmentInitializers = environmentInitializers ?? new List<CreateSymbolTableDelegate>();
            environmentInitializers = new CreateSymbolTableDelegate[] { Builtins.CreateBuiltins }.Concat(environmentInitializers);

            foreach (CreateSymbolTableDelegate initializer in environmentInitializers)
            {
                this.environment = new Environment(initializer(this), this.environment);
            }

            foreach (var iniReader in GetInitializeFiles())
            {
                this.Evaluate(iniReader);
            }
        }

        private IEnumerable<TextReader> GetInitializeFiles()
        {
            using (Stream stream = typeof(Interpreter).Assembly.GetManifestResourceStream("init.ss"))
            using (StreamReader reader = new StreamReader(stream))
            {
                yield return reader;
            }

            string initFile = Path.Combine(Path.GetDirectoryName(typeof(Interpreter).Assembly.Location), ".init.ss");
            if (File.Exists(initFile))
            {
                using (var reader = new StreamReader(initFile))
                {
                    yield return reader;
                }
            }
        }

        public IFileSystemAccessor FileSystemAccessor { get { return this.fsAccessor; } }

        public Environment Environment { get { return this.environment; } }

        /// <summary>
        /// Evaluate script from a input reader
        /// </summary>
        /// <param name="input">the input source</param>
        /// <returns>the value of the last expression</returns>
        public EvaluationResult Evaluate(TextReader input)
        {
            InPort port = new InPort(input);
            object res = null;
            while (true)
            {
                try
                {
                    var expr = Expand(Read(port), environment, macroTable, true);
                    if (Symbol.EOF.Equals(expr))
                    {
                        return new EvaluationResult(null, res);
                    }
                    else
                    {
                        res = EvaluateExpression(expr, environment);
                    }
                }
                catch (Exception e)
                {
                    return new EvaluationResult(e, null);
                }
            }
        }

        /// <summary>
        /// Starts the Read-Eval-Print loop
        /// </summary>
        /// <param name="input">the input source</param>
        /// <param name="output">the output target</param>
        /// <param name="prompt">a string prompt to be printed before each evaluation</param>
        /// <param name="headers">a head text to be printed at the beginning of the REPL</param>
        public void REPL(TextReader input, TextWriter output, string prompt = null, string[] headers = null)
        {
            InPort port = new InPort(input);

            if (headers != null)
            {
                foreach (var line in headers)
                {
                    output.WriteLine(line);
                }
            }

            object res = null;
            while (true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(prompt) && output != null) output.Write(prompt);
                    var expr = Expand(Read(port), environment, macroTable, true);
                    if (Symbol.EOF.Equals(expr))
                    {
                        return;
                    }
                    else
                    {
                        res = EvaluateExpression(expr, environment);
                        if (output != null) output.WriteLine(Utils.PrintExpr(res));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Defines a global symbol
        /// </summary>
        /// <param name="sym">the symbol</param>
        /// <param name="val">the associated value</param>
        public void DefineGlobal(Symbol sym, object val)
        {
            this.environment[sym] = val;
        }

        /// <summary>
        /// Reads an S-expression from the input source
        /// </summary>
        public static object Read(InPort port)
        {
            Func<object, object> readAhead = null;
            readAhead = token =>
            {
                Symbol quote;
                if (object.Equals(token, Symbol.EOF))
                {
                    throw new SyntaxError("unexpected EOF");
                }
                else if (token is string)
                {
                    string tokenStr = (string)token;
                    if (tokenStr == "(")
                    {
                        var L = new List<object>();
                        while (true)
                        {
                            token = port.NextToken();
                            if (token is string && (string)token == ")")
                            {
                                return L;
                            }
                            else
                            {
                                L.Add(readAhead(token));
                            }
                        }
                    }
                    else if (tokenStr == ")")
                    {
                        throw new SyntaxError("unexpected )");
                    }
                    else if (Symbol.QuotesMap.TryGetValue(tokenStr, out quote))
                    {
                        object quoted = Read(port);
                        return new List<object> { quote, quoted };
                    }
                    else
                    {
                        return ParseAtom(tokenStr);
                    }
                }
                else
                {
                    throw new SyntaxError("unexpected token: " + token);
                }
            };

            var token1 = port.NextToken();
            return Symbol.EOF.Equals(token1) ? Symbol.EOF : readAhead(token1);
        }

        /// <summary>
        /// Validates and expands the input s-expression
        /// </summary>
        /// <param name="expression">expression to expand</param>
        /// <param name="env">env used to evaluate the macro procedures</param>
        /// <param name="macroTable">the macro definition table</param>
        /// <param name="isTopLevel">whether the current expansion is at the top level</param>
        /// <returns>the s-expression after validation and expansion</returns>
        public static object Expand(object expression, Environment env, Dictionary<Symbol, Procedure> macroTable, bool isTopLevel = true)
        {
            Procedure procedure = null;
            Func<object, bool, object> expand = null;
            expand = (x, topLevel) =>
            {
                if (!(x is List<object>))
                {
                    return x;
                }

                List<object> xs = (List<object>)x;
                Utils.CheckSyntax(xs, xs.Count > 0);

                if (Symbol.QUOTE.Equals(xs[0]))
                {
                    Utils.CheckSyntax(xs, xs.Count == 2);
                    return xs;
                }
                else if (Symbol.IF.Equals(xs[0]))
                {
                    if (xs.Count == 3)
                    {
                        xs.Add(None.Instance);
                    }

                    Utils.CheckSyntax(xs, xs.Count == 4);
                    return xs.Select(expr => expand(expr, false)).ToList();
                }
                else if (Symbol.SET.Equals(xs[0]))
                {
                    Utils.CheckSyntax(xs, xs.Count == 3);
                    Utils.CheckSyntax(xs, xs[1] is Symbol, "can only set! a symbol");
                    return new List<object> { Symbol.SET, xs[1], expand(xs[2], false) };
                }
                else if (Symbol.DEFINE.Equals(xs[0]) || Symbol.DEFINE_MACRO.Equals(xs[0]))
                {
                    Utils.CheckSyntax(xs, xs.Count >= 3);
                    Symbol def = (Symbol)xs[0];
                    object v = xs[1]; // sym or (sym+)
                    List<object> body = xs.Skip(2).ToList(); // expr or expr+
                    if (v is List<object>) // defining function: ([define|define-macro] (f arg ...) body)
                    {
                        var args = (List<object>)v;
                        Utils.CheckSyntax(xs, args.Count > 0);
                        var f = args[0];
                        var @params = args.Skip(1).ToList();
                        return expand(new List<object> { def, f, Enumerable.Concat(new object[] { Symbol.LAMBDA, @params }, body).ToList() }, false);
                    }
                    else // defining variable: ([define|define-macro] id expr)
                    {
                        Utils.CheckSyntax(xs, xs.Count == 3);
                        Utils.CheckSyntax(xs, v is Symbol);
                        var expr = expand(xs[2], false);
                        if (Symbol.DEFINE_MACRO.Equals(def))
                        {
                            Utils.CheckSyntax(xs, topLevel, "define-macro is only allowed at the top level");
                            var proc = EvaluateExpression(expr, env);
                            Utils.CheckSyntax(xs, proc is Procedure, "macro must be a procedure");
                            macroTable[(Symbol)v] = (Procedure)proc;
                            return None.Instance;
                        }
                        else
                        {
                            // `define v expr`
                            return new List<object> { Symbol.DEFINE, v, expr /* after expansion */ };
                        }
                    }
                }
                else if (Symbol.BEGIN.Equals(xs[0]))
                {
                    if (xs.Count == 1) return None.Instance; // (begin) => None

                    // use the same topLevel so that `define-macro` is also allowed in a top-level `begin`.
                    return xs.Select(expr => expand(expr, topLevel)).ToList();
                }
                else if (Symbol.LAMBDA.Equals(xs[0]))
                {
                    Utils.CheckSyntax(xs, xs.Count >= 3);
                    var vars = xs[1];
                    Utils.CheckSyntax(xs, vars is Symbol || (vars is List<object> && ((List<object>)vars).All(v => v is Symbol)), "illigal lambda argument");

                    object body;
                    if (xs.Count == 3)
                    {
                        // (lambda (...) expr)
                        body = xs[2];
                    }
                    else
                    {
                        // (lambda (...) expr+
                        body = Enumerable.Concat(new[] { Symbol.BEGIN }, xs.Skip(2)).ToList();
                    }

                    return new List<object> { Symbol.LAMBDA, vars, expand(body, false) };
                }
                else if (Symbol.QUASIQUOTE.Equals(xs[0]))
                {
                    Utils.CheckSyntax(xs, xs.Count == 2);
                    return ExpandQuasiquote(xs[1]);
                }
                else if (xs[0] is Symbol && macroTable.TryGetValue((Symbol)xs[0], out procedure))
                {
                    return expand(procedure.Call(xs.Skip(1).ToList()), topLevel);
                }
                else
                {
                    return xs.Select(p => expand(p, false)).ToList();
                }
            };
            return expand(expression, isTopLevel);
        }

        /// <summary>
        /// Evaluates an s-expression
        /// </summary>
        /// <param name="expr">expression to be evaluated</param>
        /// <param name="env">the environment in which the expression is evaluated</param>
        /// <returns>the result of the evaluation</returns>
        public static object EvaluateExpression(object expr, Environment env)
        {
            while (true)
            {
                if (expr is Symbol)
                {
                    return env[(Symbol)expr];
                }
                else if (!(expr is List<object>))
                {
                    return expr; // is a constant literal
                }
                else
                {
                    List<object> exprList = (List<object>)expr;
                    if (Symbol.QUOTE.Equals(exprList[0]))
                    {
                        return exprList[1];
                    }
                    else if (Symbol.IF.Equals(exprList[0]))
                    {
                        var test = exprList[1];
                        var conseq = exprList[2];
                        var alt = exprList[3];
                        expr = ConvertToBool(EvaluateExpression(test, env)) ? conseq : alt;
                    }
                    else if (Symbol.DEFINE.Equals(exprList[0]))
                    {
                        var variable = (Symbol)exprList[1];
                        expr = exprList[2];
                        env[variable] = EvaluateExpression(expr, env);
                        return None.Instance; // TODO: what's the return type of define?
                    }
                    else if (Symbol.SET.Equals(exprList[0]))
                    {
                        var sym = (Symbol)exprList[1];
                        var containingEnv = env.TryFindContainingEnv(sym);
                        if (containingEnv == null)
                        {
                            throw new KeyNotFoundException("Symbol not defined: " + sym);
                        }

                        containingEnv[sym] = EvaluateExpression(exprList[2], env);
                        return None.Instance;
                    }
                    else if (Symbol.LAMBDA.Equals(exprList[0]))
                    {
                        // Two lambda forms:
                        // -    (lambda (arg ...) body): each arg is bound to a value
                        // -    (lambda args body): args is bound to the parameter list
                        Union<Symbol, List<Symbol>> parameters;
                        if (exprList[1] is Symbol)
                        {
                            parameters = new Union<Symbol, List<Symbol>>((Symbol)exprList[1]);
                        }
                        else
                        {
                            parameters = new Union<Symbol, List<Symbol>>(((List<object>)exprList[1]).Cast<Symbol>().ToList());
                        }

                        return new Procedure(parameters, exprList[2], env);
                    }
                    else if (Symbol.BEGIN.Equals(exprList[0]))
                    {
                        for (int i = 1; i < exprList.Count - 1 /* don't eval last expr yet */; i++)
                        {
                            EvaluateExpression(exprList[i], env);
                        }

                        expr = exprList[exprList.Count - 1]; // tail call optimization
                    }
                    else
                    {
                        // a procedure call
                        var rawProc = EvaluateExpression(exprList[0], env);
                        if (!(rawProc is ICallable))
                        {
                            throw new InvalidCastException(string.Format("Object is not callable: {0}", rawProc));
                        }

                        var args = exprList.Skip(1).Select(a => EvaluateExpression(a, env)).ToList();
                        if (rawProc is Procedure)
                        {
                            // Tail call optimization - instead of evaluating the procedure here which grows the
                            // stack by calling EvaluateExpression, we update the `expr` and `env` to be the
                            // body and the (params, args), and loop the evaluation from here.
                            var proc = (Procedure)rawProc;
                            expr = proc.Body;
                            env = Environment.FromVariablesAndValues(proc.Parameters, args, proc.Env);
                        }
                        else if (rawProc is NativeProcedure)
                        {
                            return ((NativeProcedure)rawProc).Call(args);
                        }
                        else
                        {
                            throw new InvalidOperationException("unexpected implementation of ICallable: " + rawProc.GetType().Name);
                        }
                    }
                }
            }
        }

        private static bool IsPair(object x)
        {
            return x is List<object> && ((List<object>)x).Count > 0;
        }

        private static object ExpandQuasiquote(object x)
        {
            if (!IsPair(x)) return new List<object> { Symbol.QUOTE, x };
            var xs = (List<object>)x;
            Utils.CheckSyntax(xs, !Symbol.UNQUOTE_SPLICING.Equals(xs[0]), "Cannot splice");
            if (Symbol.UNQUOTE.Equals(xs[0]))
            {
                Utils.CheckSyntax(xs, xs.Count == 2);
                return xs[1];
            }
            else if (IsPair(xs[0]) && Symbol.UNQUOTE_SPLICING.Equals(((List<object>)xs[0])[0]))
            {
                var x0 = (List<object>)xs[0];
                Utils.CheckSyntax(x0, x0.Count == 2);
                return new List<object> { Symbol.APPEND, x0[1], ExpandQuasiquote(xs.Skip(1).ToList()) };
            }
            else
            {
                return new List<object> { Symbol.CONS, ExpandQuasiquote(xs[0]), ExpandQuasiquote(xs.Skip(1).ToList()) };
            }
        }

        private static object ParseAtom(string token)
        {
            int intVal;
            double floatVal;
            if (token == "#t")
            {
                return true;
            }
            else if (token == "#f")
            {
                return false;
            }
            else if (token[0] == '"')
            {
                return token.Substring(1, token.Length - 2);
            }
            else if (int.TryParse(token, out intVal))
            {
                return intVal;
            }
            else if (double.TryParse(token, out floatVal))
            {
                return floatVal;
            }
            else
            {
                return Symbol.FromString(token); // a symbol
            }
        }

        private static bool ConvertToBool(object val)
        {
            if (val is bool) return (bool)val;
            return true;
        }

        public struct EvaluationResult
        {
            private readonly Exception error;
            private readonly object result;

            public EvaluationResult(Exception error, object result) : this()
            {
                this.error = error;
                this.result = result;
            }

            public Exception Error { get { return this.error; } }

            public object Result { get { return this.result; } }
        }

        public class InPort
        {
            private const string tokenizer = @"^\s*(,@|[('`,)]|""(?:[\\].|[^\\""])*""|;.*|[^\s('""`,;)]*)(.*)";

            private TextReader file;
            private string line;

            public InPort(TextReader file)
            {
                this.file = file;
                this.line = string.Empty;
            }

            /// <summary>
            /// Parses and returns the next token. Returns <see cref="Symbol.EOF"/> if there's no more content to read.
            /// </summary>
            public object NextToken()
            {
                while (true)
                {
                    if (this.line == string.Empty)
                    {
                        this.line = this.file.ReadLine();
                    }

                    if (this.line == string.Empty)
                    {
                        continue;
                    }
                    else if (this.line == null)
                    {
                        return Symbol.EOF;
                    }
                    else
                    {
                        var res = Regex.Match(this.line, tokenizer);
                        var token = res.Groups[1].Value;
                        this.line = res.Groups[2].Value;

                        if (string.IsNullOrEmpty(token))
                        {
                            // 1st group is empty. All string falls into 2nd group. This usually means 
                            // an error in the syntax, e.g., incomplete string "foo
                            var tmp = this.line;
                            this.line = string.Empty; // to continue reading next line

                            if (tmp.Trim() != string.Empty)
                            {
                                // this is a syntax error
                                Utils.CheckSyntax(tmp, false, "unexpected syntax");
                            }
                        }

                        if (!string.IsNullOrEmpty(token) && !token.StartsWith(";"))
                        {
                            return token;
                        }
                    }
                }
            }
        }
    }
}

