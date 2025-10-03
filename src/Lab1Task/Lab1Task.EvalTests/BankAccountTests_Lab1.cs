using FluentAssertions;
using System.Reflection;

namespace Lab1Task.EvalTests
{
    public class BankAccountTests_Lab1
    {
        private static Type BankType =>
                   Type.GetType("BankLab.BankAccount, BankLab")
                   ?? throw new InvalidOperationException("Nie znaleziono typu BankLab.BankAccount");

        private static bool IsNumeric(Type t) =>
            t == typeof(decimal) || t == typeof(double) || t == typeof(float) ||
            t == typeof(long) || t == typeof(int) || t == typeof(short);

        private static object ConvertTo(Type t, decimal value)
        {
            if (t == typeof(decimal)) return value;
            if (t == typeof(double)) return (double)value;
            if (t == typeof(float)) return (float)value;
            if (t == typeof(long)) return (long)value;
            if (t == typeof(int)) return (int)value;
            if (t == typeof(short)) return (short)value;
            throw new InvalidOperationException($"Nieobsługiwany typ liczbowy: {t.Name}");
        }

        private static decimal ToDecimal(object? v)
        {
            return v switch
            {
                decimal d => d,
                double db => (decimal)db,
                float f => (decimal)f,
                long l => l,
                int i => i,
                short s => s,
                _ => throw new InvalidOperationException("Balance nie jest typem liczbowym czytelnym dla testu.")
            };
        }

        private static object CreateInstance()
        {
           
            var ctor = BankType.GetConstructor(Type.EmptyTypes);
            if (ctor != null) return Activator.CreateInstance(BankType)!;

            var anyCtor = BankType.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault()
                          ?? throw new InvalidOperationException("Brak dostępnego konstruktora klasy BankAccount.");
            var args = anyCtor.GetParameters().Select(p =>
            {
                if (p.ParameterType == typeof(string)) return "";
                if (p.ParameterType == typeof(bool)) return false;
                if (IsNumeric(p.ParameterType)) return ConvertTo(p.ParameterType, 0m);
                if (p.ParameterType == typeof(DateTime)) return DateTime.MinValue;
         
                return p.HasDefaultValue ? p.DefaultValue! : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)! : null);
            }).ToArray();
            return anyCtor.Invoke(args);
        }

        private static PropertyInfo GetBalanceProp()
        {
            var prop = BankType.GetProperty("Balance", BindingFlags.Public | BindingFlags.Instance);
            prop.Should().NotBeNull("Właściwość Balance musi istnieć (publiczna).");
            return prop!;
        }

        private static MethodInfo GetNumericMethod(string name, int paramCount)
        {
            var m = BankType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .FirstOrDefault(mi =>
                                mi.Name == name &&
                                mi.GetParameters().Length == paramCount &&
                                (paramCount == 1
                                    ? IsNumeric(mi.GetParameters()[0].ParameterType)
                                    : (mi.GetParameters()[0].ParameterType == BankType &&
                                       IsNumeric(mi.GetParameters()[1].ParameterType))));
            m.Should().NotBeNull($"{name} o oczekiwanej sygnaturze musi istnieć.");
            return m!;
        }

        private static void SeedBalance(object account, PropertyInfo balanceProp, decimal target)
        {
         
            if (balanceProp.CanWrite)
            {
                balanceProp.SetValue(account, ConvertTo(balanceProp.PropertyType, target));
                return;
            }

      
            var deposit = GetNumericMethod("Deposit", 1);
            var amountType = deposit.GetParameters()[0].ParameterType;

  
            var current = ToDecimal(balanceProp.GetValue(account));
            if (current > target)
            {
               
                var withdraw = GetNumericMethod("Withdraw", 1);
                var wType = withdraw.GetParameters()[0].ParameterType;
                var diff = current - target;
                withdraw.Invoke(account, new[] { ConvertTo(wType, diff) });
            }
            else if (current < target)
            {
                var diff = target - current;
                deposit.Invoke(account, new[] { ConvertTo(amountType, diff) });
            }
        }

 
        [Theory]
        [InlineData("Deposit", 1)]
        [InlineData("Withdraw", 1)]
        [InlineData("TransferTo", 2)]
        public void Methods_Should_Exist_With_Expected_Arity(string methodName, int paramCount)
        {

            var methods = BankType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(m => m.Name == methodName && m.GetParameters().Length == paramCount)
                                  .ToArray();
            methods.Should().NotBeEmpty($"Metoda {methodName}({paramCount} parametr/ów) musi istnieć.");
        }

        [Fact]
        public void TransferTo_Should_Accept_BankAccount_And_Numeric()
        {
  
            var m = GetNumericMethod("TransferTo", 2);
            m.GetParameters()[0].ParameterType.Should().Be(BankType);
            IsNumeric(m.GetParameters()[1].ParameterType).Should().BeTrue();
        }


        [Fact]
        public void Deposit_Should_Increase_Balance()
        {
            var account = CreateInstance();
            var balance = GetBalanceProp();

 
            SeedBalance(account, balance, 100m);

            var deposit = GetNumericMethod("Deposit", 1);
            var pType = deposit.GetParameters()[0].ParameterType;

            deposit.Invoke(account, new[] { ConvertTo(pType, 50m) });
            ToDecimal(balance.GetValue(account)).Should().Be(150m);
        }

        [Fact]
        public void Withdraw_Should_Decrease_Balance()
        {
            var account = CreateInstance();
            var balance = GetBalanceProp();

  
            SeedBalance(account, balance, 200m);

            var withdraw = GetNumericMethod("Withdraw", 1);
            var pType = withdraw.GetParameters()[0].ParameterType;

            withdraw.Invoke(account, new[] { ConvertTo(pType, 30m) });
            ToDecimal(balance.GetValue(account)).Should().Be(170m);
        }

        [Fact]
        public void TransferTo_Should_Move_Funds()
        {
            var a = CreateInstance();
            var b = CreateInstance();

            var balance = GetBalanceProp();


            SeedBalance(a, balance, 300m);
            SeedBalance(b, balance, 10m);

            var transfer = GetNumericMethod("TransferTo", 2);
            var amountType = transfer.GetParameters()[1].ParameterType;
          
            transfer.Invoke(a, new[] { b, ConvertTo(amountType, 70m) });

            ToDecimal(balance.GetValue(a)).Should().Be(230m);
            ToDecimal(balance.GetValue(b)).Should().Be(80m);
        }
    }
}
}