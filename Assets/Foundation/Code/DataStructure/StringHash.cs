using System;
using System.Collections.Generic;

public struct StringHash : IEquatable<StringHash> {
	public readonly int hash;

	private StringHash(int h) {
		this.hash = h;
	}

	public unsafe StringHash(string s)
	{
		int hash1 = 5381;
		int hash2 = hash1;
		fixed (char* src = s)
		{
			int c;
			char* p = src;
			while ((c = p[0]) != 0)
			{
				hash1 = ((hash1 << 5) + hash1) ^ c;
				c = p[1];
				if (c == 0)
					break;
				hash2 = ((hash2 << 5) + hash2) ^ c;
				p += 2;
			}
		}
		var ret = hash1 + (hash2 * 1566083941);

		this.hash = ret;

		if (!_VisitedStrs.ContainsKey(this))
			_VisitedStrs.Add(new StringHash(hash), s);
	}

	public override bool Equals(object obj) {
		return obj is StringHash other && Equals(other);
	}

	public bool Equals(StringHash other) {
		return hash == other.hash;
	}

	public static bool operator ==(StringHash left, StringHash right) {
		return left.Equals(right);
	}

	public static bool operator !=(StringHash left, StringHash right) {
		return !left.Equals(right);
	}

	public override int GetHashCode() {
		return hash;
	}

	public override string ToString() {
		return $"StringHash({hash})";
	}

	// TODO: Debug only feature

	public string GetOriginalString() {
		if (!_VisitedStrs.TryGetValue(this, out var s)) {
			s = "<NOT FOUND>";
		}

		return s;
	}

	private static Dictionary<StringHash, string> _VisitedStrs = new Dictionary<StringHash, string>();
}
