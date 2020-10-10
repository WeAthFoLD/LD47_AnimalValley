using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlitchPostEffect : MonoBehaviour {

	public Material material;

	public float strength;

	public Texture2D noiseTex;

	Material _material;

	Dictionary<string, float> modifiers = new Dictionary<string, float>();

	private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");
	private static readonly int Strength = Shader.PropertyToID("_Strength");

	void OnRenderImage(RenderTexture src, RenderTexture dest) {
		if (!_material) {
			_material = Instantiate(material);
			material = null;
		}

		float finalStrength = strength;
		foreach (var val in modifiers.Values) {
			finalStrength += val;
		}

		if (Mathf.Approximately(finalStrength, 0f)) {
			Graphics.Blit(src, dest); // skip glitch to boost performance
		} else {
			_material.SetTexture(NoiseTex, noiseTex);
			_material.SetFloat(Strength, finalStrength);

			Graphics.Blit(src, dest, _material);
		}
	}

	void AddModifier(string id, float val) {
		modifiers.Remove(id);
		modifiers.Add(id, val);
	}

	void RemoveModifier(string id) {
		modifiers.Remove(id);
	}

	void OnEnable() {
		EventBus.Attach(this);
	}

	void OnDisable() {
		EventBus.Detach(this);
	}

	[SubscribeEvent]
	void OnStartPulse(GlitchPulseEvent ev) {
		float hd = ev.duration / 2;
		StartCoroutine(ActionPulse(ev.id, ev.strength, hd));
	}

	IEnumerator ActionPulse(string id, float str, float hd) {
		float elapsed = 0;
		while (elapsed < hd * 2) {
			elapsed += Time.deltaTime;

			float attn;
			if (elapsed < hd) {
				attn = Mathf.SmoothStep(0, 1, elapsed / hd);
			} else {
				attn = 1 - Mathf.SmoothStep(0, 1, (elapsed - hd) / hd);
			}

			AddModifier(id, attn * str);
			yield return null;
		}

		RemoveModifier(id);
	}

}
