using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Lsl.Native;

namespace Bonsai.Lsl
{
    /// <summary>
    /// Represents an operator that records the local LSL timestamp for each element
    /// produced by an observable sequence.
    /// </summary>
    [Combinator]
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Records the local LSL timestamp for each element produced by the sequence.")]
    public class Timestamp
    {
        /// <summary>
        /// Records the local LSL timestamp for each element produced by an
        /// observable sequence.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The source sequence to timestamp elements for.</param>
        /// <returns>
        /// An observable sequence containing recorded LSL timestamp information
        /// for each element in the <paramref name="source"/> sequence.
        /// </returns>
        public IObservable<Timestamped<TSource>> Process<TSource>(IObservable<TSource> source)
        {
            return source.Select(value => Timestamped.Create(value, LSL.local_clock()));
        }
    }
}
