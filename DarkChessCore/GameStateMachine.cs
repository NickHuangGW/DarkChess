namespace DarkChess.Core;

/// <summary>遊戲狀態。</summary>
public enum GameState
{
    WaitingFirstFlip, // 等待第一手翻棋（決定先手）
    RedTurn,          // 紅方回合
    BlackTurn,        // 黑方回合
    GameOver          // 遊戲結束
}

/// <summary>玩家行動類型。</summary>
public enum ActionKind
{
    Flip,   // 翻棋
    Move    // 移動/吃子
}

/// <summary>玩家行動。</summary>
public readonly record struct GameAction(ActionKind Kind, int From, int To = -1);

/// <summary>
/// 遊戲狀態機，管理回合流程與勝負判定。
/// 包裝 Banqi 的 Flip 與 TryMove，提供統一的 API。
/// </summary>
public sealed class GameStateMachine
{
    private readonly Banqi _game;
    
    public GameStateMachine(Banqi game)
    {
        _game = game;
    }

    public GameStateMachine(int? seed = null) : this(new Banqi(seed))
    {
    }

    /// <summary>取得遊戲實例（供查詢棋盤狀態）。</summary>
    public Banqi Game => _game;

    /// <summary>當前遊戲狀態。</summary>
    public GameState State
    {
        get
        {
            if (_game.Winner is not null) return GameState.GameOver;
            if (!_game.FirstMoveDone) return GameState.WaitingFirstFlip;
            return _game.CurrentColor == Color.Red ? GameState.RedTurn : GameState.BlackTurn;
        }
    }

    /// <summary>取得當前輪到的顏色（若遊戲結束或等待首手則為 null）。</summary>
    public Color? CurrentColor => State switch
    {
        GameState.RedTurn => Color.Red,
        GameState.BlackTurn => Color.Black,
        _ => null
    };

    /// <summary>
    /// 執行一個行動。
    /// </summary>
    /// <param name="action">行動內容。</param>
    /// <returns>是否成功執行。</returns>
    public bool ApplyAction(GameAction action)
    {
        return action.Kind switch
        {
            ActionKind.Flip => _game.Flip(action.From),
            ActionKind.Move => _game.TryMove(action.From, action.To),
            _ => false
        };
    }

    /// <summary>重置遊戲。</summary>
    public void Reset()
    {
        _game.Reset();
    }

    /// <summary>取得所有合法行動（含翻棋與走子）。</summary>
    public List<GameAction> GetLegalActions()
    {
        var actions = new List<GameAction>();

        // 翻棋
        for (var i = 0; i < Banqi.Count; i++)
        {
            if (_game.CanFlip(i))
                actions.Add(new GameAction(ActionKind.Flip, i));
        }

        // 走子/吃子
        if (_game.CurrentColor is { } color)
        {
            for (var from = 0; from < Banqi.Count; from++)
            {
                var cell = _game.Board[from];
                if (cell.IsEmpty || !cell.FaceUp) continue;
                if (cell.Piece!.Value.Color != color) continue;

                var moves = _game.LegalMovesFrom(from);
                foreach (var move in moves)
                    actions.Add(new GameAction(ActionKind.Move, move.From, move.To));
            }
        }

        return actions;
    }

    /// <summary>取得指定顏色的所有合法行動（忽略當前回合）。</summary>
    public List<GameAction> GetLegalActionsFor(Color color)
    {
        var actions = new List<GameAction>();

        // 若有蓋牌，可翻棋
        for (var i = 0; i < Banqi.Count; i++)
        {
            if (_game.Board[i].IsHidden)
                actions.Add(new GameAction(ActionKind.Flip, i));
        }

        // 走子/吃子
        for (var from = 0; from < Banqi.Count; from++)
        {
            var cell = _game.Board[from];
            if (cell.IsEmpty || !cell.FaceUp) continue;
            if (cell.Piece!.Value.Color != color) continue;

            // 暫存回合狀態
            var savedColor = _game.CurrentColor;
            _game.CurrentColor = color;
            var moves = _game.LegalMovesFrom(from);
            _game.CurrentColor = savedColor;

            foreach (var move in moves)
                actions.Add(new GameAction(ActionKind.Move, move.From, move.To));
        }

        return actions;
    }
}
