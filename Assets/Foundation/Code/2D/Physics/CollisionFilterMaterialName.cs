using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollisionFilterMaterialName : CollisionFilter {
    public string[] AcceptTags = new string[0];
    public string[] DenyTags = new string[0];

    private static readonly Dictionary<string, string[]> _TagsCache = new Dictionary<string, string[]>();

    public override bool Accepts(Collider2D collider) {
        if (collider.sharedMaterial == null)
            return false;

        var name = collider.sharedMaterial.name;
        if (!_TagsCache.ContainsKey(name)) {
            _TagsCache.Add(name, name.Split('_'));
        }

        var tags = _TagsCache[name];
        return tags.Any(x => AcceptTags.Contains(x)) && !tags.Any(x => DenyTags.Contains(x));
    }
}
