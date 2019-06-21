namespace Tests
{
    using System;

    using Xunit;

    public class SampleUnitTest
    {
        [Fact]
        public void NullableTypesAreEnabled()
        {
            string? a = null;
            Console.WriteLine($"{a}");
        }

        [Fact]
        public void TestSwitch()
        {
            var tuple = (1, "hello");

            var switchResult = tuple switch
            {
                (var a, var b) when a % 2 == 1 && b.StartsWith("h") => "right",
                _ => "wrong"
            };

            Assert.Equal("right", switchResult);
        }
    }
}
