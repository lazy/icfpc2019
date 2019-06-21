namespace Tests
{
    using Xunit;

    public class EnvironmentTests
    {
        [Fact]
        public void NullableTypesAreEnabled()
        {
            string? a = null;
            Assert.True(a == null);
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
