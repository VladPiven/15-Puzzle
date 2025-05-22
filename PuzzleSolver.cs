using System;
using System.Collections.Generic;

namespace FifteenPuzzle
{
    public class PuzzleSolver
    {
        private readonly int[,] initial;
        private readonly (int row, int col) empty;

        private static readonly (int dx, int dy)[] directions = {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        public PuzzleSolver(int[,] start, (int row, int col) emptyCell)
        {
            initial = (int[,])start.Clone();
            empty = emptyCell;
        }

        public List<(int row, int col)> Solve()
        {
            var visited = new HashSet<string>();
            var pq = new PriorityQueue<State, int>();
            var startState = new State(initial, empty, null, null, 0);
            pq.Enqueue(startState, startState.Cost);

            while (pq.Count > 0)
            {
                var current = pq.Dequeue();

                string hash = current.GetHash();
                if (visited.Contains(hash))
                    continue;

                visited.Add(hash);

                if (current.IsGoal())
                    return current.ExtractMoves();

                foreach (var dir in directions)
                {
                    int newRow = current.Empty.row + dir.dx;
                    int newCol = current.Empty.col + dir.dy;

                    if (newRow >= 0 && newRow < 4 && newCol >= 0 && newCol < 4)
                    {
                        var newState = current.MoveTile(newRow, newCol);
                        if (newState != null)
                            pq.Enqueue(newState, newState.Cost);
                    }
                }
            }

            return new List<(int row, int col)>();
        }

        private class State
        {
            public int[,] Board;
            public (int row, int col) Empty;
            public State? Parent;
            public (int row, int col)? Move;
            public int Depth;
            public int Cost => Depth + Heuristic();

            public State(int[,] board, (int row, int col) empty, State? parent, (int row, int col)? move, int depth)
            {
                Board = (int[,])board.Clone();
                Empty = empty;
                Parent = parent;
                Move = move;
                Depth = depth;
            }

            public int Heuristic()
            {
                int manhattan = 0;
                int linearConflict = 0;

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        int val = Board[i, j];
                        if (val == 0) continue;

                        int goalRow = (val - 1) / 4;
                        int goalCol = (val - 1) % 4;

                        manhattan += Math.Abs(i - goalRow) + Math.Abs(j - goalCol);

                        if (i == goalRow)
                        {
                            for (int k = j + 1; k < 4; k++)
                            {
                                int nextVal = Board[i, k];
                                if (nextVal == 0) continue;
                                int nextGoalRow = (nextVal - 1) / 4;
                                int nextGoalCol = (nextVal - 1) % 4;

                                if (nextGoalRow == i && nextGoalCol < goalCol)
                                    linearConflict++;
                            }
                        }

                        if (j == goalCol)
                        {
                            for (int k = i + 1; k < 4; k++)
                            {
                                int nextVal = Board[k, j];
                                if (nextVal == 0) continue;
                                int nextGoalRow = (nextVal - 1) / 4;
                                int nextGoalCol = (nextVal - 1) % 4;

                                if (nextGoalCol == j && nextGoalRow < goalRow)
                                    linearConflict++;
                            }
                        }
                    }
                }

                return manhattan + 2 * linearConflict;
            }


            public bool IsGoal()
            {
                int expected = 1;
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                    {
                        if (i == 3 && j == 3) return Board[i, j] == 0;
                        if (Board[i, j] != expected++) return false;
                    }
                return true;
            }

            public State? MoveTile(int newRow, int newCol)
            {
                int[,] newBoard = (int[,])Board.Clone();
                (int row, int col) = Empty;
                (newBoard[row, col], newBoard[newRow, newCol]) = (newBoard[newRow, newCol], newBoard[row, col]);

                return new State(newBoard, (newRow, newCol), this, (newRow, newCol), Depth + 1);
            }

            public List<(int row, int col)> ExtractMoves()
            {
                var path = new List<(int, int)>();
                var current = this;
                while (current.Move != null)
                {
                    path.Insert(0, current.Move.Value);
                    current = current.Parent!;
                }
                return path;
            }

            public string GetHash()
            {
                var hash = "";
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                        hash += Board[i, j].ToString("D2");
                return hash;
            }
        }
    }
}
