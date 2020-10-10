
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Proj/AllItemConfig")]
public class AllItemConfig : ScriptableObject {
    [TableList]
    public Item[] items;
}
