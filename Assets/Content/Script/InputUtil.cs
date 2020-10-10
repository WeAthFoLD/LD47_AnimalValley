using Rewired;

public static class InputUtil {
	public static Player Player {
		get {
			if (ReInput.isReady)
				return ReInput.players.GetPlayer(0);
			return null;
		}
	}

	public static int AxisX { get; private set; }
	public static int AxisY { get; private set; }
	public static int Inventory { get; private set; }
	public static int Interact { get; private set; }
	public static int Attack { get; private set; }

	public static void Init() {
		var mapping = ReInput.mapping;
		AxisX = mapping.GetActionId("Horizontal");
		AxisY = mapping.GetActionId("Vertical");
		Interact = mapping.GetActionId("Interact");
		Attack = mapping.GetActionId("Attack");
		Inventory = mapping.GetActionId("Inventory");
	}

}
