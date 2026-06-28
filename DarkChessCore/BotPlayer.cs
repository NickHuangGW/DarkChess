namespace DarkChess.Core;

/// <summary>
/// ?餉?拙振嚗蝙??Minimax + Alpha-Beta ?芣???
/// 蝑?芸???嚗??> ?? > ?脣?鋡怠? > 蝘餃? > 蝧餅???
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

    /// <summary>Bot ?格????脯?/summary>
    public Color BotColor => _botColor;

    /// <summary>?豢??雿唾???/summary>
    public GameAction ChooseAction(Banqi game)
    {
        var actions = GetLegalActions(game, _botColor);
        if (actions.Count == 0)
            throw new InvalidOperationException("?∪?瘜???瑁?");

        if (actions.Count == 1)
            return actions[0];

        // 蝚砌??蕃璉?憿?芸?嚗璈蕃
        if (!game.FirstMoveDone)
        {
            var flips = actions.Where(a => a.Kind == ActionKind.Flip).ToList();
            return flips[_rng.Next(flips.Count)];
        }

        // 撠??????怎蕃璉??瑁? Minimax嚗?雿?
        var bestAction = actions[0];
        var bestScore = int.MinValue;

        // 韏唳???嚗?摮???孵? Alpha-Beta ?芣???嚗?
        var ordered = OrderActions(actions, game);

        foreach (var action in ordered)
        {
            var cloned = CloneGame(game);
            ApplyAction(cloned, action);
            var score = Minimax(cloned, _searchDepth - 1, int.MinValue, int.MaxValue, false);

            // ??詨??璈??游像撅嚗?摰芋撘?
            if (score > bestScore || (score == bestScore && _rng.Next(4) == 0))
            {
                bestScore = score;
                bestAction = action;
            }
        }

        return bestAction;
    }

    /// <summary>
    /// 韏唳???嚗??孵澆?摮?> 雿?澆?摮?> 銝?祉宏??> 蝧餅???
    /// ???粥瘜? Alpha-Beta ?湔?芣???
    /// </summary>
    private List<GameAction> OrderActions(List<GameAction> actions, Banqi game)
    {
        return actions.OrderByDescending(a =>
        {
            if (a.Kind == ActionKind.Flip) return -1; // 蝧餅??敺?

            // ??嚗璅??寧蕃??摮?
            var dst = game.Board[a.To];
            if (!dst.IsEmpty && dst.FaceUp && dst.Piece!.Value.Color != _botColor)
                return GetPieceValue(dst.Piece!.Value.Kind) * 10; // 擃?澆?摮???

            return 0; // 銝?祉宏??
        }).ToList();
    }

    /// <summary>Minimax 瞍?瘜?+ Alpha-Beta ?芣???/summary>
    private int Minimax(Banqi game, int depth, int alpha, int beta, bool maximizing)
    {
        if (depth == 0 || game.Winner is not null)
            return Evaluate(game);

        var currentColor = maximizing ? _botColor : Banqi.Opposite(_botColor);
        var actions = GetLegalActions(game, currentColor);

        if (actions.Count == 0)
            return Evaluate(game);

        // 韏唳??????芣???
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
                if (beta <= alpha) break;
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
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    /// <summary>韏唳???嚗inimax ?折嚗?憿??嚗?/summary>
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
    /// 閰摯?賣??
    /// = ?璉?蝮賢? - 撠璉?蝮賢?
    /// - ?鋡怠???摮蝵堆?撠銝?甇亙??
    /// + 蝧餅?瞏???曌蝧餅??Ｙ揣鞈?嚗?
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
                // ?梯?璉?嚗??寥蝞?瞏???蝧餃????
                myScore += 50; // ??銝剜?憿?????賣?瞏?孵?
                continue;
            }

            var piece = cell.Piece!.Value;
            var val = GetPieceValue(piece.Kind);

            if (piece.Color == _botColor)
                myScore += val;
            else
                opScore += val;
        }

        // ?梢璉??脩蔑嚗??寧蕃??璉??亙鋡怠??寧??喳????脩蔑
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
                    // 撠?臭誑???寥?璉??脩蔑
                    dangerPenalty += GetPieceValue(target.Piece!.Value.Kind) / 2;
                }
            }
        }
        game.CurrentColor = savedColor;

        return (myScore - opScore) - dangerPenalty;
    }

    /// <summary>璉??孵潦?/summary>
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

    /// <summary>????銵?嚗蝧餅??粥摮???/summary>
    private List<GameAction> GetLegalActions(Banqi game, Color color)
    {
        var actions = new List<GameAction>();

        // 蝧餅?
        for (var i = 0; i < Banqi.Count; i++)
            if (game.Board[i].IsHidden)
                actions.Add(new GameAction(ActionKind.Flip, i));

        // 韏啣?/??
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
