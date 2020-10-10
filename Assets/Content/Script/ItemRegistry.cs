using System.Collections.Generic;
using SPlay;

public partial class ItemRegistry {
    private readonly Dictionary<string, Item> _allItems = new Dictionary<string, Item>();

    public void Add(Item item) {
        if (!string.IsNullOrEmpty(item.onUse)) {
            item.onUseExpr = ExpressionParser.Parse(item.onUse);
        }
        _allItems.Add(item.id, item);
    }

    public Item GetItem(string id) {
        _allItems.TryGetValue(id, out var ret);
        return ret;
    }
}