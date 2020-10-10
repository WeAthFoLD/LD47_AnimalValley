using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CameraTargetChangeEvent {
	public readonly Transform target;
	public readonly bool resetPosition;

	public CameraTargetChangeEvent(Transform target, bool resetPosition = false) {
		this.target = target;
		this.resetPosition = resetPosition;
	}
}

public sealed class CameraPositionResetEvent {
}

public sealed class CameraShakeEvent {
	public float amp, freq, duration;
	public int octaves;

	public CameraShakeEvent(float amp, float freq, float duration, int octaves = 3) {
		this.amp = amp;
		this.freq = freq;
		this.duration = duration;
		this.octaves = octaves;
	}
}

public sealed class GlitchPulseEvent {
	public string id;
	public float strength, duration;

	public GlitchPulseEvent(string _id, float _str, float _dur) {
		id = _id;
		strength = _str;
		duration = _dur;
	}
}