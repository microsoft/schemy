// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Schemy
{
    public static class Program
    {
        /// <summary>
        /// Initializes the interpreter with a init script if present.
        /// </summary>
        static void Initialize(Interpreter interpreter)
        {
            string initFile = Path.Combine(Path.GetDirectoryName(typeof(Interpreter).Assembly.Location), ".init.ss");
            if (File.Exists(initFile))
            {
                using (var reader = new StreamReader(initFile))
                {
                    var res = interpreter.Evaluate(reader);
                    if (res.Error != null)
                    {
                        Console.WriteLine(string.Format("Error loading {0}: {1}{2}",
                            initFile,
                            System.Environment.NewLine,
                            res.Error));
                    }
                    else
                    {
                        Console.WriteLine("Loaded init file: " + initFile);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                // evaluate input file's content
                var file = args[0];
                var interpreter = new Interpreter();
                Initialize(interpreter);

                using (TextReader reader = new StreamReader(file))
                {
                    object res = interpreter.Evaluate(reader);
                    Console.WriteLine(Utils.PrintExpr(res));
                }
            }
            else
            {
                // starts the REPL
                var interpreter = new Interpreter();
                Initialize(interpreter);
                var headers = new[]
                {
                    "-----------------------------------------------",
                    "| Schemy - Scheme as a Configuration Language |",
                    "| Press Ctrl-C to exit                        |",
                    "-----------------------------------------------",
                };

                interpreter.REPL(Console.In, Console.Out, "Schemy> ", headers);
            }
        }
    }
}
