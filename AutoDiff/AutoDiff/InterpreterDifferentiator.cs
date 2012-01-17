﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Collections.ObjectModel;

using CompilerVisitor = AutoDiff.Compiled.CompilerVisitor;

namespace AutoDiff
{
    /// <summary>
    /// Compiles the terms tree to a more efficient form for differentiation.
    /// </summary>
    internal partial class InterpreterDifferentiator : ICompiledTerm
    {
        private readonly Compiled.TapeElement[] tape;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterDifferentiator"/> class.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="variables">The variables.</param>
        public InterpreterDifferentiator(Term function, Variable[] variables, Compiled.Compiler compiler)
        {
            Contract.Requires(function != null);
            Contract.Requires(variables != null);
            Contract.Requires(Contract.ForAll(variables, variable => variable != null));
            Contract.Ensures(Dimension == variables.Length);

            tape = compiler.Compile(function, variables);

            Dimension = variables.Length;
            Variables = Array.AsReadOnly(variables);
        }

        public int Dimension { get; private set; }

        public double Evaluate(double[] arg)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg.Length == Dimension);
            EvaluateTape(arg);
            return tape.Last().Value;
        }

        public Tuple<double[], double> Differentiate(double[] arg)
        {
            Contract.Requires(arg != null);
            Contract.Requires(arg.Length == Dimension);

            EvaluateTape(arg);
            DifferetiateTape();

            var gradient = tape.Take(Dimension).Select(elem => elem.Derivative).ToArray();
            var value = tape.Last().Value;

            return Tuple.Create(gradient, value);
        }

        private void DifferetiateTape()
        {
            tape.Last().Derivative = 1; // derivative of the last variable with respect to itself is 1.
            var diffVisitor = new DiffVisitor(tape);
            for (int i = tape.Length - 2; i >= 0; --i)
            {
                tape[i].Derivative = 0;
                for (int j = 0; j < tape[i].InputOf.Length; ++j)
                {
                    var connection = tape[i].InputOf[j];
                    Debug.Assert(connection.IndexOnTape > i);

                    var inputElement = tape[connection.IndexOnTape];
                    diffVisitor.ArgumentIndex = connection.ArgumentIndex;
                    inputElement.Accept(diffVisitor);

                    tape[i].Derivative += diffVisitor.LocalDerivative;
                }
            }
        }

        private void EvaluateTape(double[] arg)
        {
            for(int i = 0; i < Dimension; ++i)
                tape[i].Value = arg[i];
            var evalVisitor = new EvalVisitor(tape);
            for (int i = Dimension; i < tape.Length; ++i )
                tape[i].Accept(evalVisitor);
        }

        private double ValueOf(int index)
        {
            return tape[index].Value;
        }

        public ReadOnlyCollection<Variable> Variables { get; private set; }
    }
}
