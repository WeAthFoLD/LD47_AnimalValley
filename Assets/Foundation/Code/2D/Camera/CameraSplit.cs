using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CameraSplit : MonoBehaviour {

	public enum Axis { Horizontal, Vertical }

	[Flags]
	public enum LimitMode {
		Positive = 1, Negative = 2, All = 3
	}

	public Axis axis;
	public LimitMode limitMode = LimitMode.All;
	
	public float length;

	public float blendWidth;

	public int priority;

	public bool softEdge;

	[ShowIf("softEdge")]
	public float softEdgeFactor = 0.3f;

	bool playerInPositiveAxis;
	bool playerTransitionProtect = false;

	void OnEnable() {
		StartCoroutine(ActionUpdate());
	}

	void OnDisable() {
		MainCamera.instance.RemoveCameraSplit(this);
	}

	IEnumerator ActionUpdate() {
		MainCamera.instance.AddCameraSplit(this);
		playerInPositiveAxis = IsRawInPositiveAxis(0);
		playerTransitionProtect = true;
		yield return null;
		while (enabled && MainCamera.instance.cameraSplitTarget) {
			if (!playerTransitionProtect) {
				var lastVal = playerInPositiveAxis;
				if (playerInPositiveAxis) {
					playerInPositiveAxis = IsRawInPositiveAxis(blendWidth);
				} else {
					playerInPositiveAxis = IsRawInPositiveAxis(-blendWidth);
				}
				if (lastVal != playerInPositiveAxis) {
					playerTransitionProtect = true;
				}
			} else {
				playerTransitionProtect = !IsRawInPositiveAxis(blendWidth) && IsRawInPositiveAxis(-blendWidth);
			}
			yield return null;
		}
	}

	public Vector2 TryLimit(Vector2 cameraPos, Vector2 halfSize) {
		if (playerInPositiveAxis && (limitMode & LimitMode.Positive) == 0)
			return cameraPos;
		if (!playerInPositiveAxis && (limitMode & LimitMode.Negative) == 0)
			return cameraPos;

		if (axis == Axis.Horizontal) {
			float prog = (cameraPos.x - transform.position.x) / (length / 2);
			if (prog > 1 || prog < -1) {
				return cameraPos;
			}

			float blend = softEdge ? SoftEdgeBlend(prog) : 1.0f;
			if (playerInPositiveAxis) { // limit up
				cameraPos.y = Mathf.Lerp(cameraPos.y, Mathf.Max(transform.position.y + halfSize.y - blendWidth, cameraPos.y), blend);
			} else { // limit down 
				cameraPos.y = Mathf.Lerp(cameraPos.y, Mathf.Min(transform.position.y - halfSize.y + blendWidth, cameraPos.y), blend);
			}
			return cameraPos;
		} else {
			if (cameraPos.y + halfSize.y < transform.position.y - length / 2)
				return cameraPos;
			if (cameraPos.y - halfSize.y > transform.position.y + length / 2)
				return cameraPos;
			
			if (playerInPositiveAxis) { // limit right
				cameraPos.x = Mathf.Max(transform.position.x + halfSize.x - blendWidth, cameraPos.x);
			} else { // limit left
				cameraPos.x = Mathf.Min(transform.position.x - halfSize.x + blendWidth, cameraPos.x);
			}
			return cameraPos;
		}
	}

	float SoftEdgeBlend(float prog) {
		prog = Mathf.Abs(prog);
		if (prog < 1 - softEdgeFactor) {
			return 1;
		} else {
			return Mathf.SmoothStep(0, 1, 1 - ( prog - (1 - softEdgeFactor) ) / softEdgeFactor );
		}
	}

	bool IsRawInPositiveAxis(float offset) {
		var player = MainCamera.instance.cameraSplitTarget;
		float pos;
		float lim;
		if (axis == Axis.Horizontal) {
			pos = player.position.y;
			lim = transform.position.y + offset;
		} else {
			pos = player.position.x;
			lim = transform.position.x + offset;
		}
		return pos > lim;
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.blue;
		DrawLine(0);
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.green;
		DrawLine(-blendWidth);
		DrawLine(blendWidth);
	}

	void DrawLine(float offset) {
		var p = (Vector2) transform.position;
		var hl = length / 2;
		if (axis == Axis.Horizontal) {
			p += Vector2.up * offset;
			Gizmos.DrawLine(p + Vector2.right * hl, p + Vector2.left * hl);
		} else {
			p += Vector2.right * offset;
			Gizmos.DrawLine(p + Vector2.down * hl, p + Vector2.up * hl);
		}
	}

	#if UNITY_EDITOR

	void OnValidate() {
		var view = UnityEditor.SceneView.lastActiveSceneView;
		if (view) view.Repaint();
	}

	#endif

}
