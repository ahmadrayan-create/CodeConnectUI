using System;
using System.Collections.Generic;
using CodeConnect.Models;

namespace CodeConnect.Logic
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class LevelData
    {
        public int Number { get; set; }
        public string Description { get; set; }
        public List<Node> Nodes { get; set; }
        public int TargetConnections { get; set; }
        public Difficulty Difficulty { get; set; }
        public List<string> BonusObjectives { get; set; } = new();
    }

    public static class LevelManager
    {
        public static List<LevelData> Levels = new List<LevelData>();
        private static Random _rng = new Random();

        static LevelManager()
        {
            InitializeLevels();
        }

        private static void InitializeLevels()
        {
            string[] opcodes = { "MOV", "ADD", "SUB", "XOR", "CMP", "AND", "OR", "MUL", "DIV" };
            string[] registers = { "EAX", "EBX", "ECX", "EDX", "ESI", "EDI", "RAX", "RBX" };

            for (int i = 1; i <= 20; i++)
            {
                var nodes = new List<Node>();
                int instructionCount = 2 + (i / 5);

                for (int row = 0; row < instructionCount; row++)
                {
                    nodes.Add(new Node(row * 3, opcodes[_rng.Next(Math.Min(i, opcodes.Length))], NodeType.Opcode, 0));
                    nodes.Add(new Node(row * 3 + 1, registers[_rng.Next(registers.Length)], NodeType.Register, 1));
                    nodes.Add(new Node(row * 3 + 2, _rng.Next(1, 100).ToString(), NodeType.Immediate, 2));
                }

                PositionNodesInGrid(nodes, instructionCount);

                var level = new LevelData
                {
                    Number = i,
                    Description = $"Level {i}: Assemble {instructionCount} statements correctly.",
                    Nodes = nodes,
                    TargetConnections = instructionCount * 2,
                    Difficulty = i <= 7 ? Difficulty.Easy : i <= 14 ? Difficulty.Medium : Difficulty.Hard
                };

                // Add bonus objectives
                if (i % 3 == 0) level.BonusObjectives.Add("Complete in under 60 seconds");
                if (i % 5 == 0) level.BonusObjectives.Add("No mistakes allowed");

                Levels.Add(level);
            }
        }

        private static void PositionNodesInGrid(List<Node> nodes, int rowCount)
        {
            int startX = 100;
            int startY = 80;
            int colSpacing = 250;
            int rowSpacing = 80;

            var col0 = nodes.FindAll(n => n.Column == 0);
            var col1 = nodes.FindAll(n => n.Column == 1);
            var col2 = nodes.FindAll(n => n.Column == 2);

            ShuffleList(col0);
            ShuffleList(col1);
            ShuffleList(col2);

            for (int r = 0; r < rowCount; r++)
            {
                col0[r].X = startX;
                col0[r].Y = startY + r * rowSpacing;

                col1[r].X = startX + colSpacing;
                col1[r].Y = startY + r * rowSpacing;

                col2[r].X = startX + colSpacing * 2;
                col2[r].Y = startY + r * rowSpacing;
            }
        }

        private static void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // --- New helpers ---
        public static LevelData GetLevel(int number) => Levels.Find(l => l.Number == number);

        public static LevelData GetNextLevel(int current)
        {
            int next = current + 1;
            return next <= Levels.Count ? GetLevel(next) : null;
        }

        public static LevelData GetRandomLevel(Difficulty difficulty)
        {
            var candidates = Levels.FindAll(l => l.Difficulty == difficulty);
            return candidates[_rng.Next(candidates.Count)];
        }
    }
}
