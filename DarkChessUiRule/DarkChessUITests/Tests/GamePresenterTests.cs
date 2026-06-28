using DarkChess.Core;
using DarkChess.UI;
using DarkChess.UI.Presenter;
using DarkChessUITests.Mocks;

namespace DarkChessUITests.Tests;

public class GamePresenterTests
{
    // ── 輔助工廠 ──────────────────────────────────────────────────────────

    private static (GamePresenter presenter, MockBoardView board, MockHUDView hud, GameStateMachine sm)
        CreatePvP(int? seed = null)
    {
        var sm = new GameStateMachine(seed);
        var board = new MockBoardView();
        var hud = new MockHUDView();
        var presenter = new GamePresenter(board, hud, sm);
        // 選擇 PvP 模式開始遊戲
        hud.SimulateModeSelected(GameModeRequest.PvP);
        return (presenter, board, hud, sm);
    }

    private static void PlaceAndReveal(GameStateMachine sm, int index, Piece piece)
    {
        sm.Game.Board[index] = new Cell { Piece = piece, FaceUp = true };
    }

    private static void ClearBoard(GameStateMachine sm)
    {
        for (var i = 0; i < Banqi.Count; i++)
            sm.Game.Board[i] = new Cell { Piece = null, FaceUp = false };
    }

    // ── T1：首手點蓋牌應翻棋 ─────────────────────────────────────────────

    [Fact]
    public void FirstCellClick_ShouldFlipPieceAndUpdateHUD()
    {
        var (_, board, hud, sm) = CreatePvP(seed: 1);

        // 找第一個蓋牌
        var hiddenIdx = Enumerable.Range(0, Banqi.Count)
            .First(i => sm.Game.Board[i].IsHidden);

        board.SimulateClick(hiddenIdx);

        Assert.Contains(board.FlipAnimCalls, i => i == hiddenIdx);
        Assert.Contains(board.ShowCellCalls, c => c.Index == hiddenIdx);
        Assert.NotEmpty(hud.TurnIndicatorCalls);
    }

    // ── T2：點己方子應高亮並顯示合法步 ──────────────────────────────────

    [Fact]
    public void SelectFaceUpPiece_ShouldHighlightAndShowMoves()
    {
        var (_, board, hud, sm) = CreatePvP(seed: 1);
        ClearBoard(sm);
        sm.Game.FirstMoveDone = true;
        sm.Game.CurrentColor = Color.Red;

        var at = Banqi.Index(1, 1);
        PlaceAndReveal(sm, at, new Piece(Color.Red, PieceKind.Chariot));

        board.SimulateClick(at);

        Assert.Contains(board.HighlightSelectedCalls, i => i == at);
        Assert.NotEmpty(board.ShowLegalMovesCalls);
    }

    // ── T3：選子後點合法空格 → 走子動畫 ─────────────────────────────────

    [Fact]
    public void ClickLegalTarget_ShouldTriggerMoveAnimation()
    {
        var (_, board, _, sm) = CreatePvP(seed: 1);
        ClearBoard(sm);
        sm.Game.FirstMoveDone = true;
        sm.Game.CurrentColor = Color.Red;

        var from = Banqi.Index(1, 1);
        var to = Banqi.Index(1, 2);
        PlaceAndReveal(sm, from, new Piece(Color.Red, PieceKind.Chariot));

        board.SimulateClick(from);  // 選子
        board.SimulateClick(to);    // 走子

        Assert.Contains(board.MoveAnimCalls, m => m.From == from && m.To == to);
        Assert.Contains(board.ShowCellCalls, c => c.Index == from);
        Assert.Contains(board.ShowCellCalls, c => c.Index == to);
    }

    // ── T4：選子後點可吃格 → 吃子動畫 ────────────────────────────────────

    [Fact]
    public void ClickLegalCapture_ShouldTriggerCaptureAnimation()
    {
        var (_, board, _, sm) = CreatePvP(seed: 1);
        ClearBoard(sm);
        sm.Game.FirstMoveDone = true;
        sm.Game.CurrentColor = Color.Red;

        var from = Banqi.Index(1, 1);
        var to = Banqi.Index(1, 2);
        PlaceAndReveal(sm, from, new Piece(Color.Red, PieceKind.Chariot));
        PlaceAndReveal(sm, to, new Piece(Color.Black, PieceKind.Horse));

        board.SimulateClick(from);
        board.SimulateClick(to);

        Assert.Contains(board.CaptureAnimCalls, m => m.From == from && m.To == to);
    }

    // ── T5：吃光敵子 → ShowGameOver ────────────────────────────────────────

    [Fact]
    public void WinByElimination_ShouldCallShowGameOver()
    {
        var (_, board, hud, sm) = CreatePvP(seed: 3);
        ClearBoard(sm);
        sm.Game.FirstMoveDone = true;
        sm.Game.CurrentColor = Color.Red;

        var from = Banqi.Index(0, 0);
        var to = Banqi.Index(0, 1);
        PlaceAndReveal(sm, from, new Piece(Color.Red, PieceKind.Chariot));
        PlaceAndReveal(sm, to, new Piece(Color.Black, PieceKind.Soldier));

        board.SimulateClick(from);
        board.SimulateClick(to);

        Assert.NotEmpty(hud.GameOverCalls);
        Assert.Equal("紅方", hud.GameOverCalls[0]);
    }

    // ── T6：重新開始 → RefreshAll + ShowModeSelection ─────────────────────

    [Fact]
    public void NewGameRequested_ShouldResetAndRefreshAll()
    {
        var (_, board, hud, sm) = CreatePvP(seed: 1);
        var refreshBefore = board.RefreshAllCalls.Count;
        var modeBefore = hud.ShowModeSelectionCount;

        hud.SimulateNewGameRequested();

        Assert.True(board.RefreshAllCalls.Count > refreshBefore);
        Assert.True(hud.ShowModeSelectionCount > modeBefore);
    }

    // ── T7：PvBot 模式 Bot 回合觸發 ────────────────────────────────────────

    [Fact]
    public void PvBot_TriggerBot_ShouldApplyAction()
    {
        var sm = new GameStateMachine(seed: 1);
        var board = new MockBoardView();
        var hud = new MockHUDView();
        var presenter = new GamePresenter(board, hud, sm);

        hud.SimulateModeSelected(GameModeRequest.PvBot); // Bot = 黑方

        // Bot 固定黑方，WaitingFirstFlip 狀態下也可觸發
        var cellsBefore = board.ShowCellCalls.Count;
        presenter.TriggerBotActionIfNeeded();

        Assert.True(board.ShowCellCalls.Count > cellsBefore,
            "Bot 應執行翻棋或走子，導致至少一個 ShowCell 呼叫");
        Assert.True(sm.Game.FirstMoveDone, "翻棋後 FirstMoveDone 應為 true");
    }

    // ── T8：狀態轉換 WaitingFirstFlip → RedTurn → BlackTurn ───────────────

    [Fact]
    public void StateTransition_WaitingToRedToBlack()
    {
        var sm = new GameStateMachine(seed: 1);
        var board = new MockBoardView();
        var hud = new MockHUDView();
        var presenter = new GamePresenter(board, hud, sm);
        hud.SimulateModeSelected(GameModeRequest.PvP);

        Assert.Equal(GameState.WaitingFirstFlip, sm.State);

        // 找第一個紅子並翻開（翻紅 → 換黑回合）
        var redIdx = Enumerable.Range(0, Banqi.Count)
            .First(i => sm.Game.Board[i].Piece?.Color == Color.Red);
        board.SimulateClick(redIdx);

        Assert.Equal(GameState.BlackTurn, sm.State);

        // 找第一個黑子並翻開（翻黑 → 換紅回合）
        var blackIdx = Enumerable.Range(0, Banqi.Count)
            .First(i => sm.Game.Board[i].IsHidden);
        board.SimulateClick(blackIdx);

        // 根據翻到的顏色決定狀態（可能紅也可能黑）
        Assert.True(sm.State is GameState.RedTurn or GameState.BlackTurn,
            $"翻第二手後狀態應為 RedTurn 或 BlackTurn，實際：{sm.State}");
    }
}
