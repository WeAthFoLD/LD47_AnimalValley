using UnityEngine;

public abstract class MonoListenEventBehaviour : MonoBehaviour {

	protected virtual void OnEnable() {
		// XDebug.Log("OnEnable " + this);
		EventBus.Attach(this);
	}

	protected virtual void OnDisable() {
		// XDebug.Log("OnDisable" + this);
		EventBus.Detach(this);
	}

}
