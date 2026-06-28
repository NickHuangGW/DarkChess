namespace DarkChess.Core;

public enum Color
{
    Red,
    Black,
}

/// <summary>棋子種類，數值即為階級大小（7 最大、1 最小）。</summary>
public enum PieceKind
{
    Soldier = 1, // 兵 / 卒
    Cannon = 2,  // 炮 / 包
    Horse = 3,   // 傌 / 馬
    Chariot = 4, // 俥 / 車
    Elephant = 5,// 相 / 象
    Advisor = 6, // 仕 / 士
    General = 7, // 帥 / 將
}

public readonly record struct Piece(Color Color, PieceKind Kind)
{
    public int Rank => (int)Kind;

    /// <summary>顯示用中文字（依顏色取紅或黑的字面）。</summary>
    public string Glyph => Kind switch
    {
        PieceKind.General => Color == Color.Red ? "帥" : "將",
        PieceKind.Advisor => Color == Color.Red ? "仕" : "士",
        PieceKind.Elephant => Color == Color.Red ? "相" : "象",
        PieceKind.Chariot => Color == Color.Red ? "俥" : "車",
        PieceKind.Horse => Color == Color.Red ? "傌" : "馬",
        PieceKind.Cannon => Color == Color.Red ? "炮" : "包",
        PieceKind.Soldier => Color == Color.Red ? "兵" : "卒",
        _ => "?",
    };

    /// <summary>
    /// 一般吃子大小規則（不含炮的跳吃）：高階吃低階、同階互吃。
    /// 例外：兵/卒(1) 可吃 將/帥(7)；將/帥 不能吃 兵/卒。
    /// </summary>
    public bool CanCaptureByRank(Piece target)
    {
        if (Color == target.Color) return false;

        // 兵吃帥的例外
        if (Kind == PieceKind.Soldier && target.Kind == PieceKind.General) return true;
        if (Kind == PieceKind.General && target.Kind == PieceKind.Soldier) return false;

        return Rank >= target.Rank;
    }

    /// <summary>建立一副棋（紅黑各 16 子），尚未洗牌。</summary>
    public static List<Piece> FullSet()
    {
        var counts = new (PieceKind kind, int n)[]
        {
            (PieceKind.General, 1),
            (PieceKind.Advisor, 2),
            (PieceKind.Elephant, 2),
            (PieceKind.Chariot, 2),
            (PieceKind.Horse, 2),
            (PieceKind.Cannon, 2),
            (PieceKind.Soldier, 5),
        };

        var pieces = new List<Piece>(32);
        foreach (var color in new[] { Color.Red, Color.Black })
            foreach (var (kind, n) in counts)
                for (var i = 0; i < n; i++)
                    pieces.Add(new Piece(color, kind));

        return pieces;
    }
}
