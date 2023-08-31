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
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        static int test;
        public int CalculateDepth(int legalMoves)
        {
            const int MAX_POSITIONS = 1000000; //The aproximate maximum numbers of Position you want to evaluate per round
            double d = Math.Log(MAX_POSITIONS) / Math.Log(legalMoves);
            if (legalMoves == 1)
                return (8);
            return (int)Math.Floor(d);
        }

        public int EvaluateBoard(Board board)
        {
            int score = 0;
            PieceList[] list = board.GetAllPieceLists();
            int PiecesNumber = list.Length;

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
                            if (piece.IsWhite)
                            {
                                score += piece.Square.Rank;
                            }
                            else
                            {
 
                                score -= 8 - piece.Square.Rank;
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
                    }
                }
            }
            return score;
        }



        public (Move, int) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, Timer timer)
        {
            Move[] moves = board.GetLegalMoves(false);
            Move bestMove = Move.NullMove;
            int maxEval;
            int minEval;
            if (depth == 0)
            {
                return (moves.Length > 0 ? moves[0] : Move.NullMove, EvaluateBoard(board));
            }
            if (maximizingPlayer)
            {
                maxEval = int.MinValue;
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    if (board.IsDraw())
                    {
                        board.UndoMove(move);
                        continue;
                    }
                    int eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, timer).Item2;
                    board.UndoMove(move);
                    if (maxEval <= eval)
                    {
                        maxEval = eval;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
                return (bestMove, maxEval);
            }
            else
            {
                minEval = int.MaxValue;
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    if (board.IsDraw())
                    {
                        board.UndoMove(move);
                        continue;
                    }
                    int eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, timer).Item2;
                    board.UndoMove(move);
                    if (minEval > eval)
                    {
                        minEval = eval;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
                return (bestMove, minEval);
            }
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

        public Move Think(Board board, Timer timer)
        {

            Move[] moves = board.GetLegalMoves();
            int depth = 3;

            int firsteval = EvaluateBoard(board);
            Move move;
            PieceList[] list = board.GetAllPieceLists();
            int totalPiecesLeft = getSizeofList(list);


            move = MiniMax(board, depth, int.MinValue, int.MaxValue, board.IsWhiteToMove, timer).Item1;
            return (move);
        }
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
    }
}