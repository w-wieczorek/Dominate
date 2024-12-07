using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Dominate
{

	class Graph
	{
		public int Size { get; set; }  // 0, 1, 2, ..., N-1
		public Dictionary<int, List<int>> adj { get; set; }

		public Graph()
		{
			Size = 0;
			adj = new();
		}

		public void AddEdge(int a, int b)
		{
			Debug.Assert(a < Size);
			Debug.Assert(b < Size);
			if (adj.ContainsKey(a))
			{
				if (!adj[a].Contains(b)) adj[a].Add(b);
			}
			else
			{
				adj[a] = new();
				adj[a].Add(b);
			}
			if (adj.ContainsKey(b))
			{
				if (!adj[b].Contains(a)) adj[b].Add(a);
			}
			else
			{
				adj[b] = new();
				adj[b].Add(a);
			}
		}
	}
	
	// Class to represent a node in the branch and bound algorithm
	class Node
	{
		public int Level { get; set; }
		public bool[] Flag { get; set; }

		public Node(int n)
		{
			Level = 0;
			Flag = new bool[n];
		}

		public Node(Node copy)
		{
			Level = copy.Level;
			Flag = copy.Flag.ToArray();
		}

		public override string ToString()
		{
			StringBuilder str = new();
			str.Append($"Node at level {Level}: ");
			for (int i = 0; i < Flag.Length; ++i)
			{
				str.Append($"{(Flag[i] ? 1 : 0)} ");
			}
			return str.ToString();
		}
	}

	class Program
	{
		public static void decode<T1, T2>(string line, string pattern, out T1 val1, out T2 val2)
		{
			Regex parts = new Regex(pattern);
			Match match = parts.Match(line);
			val1 = default(T1);
			val2 = default(T2);
			if (match.Success)
			{
				var groups = match.Groups;
				val1 = (T1)Convert.ChangeType(groups[1].Value, typeof(T1));
				val2 = (T2)Convert.ChangeType(groups[2].Value, typeof(T2));
			}
			else
			{
				Console.WriteLine($"{line} does not match to {pattern}");
				Environment.Exit(1);
			}
		}

		public static void readData(string fileName, Graph graph)
		{
			string[] lines = Array.Empty<string>();
			try
			{
				lines = File.ReadAllLines(fileName);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"There is a problem with file {fileName}:");
				Console.WriteLine($"{ex.GetType()} says {ex.Message}");
				Environment.Exit(1);
			}
			int n = lines.Length;
			if (n <= 1)
			{
				Console.WriteLine($"There are no edgess in file {fileName}.");
				Environment.Exit(1);
			}
			int n1, n2;
			decode(lines[2], @"^(\d+)\s+(\d+)\s+\d+\s*$", out n1, out n2);
			Debug.Assert(n1 == n2);
			graph.Size = n1;
			for (int i = 3; i < n; i++)
			{
				int u, v;
				decode(lines[i], @"^(\d+)\s+(\d+)\s*$", out u, out v);
				Debug.Assert(u > v);
				graph.AddEdge(u - 1, v - 1);
			}
		}

		static bool IsSolution(Graph graph, Node node)
		{
			for (int v = 0; v < graph.Size; ++v)
			{
				if (!node.Flag[v])
				{
					bool found = false;
					if (graph.adj.ContainsKey(v))
					{
						foreach (int u in graph.adj[v])
						{
							if (node.Flag[u])
							{
								found = true;
								break;
							}
						}
					}

					if (!found)
					{
						return false;
					}
				}
			}
			return true;
		}
		
		// Function to calculate the lower bound
		static float LowerBound(Graph graph, Node node)
		{
			float value = 0.0f;
			int i;
			for (i = 0; i < node.Level; ++i)
			{
				if (node.Flag[i]) { value += 1.0f; }
			}

			int[] degree = new int[graph.Size];

			for (i = 0; i < graph.Size; ++i)
			{
				List<int>? neighbours;
				if (node.Flag[i] == false && graph.adj.TryGetValue(i, out neighbours))
				{
					foreach (int v in neighbours)
					{
						if (!node.Flag[v])
						{
							degree[i] += 1;
						}
					}
					degree[i] += node.Flag[i] ? 0 : 1;
				}
				else
				{
					degree[i] = node.Flag[i] ? 0 : 1;
				}
			}
			
			Array.Sort(degree);

			int not_covered = graph.Size;
			for (int v = 0; v < graph.Size; ++v)
			{
				if (node.Flag[v])
				{
					--not_covered;
				}
				else
				{
					foreach (int u in graph.adj[v])
					{
						if (node.Flag[u])
						{
							--not_covered;
							break;
						}
					}
				}
			}
			
			int sum = 0;
			i = graph.Size;
			while (sum < not_covered)
			{
				value += 1.0f;
				sum += degree[--i];
			}
			return value;
		}

		// Function to solve the problem using branch and bound
		static void Solve(Graph graph)
		{
			// Initialize nodes
			Node current = new Node(graph.Size);

			// Priority queue to store nodes based on lower bounds
			PriorityQueue<Node, float> pq = new();
			pq.Enqueue(current, LowerBound(graph, current));

			// Arrays to store the final selection of items
			bool[] finalPath = new bool[graph.Size];
			float finalResult = graph.Size;

			// Explore nodes in the priority queue
			while (pq.Count > 0)
			{
				Node currNode;
				float currBound;
				pq.TryDequeue(out currNode, out currBound);
				// Console.WriteLine($"Taken {currNode} with bound = {currBound}");

				// Prune if the lower bound is greater or equal than the final result
				if (currBound >= finalResult)
				{
					// Console.WriteLine("Pruned");
					continue;
				}

				// Check if it's the last level and update the final selection if the lower bound is better
				if (currNode.Level <= graph.Size)
				{
					if (IsSolution(graph, currNode))
					{
						float s = 0.0f;
						for (int i = 0; i < graph.Size; ++i)
						{
							if (currNode.Flag[i]) { s += 1.0f; }
						}

						if (s < finalResult)
						{
							Console.WriteLine($"Taken {currNode} with bound = {currBound}");
							finalResult = s;
							for (int i = 0; i < graph.Size; ++i)
							{
								finalPath[i] = currNode.Flag[i];
							}
							Console.WriteLine($"It is a new solution with {finalResult} vertices.");
						}
					}

					if (currNode.Level == graph.Size)
					{
						continue;
					}
				}

				int level = currNode.Level;

				// Explore the right node (exclude vertex)
				Node right = new Node(currNode);
				right.Level += 1;
				float bound = LowerBound(graph, right);
				if (bound < finalResult)
				{
					pq.Enqueue(right, bound);
				}

				// Explore the left node (include vertex)
				Node left = new Node(currNode);
				left.Flag[left.Level] = true;
				left.Level += 1;
				bound = LowerBound(graph, left);
				if (bound < finalResult)
				{
					pq.Enqueue(left, bound);
				}
				
			}

			// Print the final result
			Console.WriteLine("Vertices taken into the result are");
			for (int i = 0; i < graph.Size; i++)
			{
				if (finalPath[i])
					Console.Write("1 ");
				else
					Console.Write("0 ");
			}

			Console.WriteLine($"\nMinimum dominating set has {finalResult} vertices.");
		}

		// Main function
		static void Main(string[] args)
		{
			var graph = new Graph();
			readData(args[0], graph);
			// Call the solve function to solve the problem
			Solve(graph);
		}
	}
}