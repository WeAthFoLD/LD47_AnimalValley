using System;
using System.Collections.Generic;

namespace SPlay {
	public enum NodeState {
		Running,
		Success,
		Fail
	}

	public class Seq : ExpressionFunction {
		public override void Init(object[] args) {
			_childNodes = args;
		}

		public override object Invoke(ExpressionContext ctx) {
			if (_idx == -1)
				_idx = 0;

			while (true) {
				var stateAny = Expression.Evaluate(_childNodes[_idx], ctx);
				NodeState state;
				if (stateAny is NodeState st) {
					state = st;
				} else {
					state = NodeState.Success;
				}

				if (state == NodeState.Fail) {
					Reset();
					return NodeState.Fail;
				} else if (state == NodeState.Success) {
					++_idx;
					if (_idx == _childNodes.Length) {
						Reset();
						return NodeState.Success;
					}
					// 实际上只有这里会循环 保证当帧完成所有操作
				} else {
					return NodeState.Running;
				}
			}
		}

		public override void Reset() {
			_idx = -1;
			foreach (var child in _childNodes) {
				if (child is Expression exp) {
					exp.ResetFunction();
				}
			}
		}

		private object[] _childNodes;
		private int _idx = -1;
	}

	public class Wait : ExpressionFunction {
		private object _waitSecs;

		private bool _started = false;
		private float _actualWaitSecs;
		private DateTime _startWaitTime;

		public override void Init(object[] args) {
			_waitSecs = args[0];
		}

		public override object Invoke(ExpressionContext ctx) {
			if (!_started) {
				_started = true;
				_actualWaitSecs = Convert.ToSingle(Expression.Evaluate(_waitSecs, ctx));
				_startWaitTime = DateTime.Now;
			}

			var deltaTime = DateTime.Now - _startWaitTime;
			if (deltaTime.TotalSeconds > _actualWaitSecs)
				return NodeState.Success;
			else
				return NodeState.Running;
		}

		public override void Reset() {
			_started = false;
		}
	}

}