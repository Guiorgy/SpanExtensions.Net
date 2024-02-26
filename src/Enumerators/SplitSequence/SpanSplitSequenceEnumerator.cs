﻿using System;
using SpanExtensions.SourceGenerators;

namespace SpanExtensions.Enumerators
{
    /// <summary>
    /// Supports iteration over a <see cref="Span{T}"/> by splitting it at specified delimiters and based on specified <see cref="StringSplitOptions"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerated <see cref="Span{T}"/>.</typeparam>
    [GenerateCopy(RegexReplaces = new[] { "(?<!ReadOnly)Span", "ReadOnlySpan" })]
    public ref struct SpanSplitSequenceEnumerator<T> where T : IEquatable<T>
    {
        Span<T> Span;
        readonly ReadOnlySpan<T> Delimiter;
        readonly int DelimiterLength;
        readonly bool DelimiterIsEmpty;
        bool EnumerationDone;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public Span<T> Current { get; internal set; }

        /// <summary>
        /// Constructs a <see cref="SpanSplitSequenceEnumerator{T}"/> from a span and a delimiter. <strong>Only consume this class through <see cref="SpanExtensions.Split{T}(Span{T}, ReadOnlySpan{T})"/></strong>.
        /// </summary>
        /// <param name="source">The <see cref="Span{T}"/> to be split.</param>
        /// <param name="delimiter">An instance of <see cref="ReadOnlySpan{T}"/> that delimits the various sub-Spans in <paramref name="source"/>.</param>
        public SpanSplitSequenceEnumerator(Span<T> source, ReadOnlySpan<T> delimiter)
        {
            Span = source;
            Delimiter = delimiter;
            DelimiterLength = Delimiter.Length;
            DelimiterIsEmpty = Delimiter.IsEmpty;
            EnumerationDone = false;
            Current = default;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        public readonly SpanSplitSequenceEnumerator<T> GetEnumerator()
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

            int delimiterIndex = Span.IndexOf(Delimiter);

            if(delimiterIndex == -1 || DelimiterIsEmpty)
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