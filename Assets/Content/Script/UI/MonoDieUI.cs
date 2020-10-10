using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoDieUI : MonoListenEventBehaviour {
    private bool _enableRestart = false;

    private Animator _animator;

    private void Awake() {
        _animator = GetComponent<Animator>();
    }

    private void Update() {
        if (_enableRestart && Input.anyKeyDown)
            SceneManager.LoadScene("Main");
    }

    [SubscribeEvent]
    void _OnDie(PlayerDieEvent evt) {
        _animator.Play("Die");
    }

    public void AnimEnableRestart() {
        _enableRestart = true;
    }

}
