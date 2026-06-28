namespace DarkChess.UI;

/// <summary>
/// 棋盤一格的顯示資料（不可變 DTO）。
/// </summary>
public sealed record CellViewModel(
    int Index,
    bool IsEmpty,
    bool IsHidden,
    PieceViewModel? Piece
);
