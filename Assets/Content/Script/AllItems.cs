public static class AllItems {

    public static Item
        food;

    public static void Init(ItemRegistry r) {
        food = r.GetItem("food");
    }

}
