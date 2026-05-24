using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace CodeConnect.Logic
{
    public static class BackendConnector
    {
        private const string DllPath = "backend.dll";

        [DllImport(DllPath, EntryPoint = "GenerateLevel", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GenerateLevel(int level);

        [DllImport(DllPath, EntryPoint = "GetCellType", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCellType(int x, int y);

        [DllImport(DllPath, EntryPoint = "TogglePath", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TogglePath(int x, int y, int flowId);

        [DllImport(DllPath, EntryPoint = "IsLevelComplete", CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsLevelComplete();

        [DllImport(DllPath, EntryPoint = "GetNodeText", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetNodeText(int x, int y, StringBuilder buffer);

        // --- Managed helpers ---
        private static int _moves;
        private static int _mistakes;
        private static DateTime _levelStart;
        private static Stack<(int x, int y, int flow)> _undoStack = new();
        private static Stack<(int x, int y, int flow)> _redoStack = new();

        public static void StartLevel(int level)
        {
            _moves = 0;
            _mistakes = 0;
            _undoStack.Clear();
            _redoStack.Clear();
            _levelStart = DateTime.Now;
            GenerateLevel(level);
        }

        public static void PlacePath(int x, int y, int flowId)
        {
            int before = GetCellType(x, y);
            TogglePath(x, y, flowId);
            int after = GetCellType(x, y);

            _moves++;
            _undoStack.Push((x, y, flowId));
            _redoStack.Clear();

            // If nothing changed, count as a mistake
            if (before == after) _mistakes++;
        }

        public static bool UndoLastMove()
        {
            if (_undoStack.Count == 0) return false;
            var last = _undoStack.Pop();
            TogglePath(last.x, last.y, last.flow); // remove path
            _redoStack.Push(last);
            return true;
        }

        public static bool RedoLastMove()
        {
            if (_redoStack.Count == 0) return false;
            var move = _redoStack.Pop();
            TogglePath(move.x, move.y, move.flow);
            _undoStack.Push(move);
            return true;
        }

        public static int GetScore()
        {
            int baseScore = Math.Max(1000 - _moves * 10, 0);
            int penalty = _mistakes * 20;
            int timeBonus = Math.Max(0, 300 - (int)(DateTime.Now - _levelStart).TotalSeconds);
            return Math.Max(baseScore - penalty + timeBonus, 0);
        }

        public static string GetScoreDetails()
        {
            return $"Moves: {_moves}, Mistakes: {_mistakes}, Time: {(int)(DateTime.Now - _levelStart).TotalSeconds}s, Score: {GetScore()}";
        }

        public static string GetHint()
        {
            for (int x = 0; x < 20; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    int val = GetCellType(x, y);
                    int type = val & 0x0F;
                    int flowId = val >> 4;
                    if (type > 0 && type < 4) return $"Try connecting node at ({x},{y}) with flow {flowId}";
                }
            }
            return "No hints available!";
        }

        public static string SaveProgress()
        {
            var state = new
            {
                moves = _moves,
                mistakes = _mistakes,
                elapsed = (int)(DateTime.Now - _levelStart).TotalSeconds,
                undoStack = _undoStack.ToArray(),
                redoStack = _redoStack.ToArray()
            };
            return JsonSerializer.Serialize(state);
        }

        public static void LoadProgress(string json)
        {
            var state = JsonSerializer.Deserialize<dynamic>(json);
            _moves = (int)state["moves"];
            _mistakes = (int)state["mistakes"];
            _levelStart = DateTime.Now.AddSeconds(-(int)state["elapsed"]);
            _undoStack = new Stack<(int, int, int)>(((object[])state["undoStack"]).Length);
            _redoStack = new Stack<(int, int, int)>(((object[])state["redoStack"]).Length);
            // Note: you can rehydrate stacks more fully with typed models
        }
    }
}
