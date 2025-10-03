namespace Lab1Task.EvalTests
{
    public class BankAccountTests
    {
        [Fact]
        public void Class_BankAccount_Should_Exist()
        {
            var type = typeof(Lab1Task.ConsoleApp.Program).Assembly.GetType("Lab1Task.Console.BankAccount");
            Assert.NotNull(type);
        }

        [Theory]
        [InlineData("AccountNumber")]
        [InlineData("OwnerName")]
        [InlineData("Balance")]
        [InlineData("Currency")]
        [InlineData("IsActive")]
        public void Properties_Should_Exist(string propertyName)
        {
            var type = typeof(Lab1Task.ConsoleApp.Program).Assembly.GetType("BankLab.BankAccount");
            Assert.NotNull(type?.GetProperty(propertyName));
        }
    }
}