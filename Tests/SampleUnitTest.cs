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
    }
}
