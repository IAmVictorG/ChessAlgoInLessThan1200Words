using ChessChallenge.API;
using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;



public class MyBot : IChessBot
{

    private int MVVLVAPriority(Piece attacker, Piece victim)
    {
        int[] pieceValues = { 0, 10, 31, 33, 56, 95, 1000000 }; // The values are taken from your EvaluateBoard function
        return pieceValues[(int)victim.PieceType] - pieceValues[(int)attacker.PieceType];
    }

    public int getSizeofList(PieceList[] list)
    {
        int PiecesNumber = list.Length;
        int t = 0;
        for (int i = 0; i < PiecesNumber; i++)
        {
            for (int j = 0; j < list[i].Count(); j++)
            {
                t++;
            }
        }
        return t;
    }

    public int EvaluateBoard(Board board)
    {
        int score = 0;
        PieceList[] list = board.GetAllPieceLists();
        int PiecesNumber = list.Length;
        /*if (board.HasKingsideCastleRight(true) || board.HasQueensideCastleRight(true))
            score += 500;
        if (board.HasKingsideCastleRight(false) || board.HasQueensideCastleRight(false))
            score -= 500;*/
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        }
        for (int i = 0; i < PiecesNumber; i++)
        {
            for (int j = 0; j < list[i].Count(); j++)
            {

                Piece piece = list[i].GetPiece(j);
                switch (piece.PieceType)
                {
                    case PieceType.Pawn:
                        score += (piece.IsWhite ? 1 : -1) * 10;
                        score += piece.IsWhite ? piece.Square.Rank : -1 * (8 - piece.Square.Rank);
                        break;
                    case PieceType.Knight:
                        score += (piece.IsWhite ? 1 : -1) * 31;
                        break;
                    case PieceType.Bishop:
                        score += (piece.IsWhite ? 1 : -1) * 33;
                        break;
                    case PieceType.Rook:
                        score += (piece.IsWhite ? 1 : -1) * 56;
                        break;
                    case PieceType.Queen:
                        score += (piece.IsWhite ? 1 : -1) * 95;
                        break;
                }
            }
        }
        if (score >= 5 && board.IsDraw())
        {
            score -= 20;
        }
        if (score <=  -5 && board.IsDraw())
        {
            score += 20;
        }
        return score;
    }

    public (List<Move> topMoves, int bestScore) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, Timer timer)
    {
        Move[] moves = board.GetLegalMoves(false);
        Move bestMove = Move.NullMove;
        int maxEval;
        int minEval;

        List<(Move move, int score)> moveScores = new List<(Move move, int score)>();

        if (depth == 0)
        {
            int score = EvaluateBoard(board);
            if (moves.Length > 0)
            {
                moveScores.Add((moves[0], score));
            }
            return (ExtractTopMoves(moveScores, maximizingPlayer), score);
        }

        if (maximizingPlayer)
        {
            maxEval = int.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                var result = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, timer);
                int eval = result.bestScore;
                board.UndoMove(move);

                if (maxEval < eval)
                {
                    maxEval = eval;
                    bestMove = move;
                }

                moveScores.Add((move, eval));

                if (eval >= beta)
                {
                    break;
                }

                alpha = Math.Max(alpha, eval);
            }
            return (ExtractTopMoves(moveScores, true), maxEval);
        }
        else
        {
            minEval = int.MaxValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                var result = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, timer);
                int eval = result.bestScore;
                board.UndoMove(move);

                if (minEval > eval)
                {
                    minEval = eval;
                    bestMove = move;
                }

                moveScores.Add((move, eval));

                if (eval <= alpha)
                {
                    break;
                }

                beta = Math.Min(beta, eval);
            }
            return (ExtractTopMoves(moveScores, false), minEval);
        }
    }

    private List<Move> ExtractTopMoves(List<(Move move, int score)> moveScores, bool isMaximizing)
    {
        if (isMaximizing)
        {
            return moveScores
                .OrderByDescending(ms => ms.score)
                .Take(5)
                .Select(ms => ms.move)
                .ToList();
        }
        else
        {
            return moveScores
                .OrderBy(ms => ms.score)
                .Take(5)
                .Select(ms => ms.move)
                .ToList();
        }
    }





    public Move process(Board board, Timer timer)
    {
        int depth = 3;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        var (topMoves, score) = MiniMax(board, depth, alpha, beta, board.IsWhiteToMove, timer);

        return topMoves[0];
    }


    public Move Think(Board board, Timer timer)
    {
         Move move = process(board, timer);

        return move;
        
    }

    /*public Move Think(Board board, Timer timer)
    {
        PieceList[] list = board.GetAllPieceLists();
        int totalPiecesLeft = getSizeofList(list);
        Move[] moves = board.GetLegalMoves();
        int depth = 3;
        if (totalPiecesLeft < 5)
            depth = 7;
        if (totalPiecesLeft < 8)
            depth = 6;
        else if (moves.Length <= 10 && totalPiecesLeft < 15)
            depth = 5;
        else if (moves.Length <= 30)
            depth = 4;
        int firsteval = EvaluateBoard(board);
        Move move;

        move = MiniMax(board, depth, int.MinValue, int.MaxValue, board.IsWhiteToMove, timer).Item1;
        if (move == Move.NullMove)  
            move = moves[0];    
        return (move);
    }*/
}