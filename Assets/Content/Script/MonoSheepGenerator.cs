using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSheepGenerator : MonoBehaviour {

    // 1. 过一定时间在玩家身边生成1只羊
    // 2. 羊一定不卡在碰撞里（通过和玩家的raycast达成）
    // 3. 如果羊离玩家太远，杀死羊，重新生成
    // 4. 羊不得超过k只

    public GameObject prefab;
    public int maxSheepCount;
    public float minGenerateTime = 60f;
    public float maxSheepPlayerDist;
    public FloatRange sheepDistRange;

    private bool _firstInit = true;
    private DateTime _lastGenerateTime;
    private List<GameObject> generatedSheep = new List<GameObject>();

    private void OnEnable() {
        if (_firstInit) {
            _firstInit = false;
            _lastGenerateTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(100));
        }
        StartCoroutine(CoroGenerateSheep());
    }

    IEnumerator CoroGenerateSheep() {
        yield return new WaitUntil(() => GameContext.Instance);
        while (GameContext.Instance.pollutionState == PollutionState.None) {
            yield return new WaitForSeconds(0.1f);

            var player = GameContext.Instance.player;
            foreach (var sheep in generatedSheep) {
                if (!sheep)
                    continue;
                if (Vector3.Distance(player.transform.position, sheep.transform.position) > maxSheepPlayerDist) {
                    Destroy(sheep);
                }
            }

            // 删除销毁的羊
            generatedSheep.RemoveAll(x => !x);

            // 尝试生成一个新的羊
            if (generatedSheep.Count < maxSheepCount &&
                (DateTime.Now - _lastGenerateTime).TotalSeconds > minGenerateTime) {

                var dist = sheepDistRange.random;
                var dir = UnityEngine.Random.insideUnitCircle;
                var dir3 = new Vector3(dir.x, 0, dir.y);

                if (!Physics.Raycast(player.transform.position + Vector3.up, dir3, dist,
                    LayerMaskUtil.GetLayerMask(0))) {
                    var npos = player.transform.position + dir3 * dist;
                    var sheepInstance = Instantiate(prefab, transform);
                    sheepInstance.transform.position = npos;

                    generatedSheep.Add(sheepInstance);
                    _lastGenerateTime = DateTime.Now;
                    XDebug.Log("Generated new sheep");
                } else {
                    XDebug.Log("Position failed");
                }
            }
        }
    }
}
