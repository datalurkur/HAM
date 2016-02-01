using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// The timeline contains the story content and logic
// It is comprised of timeline nodes which contain a list of linear events
// Each node type defines how the timeline progresses once it has been executed
public class HamTimeline : Packable
{
	public static void Save(HamTimeline timeline, string path)
	{
		DataPacker packer = new BinaryDataPacker();
		timeline.Pack(packer);
		File.WriteAllBytes(path, packer.GetBytes());
	}
	public static HamTimeline Load(string path)
	{
		byte[] data = File.ReadAllBytes(path);
		DataUnpacker unpacker = new BinaryDataUnpacker(data);
		HamTimeline timeline = new HamTimeline();
		timeline.Unpack(unpacker);
		return timeline;
	}

	public void Pack(DataPacker packer)
	{
		packer.Pack(this.IDCount);
		packer.Pack(this.Name);
		packer.Pack(this.OriginNodeID);
		packer.Pack(this.NarratorID);
		packer.Pack(this.DefaultSceneID);

		packer.Pack(this.Variables.Count);
		foreach (HamTimelineVariable variable in this.Variables.Values)
		{
			variable.Pack(packer);
		}

		packer.Pack(this.Scenes.Count);
		foreach (HamScene scene in this.Scenes.Values)
		{
			scene.Pack(packer);
		}

		packer.Pack(this.Characters.Count);
		foreach (HamCharacter character in this.Characters.Values)
		{
			character.Pack(packer);
		}

		packer.Pack(this.Nodes.Count);
		foreach (HamTimelineNode node in this.Nodes.Values)
		{
			HamTimelineNode.Pack(node, packer);
		}
	}
	public void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.IDCount);
		unpacker.Unpack(out this.Name);
		unpacker.Unpack(out this.OriginNodeID);
		unpacker.Unpack(out this.NarratorID);
		unpacker.Unpack(out this.DefaultSceneID);

		int numVars;
		unpacker.Unpack(out numVars);
		for (int i = 0; i < numVars; ++i)
		{
			HamTimelineVariable variable = new HamTimelineVariable();
			variable.Unpack(unpacker);
			this.Variables[variable.ID] = variable;
		}

		int numScenes;
		unpacker.Unpack(out numScenes);
		for (int i = 0; i < numScenes; ++i)
		{
			HamScene scene = new HamScene();
			scene.Unpack(unpacker);
			this.Scenes[scene.ID] = scene;
		}

		int numCharacters;
		unpacker.Unpack(out numCharacters);
		for (int i = 0; i < numCharacters; ++i)
		{
			HamCharacter character = new HamCharacter();
			character.Unpack(unpacker);
			this.Characters[character.ID] = character;
		}

		int numNodes;
		unpacker.Unpack(out numNodes);
		for (int i = 0; i < numNodes; ++i)
		{
			HamTimelineNode node;
			HamTimelineNode.Unpack(out node, unpacker);
			this.Nodes[node.ID] = node;
		}
	}

	public static int InvalidID = -1;

	public bool NodeLinkageDirty;

	public int IDCount;
	
	public string Name;
	public int OriginNodeID;
	public int NarratorID;
	public int DefaultSceneID;

	public Dictionary<int, HamTimelineVariable> Variables;
	public Dictionary<int, HamScene> Scenes;
	public Dictionary<int, HamCharacter> Characters;
	public Dictionary<int, HamTimelineNode> Nodes;

	public HamTimeline()
	{
		this.Variables = new Dictionary<int, HamTimelineVariable>();
		this.Scenes = new Dictionary<int, HamScene>();
		this.Characters = new Dictionary<int, HamCharacter>();
		this.Nodes = new Dictionary<int, HamTimelineNode>();
		this.NodeLinkageDirty = true;
	}

	public HamTimelineNode OriginNode { get { return this.Nodes[this.OriginNodeID]; } }
	public HamScene DefaultScene { get { return this.Scenes[this.DefaultSceneID]; } }
	public HamCharacter Narrator { get { return this.Characters[this.NarratorID]; } }

	public void DefaultInit()
	{
		this.IDCount = 0;

		// Always create the narrator
		this.NarratorID = AddCharacter("Narrator").ID;

		// Barebones defaults
		this.DefaultSceneID = AddScene("Default Scene").ID;
		this.OriginNodeID = AddDialogNode(this.DefaultSceneID, this.NarratorID, "Default Intro Narration").ID;	
	}

	public HamScene AddScene(string name)
	{
		int id = this.IDCount++;
		HamScene scene = new HamScene(id, name);
		this.Scenes[id] = scene;
		return scene;
	}

	public HamCharacter AddCharacter(string name)
	{
		int id = this.IDCount++;
		HamCharacter character = new HamCharacter(id, name);
		this.Characters[id] = character;
		return character;
	}

	public HamTimelineNode AddDialogNode(int sceneID, int speakerID, string dialog, List<int> characters = null)
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamDialogNode(id, sceneID, speakerID, dialog, characters);
		this.Nodes[id] = node;
		this.NodeLinkageDirty = true;
		return node;
	}

	public HamTimelineNode AddBranchNode()
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamBranchNode(id);
		this.Nodes[id] = node;
		this.NodeLinkageDirty = true;
		return node;
	}

	public HamTimelineNode AddDecisionNode()
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamDecisionNode(id);
		this.Nodes[id] = node;
		this.NodeLinkageDirty = true;
		return node;
	}

	public HamTimelineNode AddConsequenceNode()
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamConsequenceNode(id);
		this.Nodes[id] = node;
		this.NodeLinkageDirty = true;
		return node;
	}

	public HamTimelineVariable AddVariable(string name, VariableType type)
	{
		int id = this.IDCount++;
		HamTimelineVariable v = new HamTimelineVariable(id, type, name);
		this.Variables[id] = v;
		return v;
	}

	public void DeleteTree(HamTimelineNode node)
	{
		Queue<HamTimelineNode> toDelete = new Queue<HamTimelineNode>();
		toDelete.Enqueue(node);
		this.Nodes.Remove(node.ID);

		while (toDelete.Count > 0)
		{
			HamTimelineNode del = toDelete.Dequeue();

			// Remove any linkage between this node and its parents
			for (int i = 0; i < del.PreviousNodeIDs.Count; ++i)
			{
				if (!this.Nodes.ContainsKey(del.PreviousNodeIDs[i]))
				{
					// This parent already deleted, ignore it and move on
					continue;
				}
				HamTimelineNode parent = this.Nodes[del.PreviousNodeIDs[i]];
				int pIndex = parent.GetIndexOfDescendant(del.ID);
				parent.SetDescendant(InvalidID, pIndex);
			}

			// Queue up children
			List<int> descendants = del.GetDescendantIDs();
			for (int i = 0; i < descendants.Count; ++i)
			{
				if (!this.Nodes.ContainsKey(descendants[i]))
				{
					// This descendant already deleted, ignore it and move on
					continue;
				}
				HamTimelineNode descendant = this.Nodes[descendants[i]];
				int validParents = 0;
				for (int j = 0; j < descendant.PreviousNodeIDs.Count; ++j)
				{
					if (this.Nodes.ContainsKey(descendant.PreviousNodeIDs[j]) && descendant.PreviousNodeIDs[j] != del.ID)
					{
						validParents += 1;
					}
				}
				descendant.PreviousNodeIDs.Remove(del.ID);
				if (validParents == 0)
				{
					// This node is the only parent of the descendant, it gets deleted as part of the tree
					toDelete.Enqueue(descendant);
					this.Nodes.Remove(descendant.ID);
				}
			}
		}

		this.NodeLinkageDirty = true;
		SanityCheck();
	}

	public void DeleteNode(HamTimelineNode node)
	{
		// It's expected that there is at most 1 uniquely parented descendant
		// Find the dependant node, removing linkages to the current node in all descendants as we go
		HamTimelineNode dependant = null;
		List<int> descendants = node.GetDescendantIDs();
		for (int i = 0; i < descendants.Count; ++i)
		{
			HamTimelineNode descendant = this.Nodes[descendants[i]];
			if (descendant.UniquelyParented)
			{
				dependant = descendant;
			}
			descendant.PreviousNodeIDs.Remove(node.ID);
		}

		// Remove linkages to this node in the parents, linking the parents to the dependant node instead if it exists
		for (int i = 0; i < node.PreviousNodeIDs.Count; ++i)
		{
			HamTimelineNode parent = this.Nodes[node.PreviousNodeIDs[i]];
			int pIndex = parent.GetIndexOfDescendant(node.ID);
			parent.SetDescendant((dependant == null) ? InvalidID : dependant.ID, pIndex);
		}

		// If the node we're deleting is the origin, set any descendant (preferably the dependant) as the new origin
		if (node.ID == this.OriginNodeID)
		{
			if (dependant != null)
			{
				this.OriginNodeID = dependant.ID;
			}
			else
			{
				// There is assumed to be at least 1 descendant
				this.OriginNodeID = descendants[0];
			}
		}

		// Remove this node completely
		this.Nodes.Remove(node.ID);

		this.NodeLinkageDirty = true;
		SanityCheck();
	}

	public void LinkNodes(HamTimelineNode parent, HamTimelineNode child, int i = 0)
	{
		// Check for previous linkage
		int formerChildID = parent.GetDescendant(i);
		if (formerChildID != InvalidID)
		{
			HamTimelineNode formerChild = this.Nodes[formerChildID];

			// Remove former linkage
			formerChild.PreviousNodeIDs.Remove(parent.ID);

			// Set former child's parentage to the new child
			int slot = child.GetFreeDescendantSlot();
			child.SetDescendant(formerChild.ID, slot);
			formerChild.PreviousNodeIDs.Add(child.ID);
		}

		// Set new child's parentage and overwrite parent linkage
		parent.SetDescendant(child.ID, i);
		child.PreviousNodeIDs.Add(parent.ID);

		this.NodeLinkageDirty = true;
		SanityCheck();
	}

	public bool CanLinkCleanly(HamTimelineNode parent, HamTimelineNode child, int i = 0)
	{
		int formerChildID = parent.GetDescendant(i);
		if (formerChildID == InvalidID) { return true; }
		return (child.GetFreeDescendantSlot() != -1);
	}

	public bool CanRemoveCleanly(HamTimelineNode node)
	{
		int dependants = 0;
		List<int> descendants = node.GetDescendantIDs();
		if (descendants.Count == 0 && node.ID == this.OriginNodeID)
		{
			// If we're deleting the origin node, we need a replacement
			return false;
		}
		for (int i = 0; i < descendants.Count; ++i)
		{
			HamTimelineNode d = this.Nodes[descendants[i]];
			if (d.UniquelyParented) { dependants += 1; }
		}
		return dependants < 2;
	}

	public bool CanDeleteTree(HamTimelineNode node)
	{
		return node.ID != this.OriginNodeID;
	}

	// Visualization and Editing
	// Walk up the tree until a dialog node is found
	public HamDialogNode GetLastDialogNode(HamTimelineNode node)
	{
		if (node.Type == TimelineNodeType.Dialog) { return node as HamDialogNode; }
		for (int i = 0; i < node.PreviousNodeIDs.Count; ++i)
		{
			HamDialogNode last = GetLastDialogNode(this.Nodes[node.PreviousNodeIDs[i]]);
			if (last != null)
			{
				return last;
			}
		}
		return null;
	}

	private void SanityCheck()
	{
		bool passed = true;

		// Check for unique parent list, and nodes that contain themselves as their own parent
		foreach(HamTimelineNode node in this.Nodes.Values)
		{
			if (node.PreviousNodeIDs.Count != node.PreviousNodeIDs.Distinct().Count())
			{
				passed = false;
				Debug.LogError("Duplicate parent IDs found: " + node.Describe());
			}
			if (node.PreviousNodeIDs.Contains(node.ID))
			{
				passed = false;
				Debug.LogError("Node lists itself as a parent: " + node.Describe());
			}
			for (int i = 0; i < node.PreviousNodeIDs.Count; ++i)
			{
				if (!this.Nodes.ContainsKey(node.PreviousNodeIDs[i]))
				{
					passed = false;
					Debug.LogError("Missing node " + node.PreviousNodeIDs[i] + " found as parent of " + node.Describe());
				}
			}
		}

		HashSet<int> visited = new HashSet<int>();
		// Check for islands, and nodes that point to themselves
		Queue<int> queue = new Queue<int>();
		queue.Enqueue(this.OriginNodeID);
		while (queue.Count > 0)
		{
			int nodeID = queue.Dequeue();
			if (visited.Contains(nodeID)) { continue; }
			visited.Add(nodeID);

			if (!this.Nodes.ContainsKey(nodeID))
			{
				passed = false;
				Debug.LogError("Node id not found in nodes: " + nodeID);
				continue;
			}
			HamTimelineNode node = this.Nodes[nodeID];
			List<int> d = node.GetDescendantIDs();
			for (int i = 0; i < d.Count; ++i)
			{
				queue.Enqueue(d[i]);
				if (d[i] == node.ID)
				{
					passed = false;
					Debug.LogError("Node lists itself as a descendant: " + node.Describe());
				}
			}
		}

		if (visited.Count != this.Nodes.Count)
		{
			foreach (HamTimelineNode node in this.Nodes.Values)
			{
				if (!visited.Contains(node.ID))
				{
					passed = false;
					Debug.LogError("Unlinked node found: " + node.Describe());
				}
			}
		}

		if (!passed)
		{
			Debug.LogError("Failed sanity check");
		}
	}

	public bool EvaluatePredicate(HamPredicate predicate, VariableValue instanceValue)
	{
		if (predicate.VariableID == InvalidID)
		{
			Debug.LogError("Predicate variable not set");
			return false;
		}
		HamTimelineVariable timelineVar = this.Variables[predicate.VariableID];
		return timelineVar.Compare(predicate.Comparison, instanceValue);
	}
}