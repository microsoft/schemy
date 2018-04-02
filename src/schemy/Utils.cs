// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Schemy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Utils
    {
        /// <summary>
        /// Checks the arity of input arguments of a procedure
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="acceptableArities">The acceptable arity.</param>
        /// <exception cref="SyntaxError">thrown when that number of args doesn't match the expected arity.</exception>
        public static void CheckArity(List<object> args, params int[] acceptableArities)
        {
            if (!acceptableArities.Contains(args.Count))
            {
                throw new SyntaxError(string.Format("Arity mismatch. Expecting {0}, Got {1}", string.Join(" or ", acceptableArities), args.Count));
            }
        }

        /// <summary>
        /// Throws <see cref="SyntaxError"/> if the syntax check is not successful, and prints the expression for diagnostics.
        /// </summary>
        /// <param name="expr">The expr that's being checked</param>
        /// <param name="success">if the syntax check was successful</param>
        /// <param name="msg">The error message</param>
        /// <exception cref="SyntaxError">thrown when the syntax check was failed.</exception>
        public static void CheckSyntax(object expr, bool success, string msg = null)
        {
            msg = msg ?? "Syntax error";
            if (!success)
            {
                throw new SyntaxError(string.Format("{0}: {1}", msg, Utils.PrintExpr(expr)));
            }
        }

        /// <summary>
        /// Converts the type of the input to the desired type
        /// </summary>
        /// <typeparam name="T">desired target type</typeparam>
        /// <param name="val">The input value.</param>
        /// <returns>the object of the target type</returns>
        /// <exception cref="InvalidOperationException">thrown when the conversion is not possible</exception>
        /// <remarks>
        /// This is needed because the regular casting can't handle some implicit convert when going through boxing/unboxing, e.g., int to object to double.
        /// </remarks>
        public static T ConvertType<T>(object val)
        {
            if (val is T) return (T)val;

            // object x = 2;
            // double y = (double)x; // <-- this would fail.
            try
            {
                return (T)System.Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                throw new InvalidOperationException(string.Format("Cannot convert {0} to type {1}", Utils.PrintExpr(val), typeof(T).Name));
            }
        }

        public static string PrintExpr(object x)
        {
            if (x is bool)
            {
                return (bool)x ? "#t" : "#f";
            }
            else if (x is Symbol) return ((Symbol)x).AsString;
            else if (x is string) return string.Format(@"""{0}""", x);
            else if (x is List<object>) return string.Format("({0})", string.Join(" ", ((List<object>)x).Select(a => PrintExpr(a))));
            else if (x == null) return string.Empty;
            else return x.ToString();
        }

        /// <summary>
        /// Converts a binary operator (function) to the variadic version.
        /// </summary>
        /// <remarks>
        /// Given a summing function `sum(x, y) => result`. It creates a variadic version: `sum(x, y, ...) => result`.
        /// </remarks>
        public static Func<List<object>, object> MakeVariadic(Func<object, object, object> func)
        {
            return args => args.Aggregate(func);
        }
    }
}