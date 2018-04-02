// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

﻿namespace test
{
    using System;
    using System.IO;
    using System.Reflection;
    using Schemy;

    class Program
    {
        static void Main(string[] args)
        {
            var interpreter = new Interpreter(fsAccessor: new ReadOnlyFileSystemAccessor());
            using (var reader = new StreamReader(File.OpenRead(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "tests.ss"))))
            {
                var result = interpreter.Evaluate(reader);
                if (result.Error != null)
                {
                    throw new InvalidOperationException(string.Format("Test Error: {0}", result.Error));
                }
            }

            Console.WriteLine("Tests were successful");
        }
    }
}
