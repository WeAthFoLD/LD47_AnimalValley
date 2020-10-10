using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sirenix.Utilities;

namespace SPlay {
	public interface ExpressionContext {
		object GetVariable(StringHash nameHash);
	}

	// An id token in S-expression. Distinguinshes from string literal.
	public struct ID {
		public readonly string id;

		public ID(string id) {
			this.id = id;
		}
	}

	// public class FunctionArgs {
	// 	public FunctionArgs(object[] args) {
	// 		this.args = args;
	// 	}
	//
	// 	public float GetFloat(int idx) {
	// 		_CheckIndex(idx);
	// 		var obj = args[idx];
	// 		if (obj is int i)
	// 			return i;
	// 		if (obj is float f)
	// 			return f;
	// 		throw new InvalidCastException(obj + " is not float");
	// 	}
	//
	// 	public bool GetBool(int idx) {
	// 		_CheckIndex(idx);
	// 		return (bool) args[idx];
	// 	}
	//
	// 	public int GetInt(int idx) {
	// 		_CheckIndex(idx);
	// 		return (int) args[idx];
	// 	}
	//
	// 	public string GetString(int idx) {
	// 		_CheckIndex(idx);
	// 		return (string) args[idx];
	// 	}
	//
	// 	public Expression GetExpression(int idx) {
	// 		_CheckIndex(idx);
	// 		return (Expression) args[idx];
	// 	}
	//
	// 	private void _CheckIndex(int idx) {
	// 		if (idx < 0 || idx >= count)
	// 			throw new IndexOutOfRangeException($"Index {idx} is out of range! count {args.Length}");
	// 	}
	//
	// 	public int count => args.Length;
	//
	// 	public readonly object[] args;
	// }

	// 表示一个 S-expression 如果被调用 对应的C# function会被创建
	public class Expression {
		public static object Evaluate(object maybeExpression, ExpressionContext ctx) {
			if (maybeExpression is Expression exp) {
				return exp.Evaluate(ctx);
			} else {
				return maybeExpression;
			}
		}

		public readonly object[] entries;
		private ExpressionFunction _functionInstance = null;

		public Expression(object[] entries) {
			this.entries = entries;
		}

		/// 继承S-expr的内容，但不继承function impl，相当于创建一个新的instance
		public Expression Clone() {
			return new Expression(entries);
		}

		public void ResetFunction() {
			_functionInstance?.Reset();
		}

		public object Evaluate(ExpressionContext ctx) {
			XDebug.Assert(entries.Length > 0, "Must have function name");

			var firstEntry = entries[0];
			XDebug.Assert(firstEntry is ID, "Expression call first element must be ID");

			var functionNameHash = new StringHash(((ID) firstEntry).id);
			if (_functionInstance == null) {
				_functionInstance = FunctionRegistry.GetFunction(functionNameHash);
				_functionInstance.Init(entries.Skip(1).ToArray());
			}

			return _functionInstance.Invoke(ctx);
		}
	}

	public abstract class ExpressionFunction {
		public abstract void Init(object[] args);

		public abstract object Invoke(ExpressionContext ctx);

		public virtual void Reset() { }
	}

	public class FunctionRegistry {
		static FunctionRegistry() {
			ReflectionUtil.FindAllSubTypes(typeof(ExpressionFunction))
				.ForEach(ty => {
					var funcName = _Lispify(ty.Name);
					XDebug.Log("Found function " + funcName);
					functions.Add(new StringHash(funcName), ty);
				});
		}

		private static string _Lispify(string name) {
			var result = new StringBuilder();
			int begin = 0;
			for (int i = 1; i <= name.Length; ++i) {
				if (i == name.Length || char.IsUpper(name[i])) {
					if (result.Length > 0)
						result.Append('-');
					result.Append(name.Substring(begin, i - begin).ToLower());
					begin = i;
				}
			}

			return result.ToString();
		}

		public static ExpressionFunction GetFunction(StringHash h) {
			if (functions.TryGetValue(h, out var ty)) {
				return (ExpressionFunction) Activator.CreateInstance(ty);
			} else {
				throw new Exception("Can't find function " + h.GetOriginalString());
			}
		}

		private readonly static Dictionary<StringHash, Type> functions =
			new Dictionary<StringHash, Type>();
	}
}