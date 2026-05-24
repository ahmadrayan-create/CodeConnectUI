using System.Drawing;

namespace CodeConnect.Models
{
    public enum NodeType
    {
        Opcode = 1,
        Register = 2,
        Immediate = 3,
        Memory = 4
    }

    public class Node
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public NodeType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        
        // Grid Info
        public int Column { get; set; } // 0: Opcode, 1: Register, 2: Value
        
        // Interactive States
        public bool IsHovered { get; set; }
        public bool IsSelected { get; set; }
        
        // Connection Tracking (Strict 1-to-1)
        public Node? ConnectedTo { get; set; }
        public Node? ConnectedFrom { get; set; }
        
        public Node(int id, string text, NodeType type, int column)
        {
            Id = id;
            Text = text;
            Type = type;
            Column = column;
            IsHovered = false;
            IsSelected = false;
        }

        public Rectangle Bounds => new Rectangle(X, Y, 100, 50);
        public Point Center => new Point(X + 50, Y + 25);
    }

    public class Connection
    {
        public Node From { get; set; }
        public Node To { get; set; }
        
        public Connection(Node from, Node to)
        {
            From = from;
            To = to;
        }
    }
}
