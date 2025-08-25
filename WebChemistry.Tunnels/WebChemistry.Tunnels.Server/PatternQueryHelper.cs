/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Server
{
    using Microsoft.Scripting;
    using Microsoft.Scripting.Hosting;
    using System;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.MetaQueries;
    using WebChemistry.Queries.Core.Queries;

    /// <summary>
    /// WebChem scripting.
    /// </summary>
    public static class PatternQueryHelper
    {
        static ScriptEngine Engine;

        static PatternQueryHelper()
        {
            var setup = new ScriptRuntimeSetup();
            setup.HostType = typeof(ScriptHost);
            //setup.LanguageSetups.Add(Python.CreateLanguageSetup(null));
            setup.Options["SearchPaths"] = new string[] { string.Empty };

            var runtime = new ScriptRuntime(setup);
            Engine = runtime.GetEngine("Python");
            Engine.Runtime.LoadAssembly(typeof(QueryBuilder).Assembly);
        }

        /// <summary>
        /// Throws if not valid.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="desiredType">If null, can be any.</param>
        /// <returns></returns>
        public static void Validate(string expression, TypeExpression desiredType = null)
        {
            var scope = Engine.CreateScope();
            Engine.Execute("from WebChemistry.Queries.Core.QueryBuilder import *", scope);
            var script = Engine.CreateScriptSourceFromString(expression, Microsoft.Scripting.SourceCodeKind.AutoDetect);
            dynamic executed = script.Execute(scope);

            MetaQuery mq;
            try
            {
                mq = (MetaQuery)executed;
            }
            catch (Exception)
            {
                throw new InvalidCastException(string.Format("Expected 'Query', got '{0}'.", executed.GetType()));
            }

            if (desiredType != null)
            {
                if (!TypeExpression.Unify(desiredType, mq.Type).Success)
                {
                    throw new ArgumentTypeException(string.Format("Got '{0}', expected '{1}'.", mq.Type, desiredType));
                }
            }
        }

        public static QueryMotive CompileMotive(string expression)
        {
            var compiled = Compile(expression) as QueryMotive;
            if (compiled == null) throw new InvalidOperationException("Expected a motive sequence.");
            return compiled;
        }

        public static Query Compile(string expression)
        {
            var scope = Engine.CreateScope();
            Engine.Execute("from WebChemistry.Queries.Core.QueryBuilder import *", scope);
            var script = Engine.CreateScriptSourceFromString(expression, Microsoft.Scripting.SourceCodeKind.AutoDetect);
            dynamic executed = script.Execute(scope);

            MetaQuery mq;
            try
            {
                mq = (MetaQuery)executed;
            }
            catch (Exception)
            {
                throw new InvalidCastException(string.Format("Expected 'Query', got '{0}'.", executed.GetType()));
            }

            return mq.Compile();
        }
    }
}
