using ChessChallenge.API;
using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;

public class MyBot : IChessBot
{
    public int EvaluateBoard(Board board, bool isDraw, bool me)
    {
        int score = 0;
        PieceList[] list = board.GetAllPieceLists();
        int PiecesNumber = list.Length;
        Square myKingSquare = new Square("a8");
        Square oppKingSquare = new Square("a1");
        int kingmalus;
        for (int i = 0; i < PiecesNumber; i++)
        {
            for (int j = 0; j < list[i].Count(); j++)
            {

                Piece piece = list[i].GetPiece(j);
                int multiple = 1;
                kingmalus = 0;
                if (piece.PieceType == PieceType.King)
                {
                    if ((me && piece.IsWhite) || (!me && !piece.IsWhite))
                        myKingSquare = piece.Square;
                    else
                        oppKingSquare = piece.Square;
                }
                if (board.PlyCount < 15)
                {
                    multiple = 2;
                    kingmalus = 10;
                }
                if (board.PlyCount < 12)
                {
                    if (me)
                    {
                        if (board.GetPiece(new Square("d1")).IsNull)
                            score -= 5;
                        if (board.GetPiece(new Square("b1")).IsNull)
                            score += 5;
                        if (board.GetPiece(new Square("g1")).IsNull)
                            score += 5;
                    }
                    else
                    {
                        if (board.GetPiece(new Square("d8")).IsNull)
                            score += 5;
                        if (board.GetPiece(new Square("b8")).IsNull)
                            score -= 5;
                        if (board.GetPiece(new Square("g8")).IsNull)
                            score -= 5;
                    }
                }
                switch (piece.PieceType)
                {

                    case PieceType.Pawn:
                        score += (piece.IsWhite ? 1 : -1) * 10;
                        if (piece.IsWhite)
                        {
                           

                             score += (piece.Square.Rank / 2 + 2) * multiple;
                            if (piece.Square.File >= 3 && piece.Square.File <= 5 && piece.Square.Rank > 3)
                                score += 4 * multiple;
                        }
                        else
                        {
                                score += (-8 + piece.Square.Rank / 2 - 2) * multiple;
                            if (piece.Square.File >= 3 && piece.Square.File <= 5 && piece.Square.Rank < 5)
                                score -= 4 * multiple;
                        }
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
                    case PieceType.King:
                        if (piece.IsWhite && piece.Square.Rank > 1)
                            score -= kingmalus;
                        if (!piece.IsWhite && piece.Square.Rank < 7)
                            score += kingmalus;
                        break;
                }
            }
        }
        // After processing all pieces and tracking kings' positions, consider the distance between kings in the endgame.
        if (board.PlyCount >= 40) // Assuming endgame starts around move 20 for both players. Adjust as needed.
        {
            // Ensure both kings were found
                int distance = Math.Abs(myKingSquare.File - oppKingSquare.File) + Math.Abs(myKingSquare.Rank - oppKingSquare.Rank);

                // If evaluating for black (me == false), reverse the score effect
                int modifier = me ? 1 : -1;

                // Adjust score based on distance, smaller distance gives positive score for white and negative score for black
                score += modifier * (10 - distance) * 3; // Multiplier of 5 is just an example, adjust for desired influence.
        }

        if (isDraw)
        {
            if (me)
                    score -= 50;
            else if (!me)
                    score += 50;
        }
        return score;
    }

    public (List<(Move move, Board board)> topMoves, int bestScore) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, bool isDraw, bool me)
    {
        Move[] moves;
        if (depth == 1)
            moves = board.GetLegalMoves(true);
        else
            moves = board.GetLegalMoves(false);
        if (moves.Length == 0)
            moves = board.GetLegalMoves(false);
        Move bestMove = Move.NullMove;
        int maxEval;
        int minEval;
        bool t = isDraw;
        List<(Move move, Board board, int score)> moveBoardScores = new List<(Move move, Board board, int score)>();
        if (depth == 0)
        {
            int score = EvaluateBoard(board, isDraw, me);
            if (moves.Length > 0)
            {
                moveBoardScores.Add((moves[0], board, score));
            }
            return (ExtractTopMoves(moveBoardScores, maximizingPlayer), score);
        }


        if (maximizingPlayer)
        {
            maxEval = int.MinValue;

            foreach (Move move in moves)
            {
                if (t == false && board.IsDraw() == false)
                    isDraw = false;
                if (board.IsDraw())
                    isDraw = true;
                board.MakeMove(move);
                if (!isDraw && board.IsDraw() == true)
                    isDraw = true;
                var result = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer,isDraw, me);
                int eval = result.bestScore;

                if (maxEval < eval)
                {
                    maxEval = eval;
                    bestMove = move;
                }
                moveBoardScores.Add((move, board, eval));
                board.UndoMove(move);
                if (isDraw && board.IsDraw() == false)
                    isDraw = false;

                if (eval >= beta)
                {
                    break;
                }

                alpha = Math.Max(alpha, eval);
            }
            return (ExtractTopMoves(moveBoardScores, true), maxEval);
        }
        else
        {
            minEval = int.MaxValue;
            foreach (Move move in moves)
            {
                if (t == false && board.IsDraw() == false)
                    isDraw = false;
                if (board.IsDraw())
                    isDraw = true;
                board.MakeMove(move);
                if (!isDraw && board.IsDraw() == true)
                    isDraw = true;
                var result = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, isDraw, me);
                int eval = result.bestScore;
                if (minEval > eval)
                {
                    minEval = eval;
                    bestMove = move;
                }
                moveBoardScores.Add((move, board, eval));
                board.UndoMove(move);
                if (isDraw && board.IsDraw() == false)
                    isDraw = false;
                if (eval <= alpha)
                {
                    break;
                }
                beta = Math.Min(beta, eval);
            }
            return (ExtractTopMoves(moveBoardScores, false), minEval);
        }
    }

    private List<(Move move, Board board)> ExtractTopMoves(List<(Move move, Board board, int score)> moveBoardScores, bool isMaximizing)
    {
        if (isMaximizing)
        {
            return moveBoardScores
                .OrderByDescending(mbs => mbs.score)
                .Take(5)
                .Select(mbs => (mbs.move, mbs.board))
                .ToList();
        }
        else
        {
            return moveBoardScores
                .OrderBy(mbs => mbs.score)
                .Take(5)
                .Select(mbs => (mbs.move, mbs.board))
                .ToList();
        }
    }

    public Move Process(Board board, Timer timer)
    {
        bool me = board.IsWhiteToMove;
        int initialDepth = 4;
        int nbMove = board.GetLegalMoves().Length;
        if (nbMove < 3)
            initialDepth = 6;
        else if (nbMove < 15)
            initialDepth = 5;
        if (timer.MillisecondsRemaining < 5000)
            initialDepth = 3;
        var (topMoves, _) = MiniMax(board, initialDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove, false, me);

        Move bestMove = topMoves[0].move;
        /*Move bestMove = Move.NullMove;
        int bestScore = board.IsWhiteToMove ? int.MinValue : int.MaxValue;

        foreach (var moveBoard in topMoves)
        {
            var (_, nextBestScore) = MiniMax(moveBoard.board, initialDepth, int.MinValue, int.MaxValue, moveBoard.board.IsWhiteToMove, moveBoard.board.IsDraw());

            if (board.IsWhiteToMove)
            {
                if (nextBestScore > bestScore)
                {
                    bestScore = nextBestScore;
                    bestMove = moveBoard.move;
                }
            }
            else
            {
                if (nextBestScore < bestScore)
                {
                    bestScore = nextBestScore;
                    bestMove = moveBoard.move;
                }
            }
        }*/
        return bestMove;
    }



    public Move Think(Board board, Timer timer)
    {
        Move move = Process(board, timer);
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