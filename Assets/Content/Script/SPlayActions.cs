using System;
using SPlay;

public class LoadStage : ExpressionFunction {
    private StageName _name;
    private string _overrideSpawn;

    public override void Init(object[] args) {
        var result = StageName.TryParse((string) args[0], out _name);
        XDebug.Assert(result);

        if (args.Length > 1) {
            _overrideSpawn = args[1].ToString();
        }
    }

    public override object Invoke(ExpressionContext ctx) {
        StageManger.Instance.SwapStage(_name, _overrideSpawn);

        return null;
    }
}

public class AddHunger : ExpressionFunction {
    private float _add;

    public override void Init(object[] args) {
        _add = Convert.ToSingle(args[0]);
    }

    public override object Invoke(ExpressionContext ctx) {
        var p = GameContext.Instance.player.GetComponent<MonoPlayer>();
        p.hungerModule.AddHunger(_add);
        return null;
    }
}

public class AddItem : ExpressionFunction {
    private string _itemName;
    private int _count;

    public override void Init(object[] args) {
        _itemName = (string) args[0];
        _count = Convert.ToInt32(args[1]);
    }

    public override object Invoke(ExpressionContext ctx) {
        var item = GameContext.Instance.itemRegistry.GetItem(_itemName);
        var player = GameContext.Instance.player.GetComponent<MonoPlayer>();

        player.inventory.Add(new ItemStack(item, _count));
        EventBus.Post(new GeneralHintEvent { msg = $"Collected item {item.name} x{_count}", overrideDuration = 1.5f });

        return null;
    }
}

public class InstallVr : ExpressionFunction {
    public override void Init(object[] args) {
    }

    public override object Invoke(ExpressionContext ctx) {
        StageManger.Instance
            .GetStageInstance(StageName.Exterior)
            .transform
            .Find("VREquip")
            .gameObject.SetActive(true);

        EventBus.Post(new GeneralHintEvent { msg = "You have installed VR equipment in your basement." });

        return null;
    }
}

public class Hint : ExpressionFunction {
    private string _msg;

    public override void Init(object[] args) {
        _msg = args[0].ToString();
    }

    public override object Invoke(ExpressionContext ctx) {
        EventBus.Post(new GeneralHintEvent { msg = _msg });
        return null;
    }
}

public class SetEndstage : ExpressionFunction {
    public override void Init(object[] args) {
    }

    public override object Invoke(ExpressionContext ctx) {
        GameContext.Instance.SetGameFinal();
        return null;
    }
}
