
using System;

public struct TextPos : IComparable<TextPos> {
    public int Line;
    public int Column;

    public override string ToString() {
        return $"{Line}-{Column}";
    }

    public int CompareTo(TextPos other) {
        var lineComparison = Line.CompareTo(other.Line);
        if (lineComparison != 0) return lineComparison;
        return Column.CompareTo(other.Column);
    }

    public static bool operator <(TextPos left, TextPos right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(TextPos left, TextPos right) {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(TextPos left, TextPos right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(TextPos left, TextPos right) {
        return left.CompareTo(right) >= 0;
    }
}

public struct TextRange {
    public TextPos begin;
    public TextPos end; // exclusive

    public TextRange(TextPos b, TextPos e) {
        begin = b;
        end = e;
    }

    public override string ToString() {
        return $"r({begin}->{end})";
    }

    public bool Contains(TextPos p) {
        return begin <= p && p <= end;
    }

}
