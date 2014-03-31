using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;

namespace SimpleExpressionEvaluator
{
	public class ExpressionEvaluator
	{
		private readonly Stack<Expression> expressionStack = new Stack<Expression>();
		private readonly Stack<char> operatorStack = new Stack<char>();
		private readonly List<string> parameters = new List<string>();


		public Func<object, decimal> Compile(string expression)
		{
			var compiled = Parse(expression);

			Func<object, decimal> result = argument =>
			{
				var arguments = ParseArguments(argument);
				return Execute(compiled, arguments);
			};

			return result;
		}

		public decimal Evaluate(string expression, object argument = null)
		{
			var arguments = ParseArguments(argument);

			return Evaluate(expression, arguments);
		}

		public decimal Evaluate(string expression, Dictionary<string, decimal> arguments)
		{
			var compiled = Parse(expression);

			return Execute(compiled, arguments);
		}

		private Func<decimal[], decimal> Parse(string expression)
		{
			if (string.IsNullOrWhiteSpace(expression))
			{
				return s => 0;
			}

			var arrayParameter = Expression.Parameter(typeof(decimal[]), "args");

			parameters.Clear();
			operatorStack.Clear();
			expressionStack.Clear();

			using (var reader = new StringReader(expression))
			{
				int peek; char prevChar = '\0';

				while ((peek = reader.Peek()) > -1)
				{
					var next = (char)peek;

					if (char.IsDigit(next))
					{
						expressionStack.Push(ReadOperand(reader));

						prevChar = next;
						continue;
					}

					if (char.IsLetter(next))
					{
						expressionStack.Push(ReadParameter(reader, arrayParameter));

						prevChar = next;
						continue;
					}

					if (Operation.IsDefined(next))
					{
						var currentOperation = ReadOperation(reader);

						// Workaround: handle operands sign
						if (currentOperation == Operation.Subtraction || currentOperation == Operation.Addition)
						{
							var nextChar = PeekNextChar(reader);
							if (char.IsDigit(nextChar))
							{
								if (prevChar == '\0' || prevChar == '*' || prevChar == '/' || prevChar == '(')
								{
									expressionStack.Push(ReadOperand(reader, (currentOperation == Operation.Subtraction)));

									prevChar = nextChar;
									continue;
								}
							}
						}

						EvaluateWhile(() => 
							operatorStack.Count > 0 && 
							operatorStack.Peek() != '(' && 
							currentOperation.Precedence <= ((Operation)operatorStack.Peek()).Precedence);

						operatorStack.Push(next);

						prevChar = next;
						continue;
					}

					if (next == '(')
					{
						reader.Read();
						operatorStack.Push('(');

						prevChar = next;
						continue;
					}

					if (next == ')')
					{
						reader.Read();
						EvaluateWhile(() => operatorStack.Count > 0 && operatorStack.Peek() != '(');
						operatorStack.Pop();

						prevChar = next;
						continue;
					}

					if (next == ' ')
					{
						// Don't track space as previous char
						reader.Read();
					}
					else
					{
						throw new ArgumentException(string.Format("Encountered invalid character {0}", next), "expression");
					}
				}
			}

			EvaluateWhile(() => operatorStack.Count > 0);

			var lambda = Expression.Lambda<Func<decimal[], decimal>>(expressionStack.Pop(), arrayParameter);
			var compiled = lambda.Compile();
			return compiled;
		}

		char PeekNextChar(StringReader reader)
		{
			var peek = reader.Peek();
			if (peek > 0)
				return (char) peek;

			return (char) 0;
		}

		private Dictionary<string, decimal> ParseArguments(object argument)
		{
			if (argument == null)
			{
				return new Dictionary<string, decimal>();
			}

			var argumentType = argument.GetType();

			var properties = argumentType
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Where(p => p.CanRead && IsNumeric(p.PropertyType));

			var arguments = properties.ToDictionary(
				property => property.Name, 
				property => Convert.ToDecimal(property.GetValue(argument, null), CultureInfo.InvariantCulture));

			return arguments;
		}

		private decimal Execute(Func<decimal[], decimal> compiled, Dictionary<string, decimal> arguments)
		{
			arguments = arguments ?? new Dictionary<string, decimal>();

			if (parameters.Count != arguments.Count)
			{
				throw new ArgumentException(string.Format("Expression contains {0} parameters but got only {1}", parameters.Count, arguments.Count));
			}

			var missingParameters = parameters.Where(p => !arguments.ContainsKey(p)).ToList();

			if (missingParameters.Any())
			{
				throw new ArgumentException("No values provided for parameters: " + string.Join(",", missingParameters));
			}

			var values = parameters
				.Select(parameter => arguments[parameter])
				.ToArray();
			return compiled(values);
		}

		private void EvaluateWhile(Func<bool> condition)
		{
			while (condition())
			{
				var right = expressionStack.Pop();
				var left = expressionStack.Pop();

				expressionStack.Push(((Operation)operatorStack.Pop()).Apply(left, right));
			}
		}

		private Expression ReadOperand(TextReader reader, bool negative = false)
		{
			var operand = string.Empty;

			int peek;

			while ((peek = reader.Peek()) > -1)
			{
				var next = (char)peek;

				if (char.IsDigit(next) || 
					next == '.' || 
					next == 'e' || next == 'E' ||
					((next == '-' || next == '+') && operand.EndsWith("e", StringComparison.OrdinalIgnoreCase)))
				{
					reader.Read();
					operand += next;
				}
				else
				{
					break;
				}
			}

			if (operand.IndexOf("e", StringComparison.OrdinalIgnoreCase) >= 0)
				return Expression.Constant((decimal)(double.Parse(operand, CultureInfo.InvariantCulture) * (negative ? -1 : 1)));
			else
				return Expression.Constant((decimal.Parse(operand, CultureInfo.InvariantCulture) * (negative ? -1 : 1)));
		}

		private Operation ReadOperation(TextReader reader)
		{
			var operation = (char)reader.Read();
			return (Operation)operation;
		}

		private Expression ReadParameter(TextReader reader, Expression arrayParameter)
		{
			var parameter = string.Empty;

			int peek;

			while ((peek = reader.Peek()) > -1)
			{
				var next = (char)peek;

				if (char.IsLetter(next))
				{
					reader.Read();
					parameter += next;
				}
				else
				{
					break;
				}
			}

			if (!parameters.Contains(parameter))
			{
				parameters.Add(parameter);
			}

			return Expression.ArrayIndex(arrayParameter, Expression.Constant(parameters.IndexOf(parameter)));
		}


		internal bool IsNumeric(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return true;
			}
			return false;
		}
	}

	internal sealed class Operation
	{
		private readonly int precedence;
		private readonly string name;
		private readonly Func<Expression, Expression, Expression> operation;

		public static readonly Operation Addition = new Operation(1, Expression.Add, "Addition");
		public static readonly Operation Subtraction = new Operation(1, Expression.Subtract, "Subtraction");
		public static readonly Operation Multiplication = new Operation(2, Expression.Multiply, "Multiplication");
		public static readonly Operation Division = new Operation(2, Expression.Divide, "Division");

		private static readonly Dictionary<char, Operation> Operations = new Dictionary<char, Operation>
        {
            { '+', Addition },
            { '-', Subtraction },
            { '*', Multiplication},
            { '/', Division }
        };

		private Operation(int precedence, Func<Expression, Expression, Expression> operation, string name)
		{
			this.precedence = precedence;
			this.operation = operation;
			this.name = name;
		}

		public int Precedence
		{
			get { return precedence; }
		}

		public static explicit operator Operation(char operation)
		{
			Operation result;

			if (Operations.TryGetValue(operation, out result))
			{
				return result;
			}
			else
			{
				throw new InvalidCastException();
			}
		}

		public Expression Apply(Expression left, Expression right)
		{
			return operation(left, right);
		}

		public static bool IsDefined(char operation)
		{
			return Operations.ContainsKey(operation);
		}
	}
}