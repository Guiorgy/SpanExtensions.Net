using System;
using SpanExtensions.SourceGenerators;

namespace SpanExtensions.Enumerators
{

    /// <summary>
    /// Supports iteration over a <see cref="Span{T}"/> by splitting it at specified delimiters of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerated <see cref="Span{T}"/>.</typeparam>
    [GenerateCopy(RegexReplaces = new[] { "(?<!ReadOnly)Span", "ReadOnlySpan" })]
    public ref struct SpanSplitAnyEnumerator<T> where T : IEquatable<T>
    {
        Span<T> Span;
        readonly ReadOnlySpan<T> Delimiters;
        const int DelimiterLength = 1;
        bool EnumerationDone;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public Span<T> Current { get; internal set; }

        /// <summary>
        /// Constructs a <see cref="SpanSplitAnyEnumerator{T}"/> from a span and a delimiter. <strong>Only consume this class through <see cref="SpanExtensions.SplitAny{T}(Span{T}, ReadOnlySpan{T})"/></strong>.
        /// </summary>
        /// <param name="source">The <see cref="Span{T}"/> to be split.</param>
        /// <param name="delimiters">A <see cref="ReadOnlySpan{T}"/> with the instances of <typeparamref name="T"/> that delimit the various sub-Spans in <paramref name="source"/>.</param>
        public SpanSplitAnyEnumerator(Span<T> source, ReadOnlySpan<T> delimiters)
        {
            Span = source;
            Delimiters = delimiters;
            EnumerationDone = false;
            Current = default;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        public readonly SpanSplitAnyEnumerator<T> GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            if(EnumerationDone)
            {
                return false;
            }

            int delimiterIndex = Span.IndexOfAny(Delimiters);

            if(delimiterIndex == -1)
            {
                EnumerationDone = true;

                Current = Span;

                return true;
            }

            Current = Span[..delimiterIndex];
            Span = Span[(delimiterIndex + DelimiterLength)..];

            return true;
        }
    }
}