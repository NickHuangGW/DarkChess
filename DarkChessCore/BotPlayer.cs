namespace DarkChess.Core;

/// <summary>
/// 電腦 AI 玩家對手，採用 Minimax 演算法與 Alpha-Beta 剪枝技術。
/// 決策優先級：贏得比賽 > 吃子 > 規避威脅 > 移動 > 翻棋。
/// </summary>
public sealed class BotPlayer
{
    private readonly Color _botColor;
    private readonly int _searchDepth;
    private readonly Random _rng;

    public BotPlayer(Color botColor, int searchDepth = 3, int? seed = null)
    {
        _botColor = botColor;
        _searchDepth = searchDepth;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>取得 Bot 所屬的陣營顏色 (紅/黑)</summary>
    public Color BotColor => _botColor;

    /// <summary>
    /// 根據當前盤面，選擇並回傳最佳的遊戲動作 (翻棋或移動)。
    /// </summary>
    public GameAction ChooseAction(Banqi game)
    {
        var actions = GetLegalActions(game, _botColor);
        if (actions.Count == 0)
            throw new InvalidOperationException("沒有合法的動作可以執行");

        if (actions.Count == 1)
            return actions[0];

        // 規則特例：如果是遊戲的第一步 (盤面全蓋牌)，隨機選擇一個位置翻棋
        if (!game.FirstMoveDone)
        {
            var flips = actions.Where(a => a.Kind == ActionKind.Flip).ToList();
            return flips[_rng.Next(flips.Count)];
        }

        // 初始化最佳動作與最佳分數
        var bestAction = actions[0];
        var bestScore = int.MinValue;

        // 啟發式搜尋排序：先排序動作以提升 Alpha-Beta 剪枝的效率
        var ordered = OrderActions(actions, game);

        foreach (var action in ordered)
        {
            // 模擬動作：複製盤面並執行該動作
            var cloned = CloneGame(game);
            ApplyAction(cloned, action);
            
            // 遞迴呼叫 Minimax 取得該動作的最終評估分數
            var score = Minimax(cloned, _searchDepth - 1, int.MinValue, int.MaxValue, false);

            // 分數更新邏輯：如果分數一樣，有 25% 的機率替換，藉此避免 AI 行為過於固定可預測
            if (score > bestScore || (score == bestScore && _rng.Next(4) == 0))
            {
                bestScore = score;
                bestAction = action;
            }
        }

        return bestAction;
    }

    /// <summary>
    /// 對可行動作進行排序。
    /// 排序邏輯：高價值吃子 > 一般移動 > 翻棋。
    /// 目的：讓 Alpha-Beta 剪枝能更快找到好樹枝，提早砍掉不必要的搜尋分支。
    /// </summary>
    private List<GameAction> OrderActions(List<GameAction> actions, Banqi game)
    {
        return actions.OrderByDescending(a =>
        {
            if (a.Kind == ActionKind.Flip) return -1; // 翻棋的優先度墊底

            // 如果是移動動作，檢查目標位置是否有敵方棋子 (判斷是否為吃子)
            var dst = game.Board[a.To];
            if (!dst.IsEmpty && dst.FaceUp && dst.Piece!.Value.Color != _botColor)
                return GetPieceValue(dst.Piece!.Value.Kind) * 10; // 優先嘗試吃掉高價值的敵子

            return 0; // 一般的平移移動
        }).ToList();
    }

    /// <summary>
    /// 核心演算法：Minimax 搭配 Alpha-Beta 剪枝
    /// </summary>
    private int Minimax(Banqi game, int depth, int alpha, int beta, bool maximizing)
    {
        // 抵達搜尋深度底層，或遊戲已分出勝負，則進行靜態盤面評估
        if (depth == 0 || game.Winner is not null)
            return Evaluate(game);

        var currentColor = maximizing ? _botColor : Banqi.Opposite(_botColor);
        var actions = GetLegalActions(game, currentColor);

        if (actions.Count == 0)
            return Evaluate(game);

        var ordered = OrderActionsForColor(actions, game, currentColor);

        if (maximizing)
        {
            var maxEval = int.MinValue;
            foreach (var action in ordered)
            {
                var cloned = CloneGame(game);
                ApplyAction(cloned, action);
                var eval = Minimax(cloned, depth - 1, alpha, beta, false);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha) break; // Beta 剪枝
            }
            return maxEval;
        }
        else
        {
            var minEval = int.MaxValue;
            foreach (var action in ordered)
            {
                var cloned = CloneGame(game);
                ApplyAction(cloned, action);
                var eval = Minimax(cloned, depth - 1, alpha, beta, true);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha) break; // Alpha 剪枝
            }
            return minEval;
        }
    }

    /// <summary>為特定顏色進行動作排序 (用於 Minimax 內部遞迴)</summary>
    private List<GameAction> OrderActionsForColor(List<GameAction> actions, Banqi game, Color color)
    {
        var opponent = Banqi.Opposite(color);
        return actions.OrderByDescending(a =>
        {
            if (a.Kind == ActionKind.Flip) return -1;
            var dst = game.Board[a.To];
            if (!dst.IsEmpty && dst.FaceUp && dst.Piece!.Value.Color == opponent)
                return GetPieceValue(dst.Piece!.Value.Kind) * 10;
            return 0;
        }).ToList();
    }

    /// <summary>
    /// 靜態盤面評估函數 (Evaluation Function)
    /// 計算公式 = (己方總分 - 敵方總分) - 己方受威脅扣分 + 暗棋的潛在期望值
    /// </summary>
    private int Evaluate(Banqi game)
    {
        if (game.Winner == _botColor) return 10000;
        if (game.Winner == Banqi.Opposite(_botColor)) return -10000;

        var opponent = Banqi.Opposite(_botColor);
        var myScore = 0;
        var opScore = 0;
        var dangerPenalty = 0;

        for (var i = 0; i < Banqi.Count; i++)
        {
            var cell = game.Board[i];
            if (cell.IsEmpty) continue;

            if (!cell.FaceUp)
            {
                // 注意：這裡給予未翻開的棋子固定的 +50 分，鼓勵 AI 推進遊戲
                myScore += 50; 
                continue;
            }

            var piece = cell.Piece!.Value;
            var val = GetPieceValue(piece.Kind);

            if (piece.Color == _botColor)
                myScore += val;
            else
                opScore += val;
        }

        // 威脅評估：模擬敵方回合，檢查己方棋子是否暴露在被吃的風險中
        var savedColor = game.CurrentColor;
        game.CurrentColor = opponent;
        for (var opFrom = 0; opFrom < Banqi.Count; opFrom++)
        {
            var opCell = game.Board[opFrom];
            if (opCell.IsEmpty || !opCell.FaceUp || opCell.Piece!.Value.Color != opponent) continue;

            var opMoves = game.LegalMovesFrom(opFrom);
            foreach (var m in opMoves)
            {
                var target = game.Board[m.To];
                if (!target.IsEmpty && target.FaceUp && target.Piece!.Value.Color == _botColor)
                {
                    // 己方棋子處於敵方攻擊範圍內，進行懲罰扣分 (取該棋子價值的一半)
                    dangerPenalty += GetPieceValue(target.Piece!.Value.Kind) / 2;
                }
            }
        }
        game.CurrentColor = savedColor; // 復原回合狀態

        return (myScore - opScore) - dangerPenalty;
    }

    /// <summary>取得各階級棋子的權重分數</summary>
    private static int GetPieceValue(PieceKind kind) => kind switch
    {
        PieceKind.General  => 1000,
        PieceKind.Advisor  => 600,
        PieceKind.Elephant => 500,
        PieceKind.Chariot  => 500,
        PieceKind.Horse    => 400,
        PieceKind.Cannon   => 450,
        PieceKind.Soldier  => 200,
        _ => 0
    };

    /// <summary>取得當前合法的所有動作 (包含翻棋與移動/吃子)</summary>
    private List<GameAction> GetLegalActions(Banqi game, Color color)
    {
        var actions = new List<GameAction>();

        // 1. 蒐集所有可以翻開的暗棋
        for (var i = 0; i < Banqi.Count; i++)
            if (game.Board[i].IsHidden)
                actions.Add(new GameAction(ActionKind.Flip, i));

        // 2. 蒐集己方所有明棋的可行移動與吃子路線
        if (game.CurrentColor is not null)
        {
            var savedColor = game.CurrentColor;
            game.CurrentColor = color;

            for (var from = 0; from < Banqi.Count; from++)
            {
                var cell = game.Board[from];
                if (cell.IsEmpty || !cell.FaceUp || cell.Piece!.Value.Color != color) continue;

                foreach (var move in game.LegalMovesFrom(from))
                    actions.Add(new GameAction(ActionKind.Move, move.From, move.To));
            }

            game.CurrentColor = savedColor;
        }

        return actions;
    }

    private static Banqi CloneGame(Banqi game)
    {
        var cloned = new Banqi(0);
        for (var i = 0; i < Banqi.Count; i++)
            cloned.Board[i] = new Cell { Piece = game.Board[i].Piece, FaceUp = game.Board[i].FaceUp };
        cloned.CurrentColor = game.CurrentColor;
        cloned.FirstMoveDone = game.FirstMoveDone;
        cloned.Winner = game.Winner;
        cloned.Message = game.Message;
        return cloned;
    }

    private static void ApplyAction(Banqi game, GameAction action)
    {
        if (action.Kind == ActionKind.Flip)
            game.Flip(action.From);
        else
            game.TryMove(action.From, action.To);
    }
}
