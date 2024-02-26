﻿using System;
using SpanExtensions.SourceGenerators;

namespace SpanExtensions.Enumerators
{
    /// <summary>
    /// Supports iteration over a <see cref="Span{Char}"/> by splitting it at a specified delimiter and based on specified <see cref="StringSplitOptions"/>.
    /// </summary>
    [GenerateCopy(RegexReplaces = new[] { "(?<!ReadOnly)Span", "ReadOnlySpan" })]
    public ref struct SpanSplitStringSplitOptionsEnumerator
    {
        Span<char> Span;
        readonly char Delimiter;
        const int DelimiterLength = 1;
        readonly bool TrimEntries;
        readonly bool RemoveEmptyEntries;
        bool EnumerationDone;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public Span<char> Current { get; internal set; }

        /// <summary>
        /// Constructs a <see cref="SpanSplitStringSplitOptionsEnumerator"/> from a span and a delimiter. <strong>Only consume this class through <see cref="SpanExtensions.Split(Span{char}, char, StringSplitOptions)"/></strong>.
        /// </summary>
        /// <param name="source">The <see cref="Span{Char}"/> to be split.</param>
        /// <param name="delimiter">A <see cref="char"/> that delimits the various sub-Spans in <paramref name="source"/>.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim results and include empty results.</param>
        public SpanSplitStringSplitOptionsEnumerator(Span<char> source, char delimiter, StringSplitOptions options)
        {
            Span = source;
            Delimiter = delimiter;
            TrimEntries = options.ThrowIfInvalid().IsTrimEntriesSet();
            RemoveEmptyEntries = options.IsRemoveEmptyEntriesSet();
            EnumerationDone = false;
            Current = default;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        public readonly SpanSplitStringSplitOptionsEnumerator GetEnumerator()
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

            while(true) // if RemoveEmptyEntries options flag is set, repeat until a non-empty span is found, or the end is reached
            {
                int delimiterIndex = Span.IndexOf(Delimiter);

                if(delimiterIndex == -1)
                {
                    EnumerationDone = true;

                    Current = Span;

                    if(TrimEntries)
                    {
                        Current = Current.Trim();
                    }

                    return !(RemoveEmptyEntries && Current.IsEmpty);
                }

                Current = Span[..delimiterIndex];
                Span = Span[(delimiterIndex + DelimiterLength)..];

                if(TrimEntries)
                {
                    Current = Current.Trim();
                }

                if(RemoveEmptyEntries && Current.IsEmpty)
                {
                    continue;
                }

                return true;
            }
        }
    }
}