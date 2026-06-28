using DarkChess.Core;

namespace DarkChess.UI.Presenter;

/// <summary>
/// 遊戲橋接層（Presenter）。
/// 嚴格遵守 Passive View (MVP) 模式：
///   - 訂閱 IBoardView / IHUDView 的事件
///   - 呼叫 GameStateMachine 執行邏輯
///   - 根據 Core 結果驅動 View 進行畫面更新
/// 本類不含任何 Unity / Raylib 依賴，可直接 xUnit 測試。
/// </summary>
public sealed class GamePresenter
{
    private readonly IBoardView _boardView;
    private readonly IHUDView _hudView;
    private readonly GameStateMachine _sm;

    private BotPlayer? _bot;
    private int _selectedIndex = -1;   // 目前選取的格子，-1 = 無
    private bool _animating;           // 動畫播放中，不接受新輸入

    // ── 建構 ─────────────────────────────────────────────────────────────

    public GamePresenter(IBoardView boardView, IHUDView hudView,
                         GameStateMachine? stateMachine = null)
    {
        _boardView = boardView;
        _hudView = hudView;
        _sm = stateMachine ?? new GameStateMachine();

        _boardView.OnCellClicked += HandleCellClicked;
        _hudView.OnNewGameRequested += HandleNewGameRequested;
        _hudView.OnModeSelected += HandleModeSelected;

        _hudView.ShowModeSelection();
    }

    // ── 公開 API ──────────────────────────────────────────────────────────

    /// <summary>設定 Bot 玩家（null = PvP 模式）。</summary>
    public void SetBot(BotPlayer? bot) => _bot = bot;

    /// <summary>
    /// 檢查是否輪到 Bot，若是則執行 Bot 行動。
    /// 應由外層 Update / Timer 每幀呼叫。
    /// </summary>
    public void TriggerBotActionIfNeeded()
    {
        if (_bot is null || _animating) return;
        if (_sm.State == GameState.GameOver) return;

        // 判斷是否為 Bot 回合
        bool isBotTurn = _sm.State == GameState.WaitingFirstFlip
            || _sm.Game.CurrentColor == _bot.BotColor;

        if (!isBotTurn) return;

        _animating = true;
        var action = _bot.ChooseAction(_sm.Game);

        if (action.Kind == ActionKind.Flip)
        {
            _boardView.PlayFlipAnimation(action.From, () =>
            {
                _sm.ApplyAction(action);
                _boardView.ShowCell(action.From, BuildCellVm(action.From));
                _animating = false;
                RefreshHUD();
            });
        }
        else
        {
            var isCapture = !_sm.Game.Board[action.To].IsEmpty;

            void AfterAnim()
            {
                _sm.ApplyAction(action);
                _boardView.ShowCell(action.From, BuildCellVm(action.From));
                _boardView.ShowCell(action.To, BuildCellVm(action.To));
                _animating = false;
                RefreshHUD();
            }

            if (isCapture)
                _boardView.PlayCaptureAnimation(action.From, action.To, AfterAnim);
            else
                _boardView.PlayMoveAnimation(action.From, action.To, AfterAnim);
        }
    }

    // ── 內部事件處理 ──────────────────────────────────────────────────────

    private void HandleCellClicked(int index)
    {
        if (_animating) return;
        if (_sm.State == GameState.GameOver) return;

        // 輪到 Bot 時不接受玩家輸入
        if (_bot is not null && _sm.Game.CurrentColor == _bot.BotColor) return;

        var cell = _sm.Game.Board[index];

        // ①  已選子：點到合法目的地 → 移動或吃子
        if (_selectedIndex >= 0)
        {
            var legalMoves = _sm.Game.LegalMovesFrom(_selectedIndex);
            var target = legalMoves.FirstOrDefault(m => m.To == index);

            if (target.To == index)
            {
                _boardView.ClearHighlights();
                var from = _selectedIndex;
                _selectedIndex = -1;
                _animating = true;

                void AfterAnim()
                {
                    _sm.ApplyAction(new GameAction(ActionKind.Move, from, index));
                    _boardView.ShowCell(from, BuildCellVm(from));
                    _boardView.ShowCell(index, BuildCellVm(index));
                    _animating = false;
                    RefreshHUD();
                }

                if (target.Kind == MoveKind.Capture)
                    _boardView.PlayCaptureAnimation(from, index, AfterAnim);
                else
                    _boardView.PlayMoveAnimation(from, index, AfterAnim);
                return;
            }

            // 點到其他地方 → 取消選取
            _boardView.ClearHighlights();
            _selectedIndex = -1;
        }

        // ②  點蓋著的子 → 翻棋
        if (cell.IsHidden)
        {
            _animating = true;
            _boardView.PlayFlipAnimation(index, () =>
            {
                _sm.ApplyAction(new GameAction(ActionKind.Flip, index));
                _boardView.ShowCell(index, BuildCellVm(index));
                _animating = false;
                RefreshHUD();
            });
            return;
        }

        // ③  點已翻開的己方子 → 選取並顯示合法步
        if (!cell.IsEmpty && cell.FaceUp)
        {
            var currentTurn = _sm.Game.CurrentColor;
            if (currentTurn is null || cell.Piece!.Value.Color == currentTurn)
            {
                _selectedIndex = index;
                var legal = _sm.Game.LegalMovesFrom(index);
                var moveTargets = legal.Where(m => m.Kind == MoveKind.Move).Select(m => m.To).ToList();
                var captureTargets = legal.Where(m => m.Kind == MoveKind.Capture).Select(m => m.To).ToList();

                _boardView.HighlightSelected(index);
                _boardView.ShowLegalMoves(moveTargets);
                _boardView.ShowLegalCaptures(captureTargets);
            }
        }
    }

    private void HandleNewGameRequested()
    {
        _sm.Reset();
        _bot = null;
        _selectedIndex = -1;
        _animating = false;
        _boardView.ClearHighlights();
        _boardView.RefreshAll(BuildAllCellVms());
        _hudView.ShowModeSelection();
    }

    private void HandleModeSelected(GameModeRequest mode)
    {
        _sm.Reset();
        _selectedIndex = -1;
        _animating = false;

        if (mode == GameModeRequest.PvBot)
            _bot = new BotPlayer(Color.Black, searchDepth: 3);
        else
            _bot = null;

        _hudView.HideModeSelection();
        _boardView.RefreshAll(BuildAllCellVms());
        _hudView.UpdateTurnIndicator("請翻棋開始", false);
        _hudView.ShowMessage("點任一格翻棋開始");
    }

    // ── 輔助：HUD 刷新 ────────────────────────────────────────────────────

    private void RefreshHUD()
    {
        var game = _sm.Game;

        if (game.Winner is { } winner)
        {
            _hudView.ShowGameOver(Banqi.ColorName(winner));
            return;
        }

        if (!game.FirstMoveDone)
        {
            _hudView.UpdateTurnIndicator("請翻棋開始", false);
        }
        else
        {
            var colorName = Banqi.ColorName(game.CurrentColor!.Value);
            var isBotTurn = _bot is not null && game.CurrentColor == _bot.BotColor;
            _hudView.UpdateTurnIndicator(colorName, isBotTurn);
        }

        _hudView.ShowMessage(game.Message);
    }

    // ── 輔助：ViewModel 建構 ─────────────────────────────────────────────

    private CellViewModel BuildCellVm(int index)
    {
        var cell = _sm.Game.Board[index];

        if (cell.IsEmpty)
            return new CellViewModel(index, IsEmpty: true, IsHidden: false, Piece: null);

        if (!cell.FaceUp)
            return new CellViewModel(index, IsEmpty: false, IsHidden: true, Piece: null);

        var p = cell.Piece!.Value;
        var pieceVm = new PieceViewModel(p.Color, p.Kind, p.Glyph);
        return new CellViewModel(index, IsEmpty: false, IsHidden: false, Piece: pieceVm);
    }

    private IReadOnlyList<CellViewModel> BuildAllCellVms()
    {
        var result = new CellViewModel[Banqi.Count];
        for (var i = 0; i < Banqi.Count; i++)
            result[i] = BuildCellVm(i);
        return result;
    }
}
