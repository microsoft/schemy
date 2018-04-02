// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Schemy
{
    using System.Collections.Generic;

    /// <summary>
    /// Tracks the state of an interpreter or a procedure. It supports lexical scoping.
    /// </summary>
    public class Environment
    {
        private readonly IDictionary<Symbol, object> store;

        /// <summary>
        /// The enclosing environment. For top level env, this is null.
        /// </summary>
        private readonly Environment outer;

        public Environment(IDictionary<Symbol, object> env, Environment outer)
        {
            this.store = env;
            this.outer = outer;
        }

        public static Environment CreateEmpty()
        {
            return new Environment(new Dictionary<Symbol, object>(), null);
        }

        public static Environment FromVariablesAndValues(Union<Symbol, List<Symbol>> parameters, List<object> values, Environment outer)
        {
            return parameters.Use(
                @params => new Environment(new Dictionary<Symbol, object>() { { @params, values } }, outer),
                @params =>
                {
                    if (values.Count != @params.Count)
                    {
                        throw new SyntaxError(string.Format("Unexpected number of arguments. Expecting {0}, Got {1}.", @params.Count, values.Count));
                    }

                    var dict = new Dictionary<Symbol, object>();
                    for (int i = 0; i < values.Count; i++)
                    {
                        dict[@params[i]] = values[i];
                    }

                    return new Environment(dict, outer);
                });
        }

        /// <summary>
        /// Attempts to get the value of the symbol. If it's not found in current env, recursively try the enclosing env.
        /// </summary>
        /// <param name="val">The value of the symbol to find</param>
        /// <returns>if the symbol's value could be found</returns>
        public bool TryGetValue(Symbol sym, out object val)
        {
            Environment env = this.TryFindContainingEnv(sym);
            if (env != null)
            {
                val = env.store[sym];
                return true;
            }
            else
            {
                val = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to find the env that actually defines the symbol
        /// </summary>
        /// <param name="sym">The symbol to find</param>
        /// <returns>the env that defines the symbol</returns>
        public Environment TryFindContainingEnv(Symbol sym)
        {
            object val;
            if (this.store.TryGetValue(sym, out val)) return this;
            if (this.outer != null) return this.outer.TryFindContainingEnv(sym);
            return null;
        }

        public object this[Symbol sym]
        {
            get
            {
                object val;
                if (this.TryGetValue(sym, out val))
                {
                    return val;
                }
                else
                {
                    throw new KeyNotFoundException(string.Format("Symbol not defined: {0}", sym));
                }
            }

            set
            {
                this.store[sym] = value;
            }
        }
    }
}

