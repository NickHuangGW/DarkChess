namespace ConsoleApp1;

/// <summary>棋盤上一格的狀態。</summary>
public sealed class Cell
{
    public Piece? Piece;     // null = 空格
    public bool FaceUp;      // 是否已翻開（蓋著時 Piece 仍有值，只是不可見）

    public bool IsEmpty => Piece is null;
    public bool IsHidden => Piece is not null && !FaceUp;
}

public enum MoveKind
{
    Flip,   // 翻棋
    Move,   // 移動到空格
    Capture // 吃子（含炮跳吃）
}

public readonly record struct Move(MoveKind Kind, int From, int To);

/// <summary>暗棋（翻翻棋）核心邏輯，與繪圖無關，可單獨測試。</summary>
public sealed class Banqi
{
    public const int Rows = 4;
    public const int Cols = 8;
    public const int Count = Rows * Cols;

    public readonly Cell[] Board = new Cell[Count];

    /// <summary>各方顏色，第一手翻棋前為 null（尚未決定）。</summary>
    public Color? RedSidePlayer; // 不直接用，保留擴充
    public Color? CurrentColor;  // 輪到此色行動；首手前為 null 表示「誰都可翻、由翻到的色決定」
    public bool FirstMoveDone;
    public Color? Winner;
    public string Message = "點任一格翻棋開始（第一手翻到的顏色即為你方）";

    private readonly Random _rng;

    public Banqi(int? seed = null)
    {
        _rng = seed is null ? new Random() : new Random(seed.Value);
        Reset();
    }

    public static (int row, int col) RowCol(int i) => (i / Cols, i % Cols);
    public static int Index(int row, int col) => row * Cols + col;
    public static bool InBounds(int row, int col) => row >= 0 && row < Rows && col >= 0 && col < Cols;

    public void Reset()
    {
        var pieces = Piece.FullSet();
        // Fisher–Yates 洗牌
        for (var i = pieces.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (pieces[i], pieces[j]) = (pieces[j], pieces[i]);
        }

        for (var i = 0; i < Count; i++)
            Board[i] = new Cell { Piece = pieces[i], FaceUp = false };

        CurrentColor = null;
        FirstMoveDone = false;
        Winner = null;
        Message = "點任一格翻棋開始（第一手翻到的顏色即為你方）";
    }

    public Color? TurnColor => CurrentColor;

    /// <summary>是否可翻開該格。</summary>
    public bool CanFlip(int i) => Winner is null && Board[i].IsHidden;

    /// <summary>翻開棋子；首手決定雙方顏色。</summary>
    public bool Flip(int i)
    {
        if (!CanFlip(i)) return false;

        Board[i].FaceUp = true;
        var revealed = Board[i].Piece!.Value;

        if (!FirstMoveDone)
        {
            // 第一手翻棋者取得翻到的顏色，接著換對方
            FirstMoveDone = true;
            CurrentColor = Opposite(revealed.Color);
            Message = $"先手為 {ColorName(revealed.Color)}，換 {ColorName(CurrentColor.Value)} 行動";
        }
        else
        {
            CurrentColor = Opposite(CurrentColor!.Value);
            Message = $"換 {ColorName(CurrentColor.Value)} 行動";
        }

        CheckWinner();
        return true;
    }

    /// <summary>嘗試從 from 移動/吃子到 to。回傳是否成功。</summary>
    public bool TryMove(int from, int to)
    {
        if (Winner is not null) return false;
        if (!IsLegalMove(from, to)) return false;

        var attacker = Board[from].Piece!.Value;
        var captured = Board[to].Piece;

        Board[to].Piece = attacker;
        Board[to].FaceUp = true;
        Board[from].Piece = null;
        Board[from].FaceUp = false;

        Message = captured is null
            ? $"{ColorName(attacker.Color)} 移動"
            : $"{ColorName(attacker.Color)} {attacker.Glyph} 吃 {captured.Value.Glyph}";

        CurrentColor = Opposite(CurrentColor!.Value);
        CheckWinner();
        return true;
    }

    /// <summary>判斷一步移動/吃子是否合法（不含翻棋）。</summary>
    public bool IsLegalMove(int from, int to)
    {
        if (from < 0 || from >= Count || to < 0 || to >= Count || from == to) return false;

        var src = Board[from];
        if (src.IsEmpty || !src.FaceUp) return false;

        var piece = src.Piece!.Value;
        if (CurrentColor is not null && piece.Color != CurrentColor) return false;

        var dst = Board[to];

        if (piece.Kind == PieceKind.Cannon)
            return IsLegalCannon(from, to, piece, dst);

        // 非炮：只能走相鄰一格
        if (!IsAdjacent(from, to)) return false;

        if (dst.IsEmpty) return true;             // 走空格
        if (!dst.FaceUp) return false;            // 不能吃蓋著的子
        return piece.CanCaptureByRank(dst.Piece!.Value); // 依大小吃子
    }

    /// <summary>炮的合法性：移動走一格空格；吃子需沿直線隔正好一個砲架。</summary>
    private bool IsLegalCannon(int from, int to, Piece cannon, Cell dst)
    {
        var (fr, fc) = RowCol(from);
        var (tr, tc) = RowCol(to);
        var sameLine = fr == tr || fc == tc;
        if (!sameLine) return false;

        if (dst.IsEmpty)
            // 炮不吃子時，與其他子相同：只走相鄰一格
            return IsAdjacent(from, to);

        // 吃子：目標需為翻開的敵子，且中間「砲架」數量恰為 1
        if (!dst.FaceUp) return false;
        if (dst.Piece!.Value.Color == cannon.Color) return false;
        return ScreensBetween(from, to) == 1;
    }

    /// <summary>計算 from→to 直線上中間（不含端點）非空格的數量。</summary>
    private int ScreensBetween(int from, int to)
    {
        var (fr, fc) = RowCol(from);
        var (tr, tc) = RowCol(to);
        var dr = Math.Sign(tr - fr);
        var dc = Math.Sign(tc - fc);

        var screens = 0;
        var r = fr + dr;
        var c = fc + dc;
        while (r != tr || c != tc)
        {
            if (!Board[Index(r, c)].IsEmpty) screens++;
            r += dr;
            c += dc;
        }
        return screens;
    }

    private static bool IsAdjacent(int a, int b)
    {
        var (ar, ac) = RowCol(a);
        var (br, bc) = RowCol(b);
        return Math.Abs(ar - br) + Math.Abs(ac - bc) == 1;
    }

    /// <summary>列出某格已翻開己方子的所有合法目的地。</summary>
    public List<Move> LegalMovesFrom(int from)
    {
        var moves = new List<Move>();
        var src = Board[from];
        if (src.IsEmpty || !src.FaceUp) return moves;
        if (CurrentColor is not null && src.Piece!.Value.Color != CurrentColor) return moves;

        for (var to = 0; to < Count; to++)
        {
            if (to == from) continue;
            if (IsLegalMove(from, to))
                moves.Add(new Move(Board[to].IsEmpty ? MoveKind.Move : MoveKind.Capture, from, to));
        }
        return moves;
    }

    /// <summary>輪到的一方是否還有任何合法行動（翻棋或走子）。</summary>
    public bool HasAnyMove(Color color)
    {
        for (var i = 0; i < Count; i++)
        {
            if (Board[i].IsHidden) return true; // 還有蓋著的子 → 可翻
            if (Board[i].FaceUp && Board[i].Piece!.Value.Color == color && LegalMovesFromIgnoringTurn(i, color).Count > 0)
                return true;
        }
        return false;
    }

    private List<Move> LegalMovesFromIgnoringTurn(int from, Color color)
    {
        var saved = CurrentColor;
        CurrentColor = color;
        var moves = LegalMovesFrom(from);
        CurrentColor = saved;
        return moves;
    }

    private void CheckWinner()
    {
        var redAlive = 0;
        var blackAlive = 0;
        for (var i = 0; i < Count; i++)
        {
            if (Board[i].IsEmpty) continue;
            if (Board[i].Piece!.Value.Color == Color.Red) redAlive++;
            else blackAlive++;
        }

        if (redAlive == 0) { Winner = Color.Black; Message = "黑方勝（紅子被吃光）"; return; }
        if (blackAlive == 0) { Winner = Color.Red; Message = "紅方勝（黑子被吃光）"; return; }

        // 輪到的一方無任何合法行動 → 對方勝
        if (FirstMoveDone && CurrentColor is { } turn && !HasAnyMove(turn))
        {
            Winner = Opposite(turn);
            Message = $"{ColorName(turn)} 無子可動，{ColorName(Winner.Value)} 勝";
        }
    }

    public static Color Opposite(Color c) => c == Color.Red ? Color.Black : Color.Red;
    public static string ColorName(Color c) => c == Color.Red ? "紅方" : "黑方";
}
