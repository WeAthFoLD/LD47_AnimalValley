using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraResizeArea : MonoBehaviour {

	public Rect resizeArea;
	public float margin;

	public float size;

	Camera cam;

	MainCamera mainCamera {
		get { return MainCamera.instance; }
	}

	IEnumerator Start () {
		yield return new WaitUntil(() => mainCamera);
		mainCamera.AddResizeArea(this);
		cam = mainCamera.GetComponent<Camera>();
	}

	void OnDisable() {
		mainCamera.RemoveResizeArea(this);
	}
	
	public float Resize(Vector2 cameraPos, float initialSize) {
		if (RectContains(resizeArea, cameraPos)) {
			return size;
		}

		Rect outerRect = GetOuterRect(cam.aspect);
		if (RectContains(outerRect, cameraPos)) {
			Vector2 d1 = cameraPos - (resizeArea.position - resizeArea.size / 2);
			Vector2 d2 = (resizeArea.position + resizeArea.size / 2) - cameraPos;
			float dx = Mathf.Min(d1.x, d2.x) / cam.aspect;
			float dy = Mathf.Min(d1.y, d2.y);

			float blend = Mathf.Min(dx, dy) / margin;
			return Mathf.SmoothStep(initialSize, size, blend);
		}
		return -1;
	}

	void OnDrawGizmosSelected() {
		DrawGizmoRect(resizeArea, Color.cyan);
		DrawGizmoRect(GetOuterRect(Camera.current.aspect), Color.white);
	}

	void DrawGizmoRect(Rect rect, Color color) {
		Gizmos.color = color;
		Gizmos.DrawWireCube((Vector3) rect.position + transform.position, rect.size);
	}

	bool RectContains(Rect rect, Vector2 point) {
		return Mathf.Abs(point.x - (rect.position.x + transform.position.x)) <= rect.width / 2 &&
			   Mathf.Abs(point.y - (rect.position.y + transform.position.y)) <= rect.height / 2;
	}

	Rect GetOuterRect(float aspect) {
		Rect outer = resizeArea;
		outer.width += 2 * margin * aspect;
		outer.height += 2 * margin;
		return outer;
	}

	#if UNITY_EDITOR

	void OnValidate() {
		var view = UnityEditor.SceneView.lastActiveSceneView;
		if (view) view.Repaint();
	}

	#endif

}
