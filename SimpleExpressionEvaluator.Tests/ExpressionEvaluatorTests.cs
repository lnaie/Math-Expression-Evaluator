using System;
using Xunit;

namespace SimpleExpressionEvaluator.Tests
{
    public class ExpressionEvaluatorTests: IUseFixture<object>
    {
        private ExpressionEvaluator engine;
        private Random generator;

		public void SetFixture(object data)
		{
			engine = new ExpressionEvaluator();
			generator = new Random();
		}


		[Fact]
        public void Empty_String_Is_Zero()
        {
            Assert.Equal(engine.Evaluate(""), 0);
        }

		[Fact]
        public void Decimal_Is_Treated_As_Decimal()
        {
            var left = generator.Next(1, 100);

			Assert.Equal(engine.Evaluate(left.ToString()), left);
        }

		[Fact]
        public void Two_Plus_Two_Is_Four()
        {
			Assert.Equal(engine.Evaluate("2+2"), 4);
        }

		[Fact]
        public void Can_Add_Two_Decimal_Numbers()
        {
			Assert.Equal(engine.Evaluate("2.7+3.2"), (2.7m + 3.2m));
        }

		[Fact]
        public void Can_Add_Many_Numbers()
        {
			Assert.Equal(engine.Evaluate("1.2+3.4+5.6+7.8"), (1.2m + 3.4m + 5.6m + 7.8m));
			Assert.Equal(engine.Evaluate("1.7+2.9+14.24+6.58"), (1.7m + 2.9m + 14.24m + 6.58m));
        }

		[Fact]
        public void Can_Subtract_Two_Numbers()
        {
			Assert.Equal(engine.Evaluate("5-2"), (5 - 2));
        }

		[Fact]
        public void Can_Subtract_Multiple_Numbers()
        {
			Assert.Equal(engine.Evaluate("15.2-2.3-4.8-0.58"), (15.2m - 2.3m - 4.8m - 0.58m));
        }

		[Fact]
        public void Can_Add_And_Subtract_Multiple_Numbers()
        {
			Assert.Equal(engine.Evaluate("15+8-4-2+7"), (15 + 8 - 4 - 2 + 7));
			Assert.Equal(engine.Evaluate("17.89-2.47+7.16"), (17.89m - 2.47m + 7.16m));

        }

		[Fact]
        public void Can_Add_Subtract_Multiply_Divide_Multiple_Numbers()
        {
			Assert.Equal(engine.Evaluate("50-5*3*2+7"), (50 - 5 * 3 * 2 + 7));
			Assert.Equal(engine.Evaluate("84+15+4-4*3*9+24+4-54/3-5-7+47"), (84 + 15 + 4 - 4 * 3 * 9 + 24 + 4 - 54 / 3 - 5 - 7 + 47));
			Assert.Equal(engine.Evaluate("50-48/4/3+7*2*4+2+5+8"), (50 - 48 / 4 / 3 + 7 * 2 * 4 + 2 + 5 + 8));
			Assert.Equal(engine.Evaluate("5/2/2+1.5*3+4.58"), (5 / 2m / 2m + 1.5m * 3m + 4.58m));
			Assert.Equal(engine.Evaluate("25/3+1.34*2.56+1.49+2.36/1.48"), (25 / 3m + 1.34m * 2.56m + 1.49m + 2.36m / 1.48m));
			Assert.Equal(engine.Evaluate("2*3+5-4-2*5+7"), (2 * 3 + 5 - 4 - 2 * 5 + 7));
        }

		[Fact]
        public void Supports_Parentheses()
        {
			Assert.Equal(engine.Evaluate("2*(5+3)"), (2 * (5 + 3)));
			Assert.Equal(engine.Evaluate("(5+3)*2"), ((5 + 3) * 2));
			Assert.Equal(engine.Evaluate("(5+3)*5-2"), ((5 + 3) * 5 - 2));
			Assert.Equal(engine.Evaluate("(5+3)*(5-2)"), ((5 + 3) * (5 - 2)));
			Assert.Equal(engine.Evaluate("((5+3)*3-(8-2)/2)/2"), (((5 + 3) * 3 - (8 - 2) / 2) / 2m));
			Assert.Equal(engine.Evaluate("(4*(3+5)-4-8/2-(6-4)/2)*((2+4)*4-(8-5)/3)-5"), ((4 * (3 + 5) - 4 - 8 / 2 - (6 - 4) / 2) * ((2 + 4) * 4 - (8 - 5) / 3) - 5));
			Assert.Equal(engine.Evaluate("(((9-6/2)*2-4)/2-6-1)/(2+24/(2+4))"), ((((9 - 6 / 2) * 2 - 4) / 2m - 6 - 1) / (2 + 24 / (2 + 4))));
        }

		[Fact]
        public void Can_Process_Simple_Variables()
        {
            decimal a = 2.6m;
            decimal b = 5.7m;

			Assert.Equal(engine.Evaluate("a", new { a }), (a));
			Assert.Equal(engine.Evaluate("a+a", new { a }), (a + a));
			Assert.Equal(engine.Evaluate("a+b", new { a, b }), (a + b));
        }

		[Fact]
        public void Can_Process_Multiple_Variables()
        {
            var a = 6;
            var b = 4.5m;
            var c = 2.6m;
			Assert.Equal(engine.Evaluate("(((9-a/2)*2-b)/2-a-1)/(2+c/(2+4))", new { a, b, c }), ((((9 - a / 2) * 2 - b) / 2 - a - 1) / (2 + c / (2 + 4))));
			Assert.Equal(engine.Evaluate("(c+b)*a", new { a, b, c }), ((c + b) * a));
        }

		[Fact]
        public void Can_Pass_Named_Variables()
        {
            dynamic dynamicEngine = new ExpressionEvaluator();

            var a = 6;
            var b = 4.5m;
            var c = 2.6m;

			Assert.Equal(dynamicEngine.Evaluate("(c+b)*a", a: 6, b: 4.5, c: 2.6), ((c + b) * a));
        }

		[Fact]
        public void Can_Invoke_Expression_Multiple_Times()
        {
            var a = 6m;
            var b = 3.9m;
            var c = 4.9m;

            var compiled = engine.Compile("(a+b)/(a+c)");
			Assert.Equal(compiled(new { a, b, c }), ((a + b) / (a + c)));

            a = 5.4m;
            b = -2.4m;
            c = 7.5m;

			Assert.Equal(compiled(new { a, b, c }), ((a + b) / (a + c)));
        }
	}
}