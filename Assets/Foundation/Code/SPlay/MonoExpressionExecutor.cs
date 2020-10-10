using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SPlay {

	public class MonoExpressionExecutor : MonoBehaviour, ExpressionContext {

		public enum ParamType {
			Float, Int, Bool, String
		}

		public enum TriggerType {
			Update, FixedUpdate, Enable
		}

		[Serializable]
		public struct Param {
			public ParamType ty;
			public string name;
			public string s;
			public int i;
			public float f;
			public bool b;
		}

		public TriggerType triggerType;

		[TextArea]
		public string expr;

		public List<Param> paramList;

		private Expression _expr;

		void Awake() {
			// TODO: 做预编译
			_expr = ExpressionParser.Parse(expr);
		}

		private void OnEnable() {
			if (triggerType == TriggerType.Enable)
				_expr.Evaluate(this);
		}

		void Update() {
			if (triggerType == TriggerType.Update)
				_expr.Evaluate(this);
		}

		private void FixedUpdate() {
			if (triggerType == TriggerType.FixedUpdate)
				_expr.Evaluate(this);
		}

		public object GetVariable(StringHash nameHash) {
			if (nameHash == new StringHash("Owner")) {
				return gameObject;
			}

			return null;
		}
	}
}