using System.Runtime.InteropServices;
using System.Text;

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
        private static Stack<(int x, int y, int flow)> _history = new();

        public static void StartLevel(int level)
        {
            _moves = 0;
            _history.Clear();
            GenerateLevel(level);
        }

        public static void PlacePath(int x, int y, int flowId)
        {
            TogglePath(x, y, flowId);
            _moves++;
            _history.Push((x, y, flowId));
        }

        public static bool UndoLastMove()
        {
            if (_history.Count == 0) return false;
            var last = _history.Pop();
            // Toggle again to remove
            TogglePath(last.x, last.y, last.flow);
            return true;
        }

        public static int GetScore()
        {
            // Simple scoring: fewer moves = higher score
            return Math.Max(1000 - _moves * 10, 0);
        }
    }
}
