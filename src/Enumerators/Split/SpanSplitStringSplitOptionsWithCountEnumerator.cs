﻿using System;
using SpanExtensions.SourceGenerators;

namespace SpanExtensions.Enumerators
{
    /// <summary>
    /// Supports iteration over a <see cref="Span{Char}"/> by splitting it at a specified delimiter and based on specified <see cref="StringSplitOptions"/>  with an upper limit of splits performed.
    /// </summary>
    [GenerateCopy(RegexReplaces = new[] { "(?<!ReadOnly)Span", "ReadOnlySpan" })]
    public ref struct SpanSplitStringSplitOptionsWithCountEnumerator
    {
        Span<char> Span;
        readonly char Delimiter;
        const int DelimiterLength = 1;
        readonly bool TrimEntries;
        readonly bool RemoveEmptyEntries;
        readonly CountExceedingBehaviour CountExceedingBehaviour;
        int CurrentCount;
        bool EnumerationDone;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public Span<char> Current { get; internal set; }

        /// <summary>
        /// Constructs a <see cref="SpanSplitStringSplitOptionsWithCountEnumerator"/> from a span and a delimiter. <strong>Only consume this class through <see cref="SpanExtensions.Split(Span{char}, char, int, StringSplitOptions, CountExceedingBehaviour)"/></strong>.
        /// </summary>
        /// <param name="source">The <see cref="Span{Char}"/> to be split.</param>
        /// <param name="delimiter">A <see cref="char"/> that delimits the various sub-Spans in <paramref name="source"/>.</param>
        /// <param name="count">The maximum number of sub-Spans to split into.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim results and include empty results.</param>
        /// <param name="countExceedingBehaviour">The handling of the instances more than count.</param>
        public SpanSplitStringSplitOptionsWithCountEnumerator(Span<char> source, char delimiter, int count, StringSplitOptions options, CountExceedingBehaviour countExceedingBehaviour = CountExceedingBehaviour.AppendRemainingElements)
        {
            Span = source;
            Delimiter = delimiter;
            CurrentCount = count.ThrowIfNegative();
            TrimEntries = options.ThrowIfInvalid().IsTrimEntriesSet();
            RemoveEmptyEntries = options.IsRemoveEmptyEntriesSet();
            CountExceedingBehaviour = countExceedingBehaviour.ThrowIfInvalid();
            EnumerationDone = count == 0;
            Current = default;

            if(count == 1) // special case
            {
                CurrentCount = 0;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        public readonly SpanSplitStringSplitOptionsWithCountEnumerator GetEnumerator()
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

                if(delimiterIndex == -1 || CurrentCount <= 1)
                {
                    EnumerationDone = true;

                    if(CurrentCount != 0 && delimiterIndex != -1 && RemoveEmptyEntries) // skip all empty (after trimming if necessary) entries from the left
                    {
                        do
                        {
                            Span<char> beforeDelimiter = Span[..delimiterIndex];

                            if(TrimEntries ? beforeDelimiter.IsWhiteSpace() : beforeDelimiter.IsEmpty)
                            {
                                Span = Span[(delimiterIndex + DelimiterLength)..];
                                delimiterIndex = Span.IndexOf(Delimiter);

                                continue;
                            }

                            if(CountExceedingBehaviour.IsCutRemainingElements())
                            {
                                Span = beforeDelimiter;
                            }
                            break;
                        }
                        while(delimiterIndex != -1);

                        Current = Span;
                    }
                    else
                    {
                        Current = delimiterIndex == -1 || CountExceedingBehaviour.IsAppendRemainingElements() ? Span : Span[..delimiterIndex];
                    }

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

                CurrentCount--;
                return true;
            }
        }
    }
}