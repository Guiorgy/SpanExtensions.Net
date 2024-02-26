using static SpanExtensions.Tests.UnitTests.TestHelper;
using SpanExtensions.SourceGenerators;

namespace SpanExtensions.Tests.UnitTests
{
    [GenerateCopy(RegexReplaces = new[] { "(?<!ReadOnly)Span", "ReadOnlySpan" })]
    public static partial class SpanSplitTests
    {
        static readonly IEnumerable<StringSplitOptions> stringSplitOptions = GetAllStringSplitOptions();
        static readonly CountExceedingBehaviour[] countExceedingBehaviours = (CountExceedingBehaviour[])Enum.GetValues(typeof(CountExceedingBehaviour));
    }
}
