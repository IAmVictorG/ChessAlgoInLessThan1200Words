using ChessChallenge.API;
using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
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

        public int EvaluateBoard(Board board, bool isDraw)
        {
            int score = 0;
            PieceList[] list = board.GetAllPieceLists();
            int PiecesNumber = list.Length;
            if (board.HasKingsideCastleRight(true) || board.HasQueensideCastleRight(true))
                score -= 50;
            if (board.HasKingsideCastleRight(false) || board.HasQueensideCastleRight(false))
                score += 50;
            for (int i = 0; i < PiecesNumber; i++)
            {
                for (int j = 0; j < list[i].Count(); j++)
                {

                    Piece piece = list[i].GetPiece(j);
                    switch (piece.PieceType)
                    {
                        case PieceType.Pawn:
                            score += (piece.IsWhite ? 1 : -1) * 10;
                            if (piece.IsWhite)
                            {
                                if (piece.Square.Rank < 5)
                                    score += piece.Square.Rank;
                            }
                            else
                                if (piece.Square.Rank > 4)
                                score += -8 + piece.Square.Rank;
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
            if (isDraw)
            {
                if (board.IsWhiteToMove)
                {
                    if (score >= 10)
                        score += -20;
                    else
                        score += 20;
                }
                else
                {
                    if (score >= 10)
                        score += 20;
                    else
                        score += -20;
                }
            }
            return score;
        }

        public (List<(Move move, Board board)> topMoves, int bestScore) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, bool isDraw)
        {
            Move[] moves = board.GetLegalMoves(false);
            Move bestMove = Move.NullMove;
            int maxEval;
            int minEval;
            List<(Move move, Board board, int score)> moveBoardScores = new List<(Move move, Board board, int score)>();
            if (depth == 0)
            {
                int score = EvaluateBoard(board, isDraw);
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
                    board.MakeMove(move);
                    if (!isDraw && board.IsDraw() == true)
                        isDraw = true;
                    var result = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, isDraw);
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
                    board.MakeMove(move);
                    if (!isDraw && board.IsDraw() == true)
                        isDraw = true;
                    var result = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, isDraw);
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
            int initialDepth = 3;
            var (topMoves, _) = MiniMax(board, initialDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove, false);

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
}