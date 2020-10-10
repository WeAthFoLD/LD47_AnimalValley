using System.Collections.Generic;
using UnityEngine;

namespace SPlay
{

	public class Log : ExpressionFunction {

		private object _firstArg;

		public override void Init(object[] args) {
			_firstArg = args[0];
		}

		public override object Invoke(ExpressionContext ctx) {
			var logContent = Expression.Evaluate(_firstArg, ctx);
			if (logContent is string s)
			{
				XDebug.Log(s);
			}
			else if (logContent is Expression childExp)
			{
				XDebug.Log((string) childExp.Evaluate(ctx));
			}
			return null;
		}
	}

	public class DestroyOwner : ExpressionFunction {
		public override void Init(object[] args) {
		}

		public override object Invoke(ExpressionContext ctx) {
			Object.Destroy(ctx.GetVariable(new StringHash("Owner")) as Object);
			return null;
		}
	}
}