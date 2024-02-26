﻿using SpanExtensions;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpanExtensions.Testing
{
    public static class TestHelper
    {
        public static readonly Random random = new();

        /// <summary>
        /// Generates a sequence of a specified number of random integers that are in the specified range.
        /// </summary>
        /// <param name="count">The number of integers to generate.</param>
        /// <param name="minValue">The inclusive lower bound of the random numbers returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>A sequence of random integers.</returns>
        public static IEnumerable<int> GenerateRandomIntegers(int count, int minValue, int maxValue)
        {
            for(int i = 0; i < count; i++)
            {
                yield return random.Next(minValue, maxValue);
            }
        }

        /// <summary>
        /// Generates a random string of a specified length. The string is generated from an alphabet of lowercase letters, numbers, and white spaces.
        /// </summary>
        /// <param name="length">The length of the generated string.</param>
        /// <returns>A random string.</returns>
        public static string GenerateRandomString(int length)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789 ";

            StringBuilder builder = new(length);
            for(int i = 0; i < length; i++)
            {
                int alphabetIndex = random.Next(alphabet.Length);
                builder.Append(alphabet[alphabetIndex]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Generates a message to be displayed when an assertion fails containing the necessary information to reproduce the failure.
        /// </summary>
        /// <typeparam name="T">The element type of the source span.</typeparam>
        /// <param name="source">The elements inside the source span.</param>
        /// <param name="expected">The expected results.</param>
        /// <param name="actual">The actual results.</param>
        /// <param name="method">The method that failed an assertion.</param>
        /// <param name="args">The argument names and values of the method that failed an asserion.</param>
        /// <returns>The message to be displayed when assertion fails.</returns>
        public static string GenerateAssertionMessage<T>(IEnumerable<T> source, IEnumerable<IEnumerable<T>> expected, IEnumerable<IEnumerable<T>> actual, string method, params (string argName, object? argValue)[] args)
        {
            static void AppendKeyValues(StringBuilder builder, string key, params (string subkey, object? value)[] keyValues)
            {
                static IEnumerable<object?> Enumerate(IEnumerable enumerable)
                {
                    List<object?> list = [];

                    IEnumerator enumerator = enumerable.GetEnumerator();
                    while(enumerator.MoveNext())
                    {
                        list.Add(enumerator.Current);
                    }

                    return list;
                }

                static string ToString(object? obj)
                {
                    return obj switch
                    {
                        null => "null",
                        IEnumerable enumerable => '[' + string.Join(", ", Enumerate(enumerable).Select(x => ToString(x))) + ']',
                        _ => obj.ToString()
                    } ?? "null";
                }

                builder.Append(key).AppendLine(":");
                foreach((string subkey, object? value) in keyValues)
                {
                    builder.Append('\t').Append(subkey).Append(": ").AppendLine(ToString(value));
                }
            }

            static void AppendNestedEnumerable(StringBuilder builder, string key, IEnumerable<IEnumerable<T>> nestedEnumerable)
            {
                builder.Append(key).AppendLine(": [");
                bool emptyCollection = true;
                foreach(IEnumerable<T> enumerable in nestedEnumerable)
                {
                    emptyCollection = false;
                    builder.Append("\t[").AppendJoin(", ", enumerable).AppendLine("],");
                }
                builder.Length -= Environment.NewLine.Length + (!emptyCollection ? 1 : 0); // remove the last endline and comma
                builder.AppendLine().AppendLine("]");
            }

            StringBuilder builder = new();

            builder.AppendLine();
            builder.Append("Runtime Version: ").AppendLine(Environment.Version.ToString());
            builder.Append("Source: [").AppendJoin(", ", source).AppendLine("]");
            builder.Append("Method: ").AppendLine(method);
            AppendKeyValues(builder, "Arguments", args);
            AppendNestedEnumerable(builder, "Expected", expected);
            AppendNestedEnumerable(builder, "Actual", actual);

            return builder.ToString();
        }

        /// <summary>
        /// Tests whether the specified nested collections are equal.
        /// </summary>
        /// <typeparam name="T">The element type in the compared collections.</typeparam>
        /// <param name="first">The first collection to compare.</param>
        /// <param name="second">The second collection to compare.</param>
        /// <returns><see langword="true"/> if both collections are equal; otherwise <see langword="false"/>.</returns>
        public static bool SequencesEqual<T>(IEnumerable<IEnumerable<T>> first, IEnumerable<IEnumerable<T>> second) where T : IEquatable<T>
        {
            IEnumerator<IEnumerable<T>> firstEnumerator = first.GetEnumerator();
            IEnumerator<IEnumerable<T>> secondEnumerator = second.GetEnumerator();

            while(firstEnumerator.MoveNext())
            {
                if(!secondEnumerator.MoveNext()) // first is longer
                {
                    return false;
                }

                IEnumerator<T> firstSubEnumerator = firstEnumerator.Current.GetEnumerator();
                IEnumerator<T> secondSubEnumerator = secondEnumerator.Current.GetEnumerator();

                while(firstSubEnumerator.MoveNext())
                {
                    if(!secondSubEnumerator.MoveNext()) // first is larger
                    {
                        return false;
                    }

                    if(!firstSubEnumerator.Current.Equals(secondSubEnumerator.Current))
                    {
                        return false;
                    }
                }

                if(secondSubEnumerator.MoveNext()) // second is larger
                {
                    return false;
                }
            }

            if(secondEnumerator.MoveNext()) // second is larger
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tests whether calling the specified method on the specified source with the specified arguments results in the expected result.
        /// </summary>
        /// <typeparam name="T">The element type of the source span.</typeparam>
        /// <param name="expected">The expected results.</param>
        /// <param name="actual">The actual results.</param>
        /// <param name="source">The elements inside the source span.</param>
        /// <param name="method">The method that was called.</param>
        /// <param name="args">The argument names and values of the method that was called.</param>
        public static void AssertMethodResults<T>(IEnumerable<IEnumerable<T>> expected, IEnumerable<IEnumerable<T>> actual, IEnumerable<T> source, string method, params (string argName, object? argValue)[] args) where T : IEquatable<T>
        {
            if(!SequencesEqual(expected, actual))
            {
                Assert.Fail(GenerateAssertionMessage(source, expected, actual, method, args));
            }
        }

#if !NET8_0_OR_GREATER
        /// <summary>Counts the number of times the specified <paramref name="value"/> occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            int count = 0;
            foreach(T item in span)
            {
                if(item.Equals(value))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Counts the number of times the specified <paramref name="value"/> occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this Span<T> span, T value) where T : IEquatable<T>
        {
            return Count((ReadOnlySpan<T>)span, value);
        }
#endif

        /// <summary>Counts the number of times any of the specified <paramref name="values"/> occur in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values for which to search.</param>
        /// <returns>The number of times any of the <paramref name="values"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
        {
            int count = 0;
            foreach(T item in span)
            {
                if(values.Contains(item))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Counts the number of times any of the specified <paramref name="values"/> occur in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="values">The values for which to search.</param>
        /// <returns>The number of times any of the <paramref name="values"/> was found in the <paramref name="span"/>.</returns>
        public static int Count<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
        {
            return Count((ReadOnlySpan<T>)span, values);
        }

        /// <summary>
        /// Counts the number of times any of the specified <paramref name="substring"/> occur in the <paramref name="string"/>.
        /// </summary>
        /// <param name="string">The string to search.</param>
        /// <param name="substring">The substring for which to search.</param>
        /// <returns>The number of times any of the <paramref name="substring"/> was found in the <paramref name="string"/>.</returns>
        public static int Count(this string @string, string substring)
        {
#if NET7_0_OR_GREATER
            return Regex.Count(@string, substring);
#else
            return Regex.Matches(@string, substring).Count;
#endif
        }

        /// <summary>Counts the number of times the specified <paramref name="value"/> subsequence occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value subsequence for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> subsequence was found in the <paramref name="span"/>.</returns>
        public static int CountSequence<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value)
        {
            string source = string.Join(",", span.ToArray());
            string substring = string.Join(",", value.ToArray());

            return source.Count(substring);
        }

        /// <summary>Counts the number of times the specified <paramref name="value"/> subsequence occurs in the <paramref name="span"/>.</summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value subsequence for which to search.</param>
        /// <returns>The number of times <paramref name="value"/> subsequence was found in the <paramref name="span"/>.</returns>
        public static int CountSequence<T>(this Span<T> span, ReadOnlySpan<T> value)
        {
            return CountSequence((ReadOnlySpan<T>)span, value);
        }

        /// <summary>
        /// Returns an <see cref="Exception"/> indicating that the specified <see cref="CountExceedingBehaviour"/> value wasn't handled.
        /// </summary>
        /// <param name="countExceedingBehaviour">The <see cref="CountExceedingBehaviour"/> value that wasn't handled.</param>
        public static Exception UnhandledCaseException(CountExceedingBehaviour countExceedingBehaviour)
        {
            return new
#if NET7_0_OR_GREATER
                UnreachableException
#else
                NotImplementedException
#endif
                ($"Unhandled {nameof(CountExceedingBehaviour)} enum value: {countExceedingBehaviour}.");
        }

        /// <summary>
        /// Take up to a specified count of elements from an array.
        /// <remark>Unlike <paramref name="source"/>[..<paramref name="count"/>], this doesn't throw an exception when <paramref name="count"/> is greater than the length of <paramref name="source"/>.</remark>
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="source">The array to cut.</param>
        /// <param name="count">The number of elements to take.</param>
        /// <returns>The cut array.</returns>
        public static T[] UpTo<T>(this T[] source, int count)
        {
            return source.Length <= count ? source : source[..count];
        }

        /// <summary>
        /// Splits a sequence into a maximum number of subsequences based on a specified delimiter.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The sequence to be split.</param>
        /// <param name="delimiter">A <typeparamref name="T"/> that delimits the subsequences in <paramref name="source"/>.</param>
        /// <param name="count">The maximum number of splits. If zero, split on every occurence of <paramref name="delimiter"/>.</param>
        /// <param name="countExceedingBehaviour">Specifies how the elements after count splits should be handled.</param>
        /// <returns>A sequence of split subsequences.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is negative.</exception>
        public static IEnumerable<IEnumerable<T>> Split<T>(IEnumerable<T> source, T delimiter, int count = 0, CountExceedingBehaviour countExceedingBehaviour = CountExceedingBehaviour.AppendLastElements) where T : IEquatable<T>
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(count);
#else
            if(count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
#endif

            List<T> segment = [];

            foreach(T element in source)
            {
                if(count == 1 && countExceedingBehaviour == CountExceedingBehaviour.CutLastElements && element.Equals(delimiter))
                {
                    break;
                }

                if(count == 1 || !element.Equals(delimiter))
                {
                    segment.Add(element);
                }
                else
                {
                    yield return segment;
                    segment = [];
                    count--;
                }
            }

            yield return segment;
        }

        /// <summary>
        /// Splits a sequence into a maximum number of subsequences based on specified delimiters.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The sequence to be split.</param>
        /// <param name="delimiters">A <see cref="IEnumerable{T}"/> with the instances of <typeparamref name="T"/> that delimit the subsequences in <paramref name="source"/>.</param>
        /// <param name="count">The maximum number of splits. If zero, split on every occurence of <paramref name="delimiters"/>.</param>
        /// <param name="countExceedingBehaviour">Specifies how the elements after count splits should be handled.</param>
        /// <returns>A sequence of split subsequences.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is negative.</exception>
        public static IEnumerable<IEnumerable<T>> SplitAny<T>(IEnumerable<T> source, IEnumerable<T> delimiters, int count = 0, CountExceedingBehaviour countExceedingBehaviour = CountExceedingBehaviour.AppendLastElements) where T : IEquatable<T>
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(count);
#else
            if(count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
#endif

            List<T> segment = [];

            foreach(T element in source)
            {
                if(count == 1 && countExceedingBehaviour == CountExceedingBehaviour.CutLastElements && delimiters.Any(delimiter => element.Equals(delimiter)))
                {
                    break;
                }

                if(count == 1 || delimiters.All(delimiter => !element.Equals(delimiter)))
                {
                    segment.Add(element);
                }
                else
                {
                    yield return segment;
                    segment = [];
                    count--;
                }
            }

            yield return segment;
        }

        /// <summary>
        /// Splits a sequence into a maximum number of subsequences based on specified delimiter subsequence.
        /// </summary>
        /// <param name="source">The sequence to be split.</param>
        /// <param name="delimiter">A <see cref="IEnumerable{int}"/> that delimits the subsequences in <paramref name="source"/>.</param>
        /// <param name="count">The maximum number of splits. If zero, split on every occurence of <paramref name="delimiter"/>.</param>
        /// <param name="countExceedingBehaviour">Specifies how the elements after count splits should be handled.</param>
        /// <returns>A sequence of split subsequences.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is negative.</exception>
        public static IEnumerable<IEnumerable<int>> Split(IEnumerable<int> source, IEnumerable<int> delimiter, int count = 0, CountExceedingBehaviour countExceedingBehaviour = CountExceedingBehaviour.AppendLastElements)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(count);
#else
            if(count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
#endif

            string _source = string.Join(",", source);
            string _delimiter = string.Join(",", delimiter);

            string[] splits = countExceedingBehaviour switch
            {
                CountExceedingBehaviour.AppendLastElements => _source.Split(_delimiter, count == 0 ? int.MaxValue : count, StringSplitOptions.None),
                CountExceedingBehaviour.CutLastElements => _source.Split(_delimiter, int.MaxValue, StringSplitOptions.None).UpTo(count),
                _ => throw UnhandledCaseException(countExceedingBehaviour)
            };

            return splits.Select(s => s.Trim(',').Split(',').Select(x => int.Parse(x, CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Splits a string into a maximum number of substrings based on a specified delimiting string and, optionally, options.
        /// </summary>
        /// <typeparam name="T">The type of the separator. <strong>The only supported types: <see cref="char"/>, <see cref="string"/>, <see cref="char"/>[]</strong>.</typeparam>
        /// <param name="source">The string to be split.</param>
        /// <param name="separator">A char/string/cahr[] that delimits the substrings in this instance.</param>
        /// <param name="count">The maximum number of elements expected in the array.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
        /// <param name="countExceedingBehaviour">Specifies how the elements after count splits should be handled.</param>
        /// <returns>An array that contains at most <paramref name="count"/> substrings from this instance that are delimited by <paramref name="separator"/>.</returns>
        /// <exception cref="UnreachableException"></exception>
        public static string[] Split<T>(this string source, T separator, int count = 0, StringSplitOptions options = StringSplitOptions.None, CountExceedingBehaviour countExceedingBehaviour = CountExceedingBehaviour.AppendLastElements)
        {
            if(count == 0)
            {
                count = int.MaxValue;
            }

            return separator switch
            {
                char charSeparator => countExceedingBehaviour switch
                {
                    CountExceedingBehaviour.AppendLastElements => source.Split(charSeparator, count, options),
                    CountExceedingBehaviour.CutLastElements => source.Split(charSeparator, options).UpTo(count),
                    _ => throw UnhandledCaseException(countExceedingBehaviour)
                },
                string stringSeparator => countExceedingBehaviour switch
                {
                    CountExceedingBehaviour.AppendLastElements => source.Split(stringSeparator, count, options),
                    CountExceedingBehaviour.CutLastElements => source.Split(stringSeparator, options).UpTo(count),
                    _ => throw UnhandledCaseException(countExceedingBehaviour)
                },
                char[] charSeparators => countExceedingBehaviour switch
                {
                    CountExceedingBehaviour.AppendLastElements => source.Split(charSeparators, count, options),
                    CountExceedingBehaviour.CutLastElements => source.Split(charSeparators, options).UpTo(count),
                    _ => throw UnhandledCaseException(countExceedingBehaviour)
                },
                _ => throw new NotSupportedException($"Invalid separator type: {typeof(T)}")
            };
        }

        /// <summary>
        /// Splits a sequence into a maximum number of subsequences based on specified delimiter subsequence.
        /// </summary>
        /// <param name="source">The sequence to be split.</param>
        /// <param name="delimiter">A <see cref="IEnumerable{char}"/> that delimits the subsequences in <paramref name="source"/>.</param>
        /// <param name="count">The maximum number of splits. If zero, split on every occurence of <paramref name="delimiter"/>.</param>
        /// <param name="countExceedingBehaviour">Specifies how the elements after count splits should be handled.</param>
        /// <returns>A sequence of split subsequences.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is negative.</exception>
        public static IEnumerable<IEnumerable<char>> Split(IEnumerable<char> source, IEnumerable<char> delimiter, int count = 0, CountExceedingBehaviour countExceedingBehaviour = CountExceedingBehaviour.AppendLastElements)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(count);
#else
            if(count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
#endif

            string _source = string.Join(",", source);
            string _delimiter = string.Join(",", delimiter);

            string[] splits = countExceedingBehaviour switch
            {
                CountExceedingBehaviour.AppendLastElements => _source.Split(_delimiter, count == 0 ? int.MaxValue : count, StringSplitOptions.None),
                CountExceedingBehaviour.CutLastElements => _source.Split(_delimiter, int.MaxValue, StringSplitOptions.None).UpTo(count),
                _ => throw UnhandledCaseException(countExceedingBehaviour)
            };

            return splits.Select(s => s.Trim(',').Split(',').Select(x => x[0]));
        }

        /// <summary>
        /// Get an array with all permutations of the <see cref="StringSplitOptions"/> enum flags.
        /// </summary>
        /// <returns>All permutations of the <see cref="StringSplitOptions"/> enum flags.</returns>
        public static StringSplitOptions[] GetAllStringSplitOptions()
        {
#if NET5_0_OR_GREATER
            // ensure that no new option was added in an update
            Debug.Assert(Enumerable.SequenceEqual(
                (StringSplitOptions[])Enum.GetValues(typeof(StringSplitOptions)),
                [StringSplitOptions.None, StringSplitOptions.RemoveEmptyEntries, StringSplitOptions.TrimEntries]
            ));

            return [
                StringSplitOptions.None,
                StringSplitOptions.RemoveEmptyEntries,
                StringSplitOptions.TrimEntries,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ];
#else
            return [
                StringSplitOptions.None,
                StringSplitOptions.RemoveEmptyEntries
            ];
#endif
        }
    }
}
