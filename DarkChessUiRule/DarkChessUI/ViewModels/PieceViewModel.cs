namespace DarkChess.UI;

/// <summary>
/// 棋子顯示資料（不可變 DTO，不含任何 Core 邏輯）。
/// </summary>
public sealed record PieceViewModel(
    DarkChess.Core.Color Color,
    DarkChess.Core.PieceKind Kind,
    string Glyph
);
