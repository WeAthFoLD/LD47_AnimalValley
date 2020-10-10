using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

enum FoodPlantState {
    Normal,
    Harvested,
    Dead
}

public class MonoFoodPlant : MonoListenEventBehaviour {

    public SpriteRenderer sr;
    public Sprite normalSprite, harvestedSprite, deadSprite;
    public float recoverTimeMin, recoverTimeMax;
    public PollutionState maxPollutionState;

    private FoodPlantState _state;
    private float _stateElapsed;
    private float _harvestRecoverTime;

    private void Awake() {
        _Transit(FoodPlantState.Normal);
    }

    void Update() {
        _stateElapsed += Time.deltaTime;
        if (_state == FoodPlantState.Harvested && _stateElapsed > _harvestRecoverTime) {
            _Transit(FoodPlantState.Normal);
        }

        if (_state != FoodPlantState.Dead && GameContext.Instance.pollutionState > maxPollutionState) {
            _Transit(FoodPlantState.Dead);
        }
    }

    private void _Transit(FoodPlantState newState) {
        _state = newState;
        _stateElapsed = 0f;

        if (_state == FoodPlantState.Harvested)
            _harvestRecoverTime = Random.Range(recoverTimeMin, recoverTimeMax);

        if (newState == FoodPlantState.Normal)
            sr.sprite = normalSprite;
        else if (newState == FoodPlantState.Harvested)
            sr.sprite = harvestedSprite;
        else
            sr.sprite = deadSprite;

        GetComponent<MonoInteractable>().enabled = newState == FoodPlantState.Normal;
    }

    [SubscribeEvent]
    private void _OnInteract(InteractEvent evt) {
        if (evt.interactable.gameObject == gameObject) {
            if (_state == FoodPlantState.Normal) {
                _Transit(FoodPlantState.Harvested);
            }
        }
    }
}
