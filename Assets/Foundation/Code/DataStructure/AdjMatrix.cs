
using System;
using System.IO;

/// <summary>
/// 简单的对称邻接矩阵
/// </summary>
public abstract class AdjMatrix<T> {
	private T[] data;
	private int dimension;

	public AdjMatrix(int dimension) {
		this.dimension = dimension;
		data = new T[(dimension * (dimension - 1)) / 2];
	}

	public AdjMatrix(byte[] bytes) {
		using (var reader = new BinaryReader(new MemoryStream(bytes))) {
			var dim = reader.ReadByte();
			dimension = dim;

			data = new T[(dimension * (dimension - 1)) / 2];
			for (int i = 0; i < data.Length; ++i)
				data[i] = ElementFromBytes(reader);
		}
	}

	private int CvtIndex(int i, int j, out bool isInverse) {
		XDebug.Assert(i < dimension && j < dimension);
		if (i > j) {
			int tmp = i;
			i = j;
			j = tmp;
			isInverse = true;
		} else {
			isInverse = false;
		}

		int baseIdx = (i * (2 * dimension - 1 - i)) / 2;
		// dim=5 i=1: 1 * (10 - 2) / 2 = 4
		// dim=5 i=2: 2 * (10 - 3) / 2 = 7

		return baseIdx + (j - i - 1);
	}

	public T this[int i, int j] {
		get {
			var idx = CvtIndex(i, j, out var inverse);
			if (inverse)
				return Inverse(data[idx]);
			else
				return data[idx];
		}
		set {
			var idx = CvtIndex(i, j, out var inverse);
			if (inverse)
				data[idx] = Inverse(value);
			else
				data[idx] = value;
		}
	}

	public void Fill(T value) {
		ArrayUtil.Fill(data, value);
	}

	public void ToBytes(BinaryWriter writer) {
		writer.Write((byte) dimension);
		for (int i = 0; i < data.Length; ++i)
			ElementToBytes(data[i], writer);
	}

	public virtual T Inverse(T elem) {
		return elem;
	}

	public abstract void ElementToBytes(T elem, BinaryWriter writer);
	public abstract T ElementFromBytes(BinaryReader reader);
}

public class EnumAdjMatrix<T> : AdjMatrix<T> where T : Enum {
	public EnumAdjMatrix(int dimension) : base(dimension) { }
	public EnumAdjMatrix(byte[] bytes) : base(bytes) { }
	public override void ElementToBytes(T elem, BinaryWriter writer) {
		writer.Write(Convert.ToInt32(elem));
	}
	public override T ElementFromBytes(BinaryReader reader) {
		return (T) Enum.ToObject(typeof(T), reader.ReadInt32());
	}
}

public class FloatAdjMatrix : AdjMatrix<float> {
	public FloatAdjMatrix(int dimension) : base(dimension) { }
	public FloatAdjMatrix(byte[] bytes) : base(bytes) { }
	public override void ElementToBytes(float elem, BinaryWriter writer) {
		writer.Write(elem);
	}
	public override float ElementFromBytes(BinaryReader reader) {
		return reader.ReadSingle();
	}
}