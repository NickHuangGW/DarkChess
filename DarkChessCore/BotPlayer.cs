namespace DarkChess.Core;

/// <summary>
/// 電腦玩家，使用 Minimax + Alpha-Beta 剪枝。
/// </summary>
public sealed class BotPlayer
{
    private readonly Color _botColor;
    private readonly int _searchDepth;
    private readonly Random _rng;

    /// <summary>
    /// 建立電腦玩家。
    /// </summary>
    /// <param name="botColor">Bot 扮演的顏色。</param>
    /// <param name="searchDepth">搜尋深度（預設 3）。</param>
    /// <param name="seed">隨機種子（用於翻棋與同分選擇）。</param>
    public BotPlayer(Color botColor, int searchDepth = 3, int? seed = null)
    {
        _botColor = botColor;
        _searchDepth = searchDepth;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>Bot 扮演的顏色。</summary>
    public Color BotColor => _botColor;

    /// <summary>
    /// 選擇最佳行動。
    /// </summary>
    public GameAction ChooseAction(Banqi game)
    {
        var actions = GetLegalActions(game, _botColor);
        if (actions.Count == 0)
            throw new InvalidOperationException("無合法行動可執行");

        // 若只有一個行動，直接返回
        if (actions.Count == 1)
            return actions[0];

        // 優先翻棋的策略：若有蓋牌且無明顯優勢走法（對手或己方棋子數量懸殊），優先翻棋
        var flips = actions.Where(a => a.Kind == ActionKind.Flip).ToList();
        var moves = actions.Where(a => a.Kind == ActionKind.Move).ToList();

        if (flips.Count > 0 && !game.FirstMoveDone)
        {
            // 第一手翻棋隨機選擇
            return flips[_rng.Next(flips.Count)];
        }

        // 已決定顏色後，使用 Minimax 評估
        if (moves.Count > 0)
        {
            var bestAction = default(GameAction);
            var bestScore = int.MinValue;

            foreach (var action in moves)
            {
                var cloned = CloneGame(game);
                ApplyAction(cloned, action);
                var score = Minimax(cloned, _searchDepth - 1, int.MinValue, int.MaxValue, false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = action;
                }
            }

            // 若有高分走法，優先走
            if (bestScore > -500)
                return bestAction;
        }

        // 否則翻棋
        if (flips.Count > 0)
            return flips[_rng.Next(flips.Count)];

        // 最後退回任意移動
        return actions[_rng.Next(actions.Count)];
    }

    /// <summary>
    /// Minimax 演算法 + Alpha-Beta 剪枝。
    /// </summary>
    private int Minimax(Banqi game, int depth, int alpha, int beta, bool maximizing)
    {
        // 終止條件
        if (depth == 0 || game.Winner is not null)
            return Evaluate(game);

        var currentColor = maximizing ? _botColor : Banqi.Opposite(_botColor);
        var actions = GetLegalActions(game, currentColor);

        if (actions.Count == 0)
        {
            // 無合法行動（應由 HasAnyMove 判定勝負）
            return Evaluate(game);
        }

        if (maximizing)
        {
            var maxEval = int.MinValue;
            foreach (var action in actions)
            {
                var cloned = CloneGame(game);
                ApplyAction(cloned, action);
                var eval = Minimax(cloned, depth - 1, alpha, beta, false);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break; // Beta 剪枝
            }
            return maxEval;
        }
        else
        {
            var minEval = int.MaxValue;
            foreach (var action in actions)
            {
                var cloned = CloneGame(game);
                ApplyAction(cloned, action);
                var eval = Minimax(cloned, depth - 1, alpha, beta, true);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break; // Alpha 剪枝
            }
            return minEval;
        }
    }

    /// <summary>
    /// 評估函數：計算場上己方棋子總分 - 對方棋子總分。
    /// </summary>
    private int Evaluate(Banqi game)
    {
        // 勝負判定
        if (game.Winner == _botColor) return 10000;
        if (game.Winner == Banqi.Opposite(_botColor)) return -10000;

        var myScore = 0;
        var opScore = 0;

        for (var i = 0; i < Banqi.Count; i++)
        {
            var cell = game.Board[i];
            if (cell.IsEmpty || !cell.FaceUp) continue;

            var piece = cell.Piece!.Value;
            var score = GetPieceValue(piece.Kind);

            if (piece.Color == _botColor)
                myScore += score;
            else
                opScore += score;
        }

        return myScore - opScore;
    }

    /// <summary>
    /// 棋子價值評估。
    /// </summary>
    private static int GetPieceValue(PieceKind kind) => kind switch
    {
        PieceKind.General => 1000,
        PieceKind.Advisor => 600,
        PieceKind.Elephant => 500,
        PieceKind.Chariot => 500,
        PieceKind.Horse => 400,
        PieceKind.Cannon => 450,  // 炮特殊能力，加分
        PieceKind.Soldier => 200,
        _ => 0
    };

    /// <summary>
    /// 取得合法行動（含翻棋與走子）。
    /// </summary>
    private List<GameAction> GetLegalActions(Banqi game, Color color)
    {
        var actions = new List<GameAction>();

        // 翻棋
        for (var i = 0; i < Banqi.Count; i++)
        {
            if (game.Board[i].IsHidden)
                actions.Add(new GameAction(ActionKind.Flip, i));
        }

        // 走子/吃子
        if (game.CurrentColor is not null)
        {
            for (var from = 0; from < Banqi.Count; from++)
            {
                var cell = game.Board[from];
                if (cell.IsEmpty || !cell.FaceUp) continue;
                if (cell.Piece!.Value.Color != color) continue;

                var savedColor = game.CurrentColor;
                game.CurrentColor = color;
                var moves = game.LegalMovesFrom(from);
                game.CurrentColor = savedColor;

                foreach (var move in moves)
                    actions.Add(new GameAction(ActionKind.Move, move.From, move.To));
            }
        }

        return actions;
    }

    /// <summary>
    /// 複製遊戲狀態。
    /// </summary>
    private static Banqi CloneGame(Banqi game)
    {
        var cloned = new Banqi(0);
        for (var i = 0; i < Banqi.Count; i++)
        {
            cloned.Board[i] = new Cell
            {
                Piece = game.Board[i].Piece,
                FaceUp = game.Board[i].FaceUp
            };
        }
        cloned.CurrentColor = game.CurrentColor;
        cloned.FirstMoveDone = game.FirstMoveDone;
        cloned.Winner = game.Winner;
        cloned.Message = game.Message;
        return cloned;
    }

    /// <summary>
    /// 執行行動。
    /// </summary>
    private static void ApplyAction(Banqi game, GameAction action)
    {
        if (action.Kind == ActionKind.Flip)
            game.Flip(action.From);
        else
            game.TryMove(action.From, action.To);
    }
}
