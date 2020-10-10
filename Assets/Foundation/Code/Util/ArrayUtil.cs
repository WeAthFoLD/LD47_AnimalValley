
public static class ArrayUtil {

	public static void Fill<T>(T[] array, T value) {
		for (int i = 0; i < array.Length; ++i)
			array[i] = value;
	}

}
