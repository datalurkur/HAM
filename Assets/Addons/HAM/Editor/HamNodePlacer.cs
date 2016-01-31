//#define PLACER_DEBUG

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class HamNodePlacer
{
	private class PlacementNode
	{
		public HamTimelineNode Node;
		public PlacementNode Parent;
		public int Depth;
		public int Width;
		public List<PlacementNode> Children;
		public float WidthOffset;

		public PlacementNode(HamTimelineNode node, PlacementNode parent)
		{
			this.Node = node;
			this.Parent = parent;
			if (this.Parent != null)
			{
				this.Parent.Children.Add(this);
				this.Depth = this.Parent.Depth + 1;
			}
			else
			{
				this.Depth = 0;
			}
			this.Children = new List<PlacementNode>();
			this.Width = 0;
			this.WidthOffset = 0;
		}

		public void ReParent(PlacementNode parent)
		{
			int newDepth = parent.Depth + 1;
			if (this.Depth < newDepth)
			{
				this.Depth = newDepth;
				RefreshDepth(newDepth);
			}
		}

		public void RefreshDepth(int newDepth)
		{
			Queue<PlacementNode> depthQueue = new Queue<PlacementNode>();
			for (int i = 0; i < this.Children.Count; ++i)
			{
				depthQueue.Enqueue(this.Children[i]);
			}
			while (depthQueue.Count > 0)
			{
				PlacementNode subNode = depthQueue.Dequeue();
				subNode.Depth = subNode.Parent.Depth + 1;
				for (int i = 0; i < subNode.Children.Count; ++i)
				{
					depthQueue.Enqueue(subNode.Children[i]);
				}
			}
		}

		public string Describe()
		{
			if (this.Node.Type == TimelineNodeType.Dialog)
			{
				return String.Format("{0} {1} - {2}", this.Node.Type, this.Node.ID, ((HamDialogNode)this.Node).Dialog);
			}
			else
			{
				return String.Format("{0} {1}", this.Node.Type, this.Node.ID);
			}
		}
	}

	private HamTimeline timeline;
	private bool attemptReparenting;

	public HamNodePlacer(HamTimeline timeline, bool attemptReparenting = true)
	{
		this.timeline = timeline;
		this.attemptReparenting = attemptReparenting;
	}

	public void GetNodePlacement(out Dictionary<int,Vector2> places)
	{
		places = new Dictionary<int,Vector2>();

		HashSet<int> visited = new HashSet<int>();
		Dictionary<int, PlacementNode> allNodes = new Dictionary<int, PlacementNode>();

		// Iterate through tree, one row of depth at a time, developing a parentage relationship as we go
		// The goal here is to find 0 or 1 unique parent for each node to determine its final position in the tree
		// There will still be odd linkages, but we'll ignore those in favor of generally looking good
		PlacementNode root = new PlacementNode(this.timeline.OriginNode, null);
		allNodes[root.Node.ID] = root;

		Queue<PlacementNode> unprocessed = new Queue<PlacementNode>();
		unprocessed.Enqueue(root);
		visited.Add(this.timeline.OriginNode.ID);

		Stack<PlacementNode> reverse = new Stack<PlacementNode>();
		while (unprocessed.Count > 0)
		{
			PlacementNode current = unprocessed.Dequeue();
			reverse.Push(current);
			List<int> dids = current.Node.GetDescendantIDs();
			bool hadChildren = false;
			for (int j = 0; j < dids.Count; ++j)
			{
				if (!visited.Contains(dids[j]))
				{
					PlacementNode child = new PlacementNode(this.timeline.Nodes[dids[j]], current);
					allNodes[child.Node.ID] = child;
#if PLACER_DEBUG
					Debug.Log("Parenting " + child.Describe() + " under " + current.Describe());
#endif
					unprocessed.Enqueue(child);
					visited.Add(dids[j]);
					hadChildren = true;
				}
				else if(this.attemptReparenting)
				{
					allNodes[dids[j]].ReParent(current);
				}
			}
			if (!hadChildren)
			{
				current.Width = 1;
			}
		}

		// Now run through the nodes in the opposite order, computing width as we go
		// Since we pushed the nodes onto a stack in the previous traversal, this should guarantee proper order traversing back up
		while (reverse.Count > 0)
		{
			PlacementNode current = reverse.Pop();
			if (current.Parent != null)
			{
				current.Parent.Width += current.Width;
			}
		}

		// We should now have a nice tree with depths and widths properly computed
		// Starting from the root, propagate back down the tree, setting offsets for the children as we go
		unprocessed.Enqueue(root);
		while (unprocessed.Count > 0)
		{
			PlacementNode current = unprocessed.Dequeue();
			float cumulative = 0f;
			float r = current.WidthOffset - (current.Width / 2f);
			for (int j = 0; j < current.Children.Count; ++j)
			{
				PlacementNode child = current.Children[j];
				child.WidthOffset = (child.Width / 2f) + cumulative + r;
				cumulative += child.Width;
				unprocessed.Enqueue(child);
			}

			Vector2 finalPosition = new Vector2(current.WidthOffset, current.Depth);
			places[current.Node.ID] = finalPosition;
#if PLACER_DEBUG
			Debug.Log("Computed position for " + current.Describe() + " of " + finalPosition);
#endif
		}
	}
}