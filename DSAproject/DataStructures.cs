using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAproject
{

    public class TaskNode
    {
        public string Name { get; set; }
        public int InDegree { get; set; }
        public List<TaskNode> Neighbors { get; set; }

        // Visualization properties (Coordinates for drawing)
        public Point Location { get; set; }
        public int Level { get; set; } // For arranging nodes in layers

        public TaskNode(string name)
        {
            Name = name;
            Neighbors = new List<TaskNode>();
            InDegree = 0;
        }
    }

    public class InputParser
    {
        // Responsibility: Convert raw text into a list of "A->B" relationships
        // new 
        public List<Tuple<string, string>> ParseRules(string rawText)
        {
            var dependencies = new List<Tuple<string, string>>();
            var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string cleanLine = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith("#")) continue;

                if (cleanLine.Contains("->"))
                {
                    var parts = cleanLine.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string source = parts[0].Trim();
                        string target = parts[1].Trim();

                        // --- NEW STRICT CHECK ---
                        // If a name contains spaces, quotes, or semicolons, it's probably garbage/code.
                        // We skip it.
                        if (source.Contains(" ") || target.Contains(" ") ||
                            source.Contains("\"") || target.Contains("\"") ||
                            source.Contains(";") || target.Contains(";"))
                        {
                            continue; // Skip this bad line
                        }
                        // ------------------------

                        dependencies.Add(Tuple.Create(source, target));
                    }
                }
            }
            return dependencies;
        }
        // old method
        //public List<Tuple<string, string>> ParseRules(string rawText)
        //{
        //    var dependencies = new List<Tuple<string, string>>();

        //    // Split by new lines
        //    var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //    foreach (var line in lines)
        //    {
        //        string cleanLine = line.Trim();

        //        // 1. Ignore comments (#) and empty lines
        //        if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith("#"))
        //            continue;

        //        // 2. Handle "->" separator
        //        if (cleanLine.Contains("->"))
        //        {
        //            var parts = cleanLine.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
        //            if (parts.Length == 2)
        //            {
        //                string source = parts[0].Trim();
        //                string target = parts[1].Trim();

        //                // Validation: Detect self-dependency (A->A) immediately? 
        //                // Or let the graph handle it. Let's add it here for robustness.
        //                if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
        //                    throw new Exception($"Self-dependency detected: {source} cannot depend on itself.");

        //                dependencies.Add(Tuple.Create(source, target));
        //            }
        //        }
        //    }
        //    return dependencies;
        //}
    }

    public class GraphManager
    {
        public Dictionary<string, TaskNode> Nodes { get; private set; }

        public GraphManager()
        {
            Nodes = new Dictionary<string, TaskNode>(StringComparer.OrdinalIgnoreCase);
        }

        public void BuildGraph(List<Tuple<string, string>> dependencies)
        {
            Nodes.Clear();

            // Create Nodes
            foreach (var dep in dependencies)
            {
                if (!Nodes.ContainsKey(dep.Item1)) Nodes[dep.Item1] = new TaskNode(dep.Item1);
                if (!Nodes.ContainsKey(dep.Item2)) Nodes[dep.Item2] = new TaskNode(dep.Item2);

                // Add Edge: Item1 -> Item2
                Nodes[dep.Item1].Neighbors.Add(Nodes[dep.Item2]);
                Nodes[dep.Item2].InDegree++;
            }
        }

        // ALGORITHM 1: Topological Sort (Kahn's Algorithm)
        public List<string> GetExecutionOrder()
        {
            var sortedList = new List<string>();
            var queue = new Queue<TaskNode>();

            // Need a temp dictionary to track indegrees so we don't destroy the main graph
            var tempInDegree = Nodes.Values.ToDictionary(n => n.Name, n => n.InDegree);

            // Add all nodes with 0 InDegree to queue
            foreach (var node in Nodes.Values)
            {
                if (node.InDegree == 0) queue.Enqueue(node);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                sortedList.Add(current.Name);

                foreach (var neighbor in current.Neighbors)
                {
                    tempInDegree[neighbor.Name]--;
                    if (tempInDegree[neighbor.Name] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // Cycle Detection Check: If sorted count != total nodes, there is a cycle
            if (sortedList.Count != Nodes.Count)
            {
                throw new InvalidOperationException("Cycle Detected! The plan is impossible.");
            }

            return sortedList;
        }

        // ALGORITHM 2: Cycle Detection (DFS) to find the specific "bad path"
        public List<string> FindCyclePath()
        {
            // Simple DFS to find one cycle for reporting
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var pathStack = new Stack<string>();

            foreach (var nodeKey in Nodes.Keys)
            {
                if (DetectCycleUtil(Nodes[nodeKey], visited, recursionStack, pathStack))
                {
                    // Return the path that caused the cycle
                    return pathStack.Reverse().ToList();
                }
            }
            return new List<string>();
        }

        private bool DetectCycleUtil(TaskNode node, HashSet<string> visited, HashSet<string> recStack, Stack<string> path)
        {
            visited.Add(node.Name);
            recStack.Add(node.Name);
            path.Push(node.Name);

            foreach (var neighbor in node.Neighbors)
            {
                if (!visited.Contains(neighbor.Name))
                {
                    if (DetectCycleUtil(neighbor, visited, recStack, path)) return true;
                }
                else if (recStack.Contains(neighbor.Name))
                {
                    path.Push(neighbor.Name); // Add the start of cycle to complete the loop visualization
                    return true;
                }
            }

            recStack.Remove(node.Name);
            path.Pop();
            return false;
        }
    }

    public class FileManager
    {
        public void SaveRules(string content, string filePath)
        {
            File.WriteAllText(filePath, content);
        }

        public string LoadRules(string filePath)
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            throw new FileNotFoundException("File not found.");
        }
    }
}
