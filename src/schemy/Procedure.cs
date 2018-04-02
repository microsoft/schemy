// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Schemy
{
    /// <summary>
    /// Represents a procedure value in Scheme
    /// </summary>
    interface ICallable
    {
        /// <summary>
        /// Invokes this procedure
        /// </summary>
        /// <param name="args">The arguments. These are the `cdr` of the s-expression for the procedure invocation.</param>
        /// <returns>the result of the procedure invocation</returns>
        object Call(List<object> args);
    }

    /// <summary>
    /// A procedure implemented in Scheme
    /// </summary>
    /// <seealso cref="Schemy.ICallable" />
    public class Procedure : ICallable
    {
        private readonly Union<Symbol, List<Symbol>> parameters;
        private readonly object body;
        private readonly Environment env;

        public Procedure(Union<Symbol, List<Symbol>> parameters, object body, Environment env)
        {
            this.parameters = parameters;
            this.body = body;
            this.env = env;
        }

        public object Body
        {
            get { return this.body; }
        }

        public Union<Symbol, List<Symbol>> Parameters
        {
            get { return this.parameters; }
        }

        public Environment Env
        {
            get { return this.env; }
        }

        /// <summary>
        /// Invokes this procedure
        /// </summary>
        /// <remarks>
        /// Implementation note: under normal function invocation scenarios, this method is not used. Instead,
        /// a tail call optimization is used in the interpreter evaluation phase that runs Scheme functions.
        /// 
        /// This method is useful however, in macro expansions, and any other occasions where the tail call optimization
        /// is not (yet) implemented.
        /// 
        /// <see cref="Interpreter.EvaluateExpression(object, Environment)"/>
        /// </remarks>
        public object Call(List<object> args)
        {
            // NOTE: This is not needed for regular function invoke after the tail call optimization.
            // a (non-native) procedure is now optimized into evaluating the body under the environment
            // formed by the (params, args). So the `Call` method will never be used.
            return Interpreter.EvaluateExpression(this.body, Environment.FromVariablesAndValues(this.parameters, args, this.env));
        }

        /// <summary>
        /// Prints the implementation of the function.
        /// </summary>
        public override string ToString()
        {
            var form = new List<object> { Symbol.LAMBDA, this.parameters.Use(sym => (object)sym, syms => syms.Cast<object>().ToList()), this.body };
            return Utils.PrintExpr(form);
        }
    }

    /// <summary>
    /// A procedure implemented in .NET
    /// </summary>
    /// <seealso cref="Schemy.ICallable" />
    public class NativeProcedure : ICallable
    {
        private readonly Func<List<object>, object> func;
        private readonly string name;

        public NativeProcedure(Func<List<object>, object> func, string name = null)
        {
            this.func = func;
            this.name = name;
        }

        public object Call(List<object> args)
        {
            return this.func(args);
        }

        /// <summary>
        /// Convenient function method to create a native procedure and doing arity and type check for inputs. It makes the input function
        /// implementation strongly typed.
        /// </summary>
        /// <see cref="Create{T1, T2}(Func{T1, T2}, string)"/>
        public static NativeProcedure Create<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8> func, string name = null)
        {
            return new NativeProcedure(args =>
            {
                Utils.CheckArity(args, 7);
                return func(
                    Utils.ConvertType<T1>(args[0]),
                    Utils.ConvertType<T2>(args[1]),
                    Utils.ConvertType<T3>(args[2]),
                    Utils.ConvertType<T4>(args[3]),
                    Utils.ConvertType<T5>(args[4]),
                    Utils.ConvertType<T6>(args[5]),
                    Utils.ConvertType<T7>(args[6])
                    );
            }, name);
        }

        /// <summary>
        /// Convenient function method to create a native procedure and doing arity and type check for inputs. It makes the input function
        /// implementation strongly typed.
        /// </summary>
        /// <see cref="Create{T1, T2}(Func{T1, T2}, string)"/>
        public static NativeProcedure Create<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> func, string name = null)
        {
            return new NativeProcedure(args =>
            {
                Utils.CheckArity(args, 4);
                return func(
                    Utils.ConvertType<T1>(args[0]),
                    Utils.ConvertType<T2>(args[1]),
                    Utils.ConvertType<T3>(args[2]),
                    Utils.ConvertType<T4>(args[3]));
            }, name);
        }

        /// <summary>
        /// Convenient function method to create a native procedure and doing arity and type check for inputs. It makes the input function
        /// implementation strongly typed.
        /// </summary>
        /// <see cref="Create{T1, T2}(Func{T1, T2}, string)"/>
        public static NativeProcedure Create<T1, T2, T3, T4>(Func<T1, T2, T3, T4> func, string name = null)
        {
            return new NativeProcedure(args =>
            {
                Utils.CheckArity(args, 3);
                return func(
                    Utils.ConvertType<T1>(args[0]),
                    Utils.ConvertType<T2>(args[1]),
                    Utils.ConvertType<T3>(args[2]));
            }, name);
        }

        /// <summary>
        /// Convenient function method to create a native procedure and doing arity and type check for inputs. It makes the input function
        /// implementation strongly typed.
        /// </summary>
        /// <see cref="Create{T1, T2}(Func{T1, T2}, string)"/>
        public static NativeProcedure Create<T1, T2, T3>(Func<T1, T2, T3> func, string name = null)
        {
            return new NativeProcedure(args =>
            {
                Utils.CheckArity(args, 2);
                return func(Utils.ConvertType<T1>(args[0]), Utils.ConvertType<T2>(args[1]));
            }, name);
        }

        /// <summary>
        /// Convenient function method to create a native procedure and doing arity and type check for inputs. It makes the input function
        /// implementation strongly typed.
        /// </summary>
        /// <typeparam name="T1">The type of the 1st argument</typeparam>
        /// <typeparam name="T2">The type of the 2nd argument</typeparam>
        /// <param name="func">The function implementation</param>
        /// <param name="name">The name of the function</param>
        public static NativeProcedure Create<T1, T2>(Func<T1, T2> func, string name = null)
        {
            return new NativeProcedure(args =>
            {
                Utils.CheckArity(args, 1);
                return func(Utils.ConvertType<T1>(args[0]));
            }, name);
        }

        /// <summary>
        /// Convenient function method to create a native procedure and doing arity and type check for inputs. It makes the input function
        /// implementation strongly typed.
        /// </summary>
        /// <see cref="Create{T1, T2}(Func{T1, T2}, string)"/>
        public static NativeProcedure Create<T1>(Func<T1> func, string name = null)
        {
            return new NativeProcedure(args =>
            {
                Utils.CheckArity(args, 0);
                return func();
            }, name);
        }

        /// <summary>
        /// ToString implementation
        /// </summary>
        /// <returns>the string representation</returns>
        public override string ToString()
        {
            return string.Format("#<NativeProcedure:{0}>", string.IsNullOrEmpty(this.name) ? "noname" : this.name);
        }
    }
}

