using System;
using System.Collections.Generic;
using System.Dynamic;

namespace SimpleExpressionEvaluator
{
	public class DynamicExpressionEvaluator: DynamicObject
	{
		readonly ExpressionEvaluator _evaluator;

		public DynamicExpressionEvaluator()
		{
			_evaluator = new ExpressionEvaluator();
		}


		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			if ("Evaluate" != binder.Name)
			{
				return base.TryInvokeMember(binder, args, out result);
			}

			if (!(args[0] is string))
			{
				throw new ArgumentException("No expression specified for parsing");
			}

			//args will contain expression and arguments,
			//ArgumentNames will contain only named arguments
			if (args.Length != binder.CallInfo.ArgumentNames.Count + 1)
			{
				throw new ArgumentException("Argument names missing.");
			}

			var arguments = new Dictionary<string, decimal>();

			for (int i = 0; i < binder.CallInfo.ArgumentNames.Count; i++)
			{
				if (_evaluator.IsNumeric(args[i + 1].GetType()))
				{
					arguments.Add(binder.CallInfo.ArgumentNames[i], Convert.ToDecimal(args[i + 1]));
				}
			}

			result = _evaluator.Evaluate((string)args[0], arguments);
			return true;
		}
	}
}