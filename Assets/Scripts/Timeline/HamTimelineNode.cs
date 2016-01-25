using System.Collections.Generic;
using UnityEngine;

public enum TimelineNodeType
{
	Dialog = 0,
	Branch,
	Decision,
	Consequence
}

public abstract class HamTimelineNode
{
	public static void Pack(HamTimelineNode node, DataPacker packer)
	{
		packer.Pack((byte)node.Type);
		packer.Pack(node.ID);

		packer.Pack(node.PreviousNodeIDs.Count);
		for (int i = 0; i < node.PreviousNodeIDs.Count; ++i)
		{
			packer.Pack(node.PreviousNodeIDs[i]);
		}

		node.Pack(packer);
	}
	public static void Unpack(out HamTimelineNode node, DataUnpacker unpacker)
	{
		byte typeByte;
		unpacker.Unpack(out typeByte);
		TimelineNodeType type = (TimelineNodeType)typeByte;

		int id;
		unpacker.Unpack(out id);

		int numPrevIDs;
		unpacker.Unpack(out numPrevIDs);
		List<int> previousNodeIDs = new List<int>();
		for (int i = 0; i < numPrevIDs; ++i)
		{
			int prevID;
			unpacker.Unpack(out prevID);
			previousNodeIDs.Add(prevID);
		}

		switch (type)
		{
		case TimelineNodeType.Dialog:
			node = new HamDialogNode();
			break;
		case TimelineNodeType.Branch:
			node = new HamBranchNode();
			break;
		case TimelineNodeType.Decision:
			node = new HamDecisionNode();
			break;
		case TimelineNodeType.Consequence:
			node = new HamConsequenceNode();
			break;
		default:
			node = null;
			return;
		}
		node.ID = id;
		node.Type = type;
		node.PreviousNodeIDs = previousNodeIDs;

		node.Unpack(unpacker);
	}

	public TimelineNodeType Type;
	public int ID;
	public List<int> PreviousNodeIDs;

	public HamTimelineNode() { }
	public HamTimelineNode(TimelineNodeType type, int id)
	{
		this.PreviousNodeIDs = new List<int>();
		this.Type = type;
		this.ID = id;
	}

	public void AddPreviousNode(int id)
	{
		if (this.PreviousNodeIDs.Contains(id))
		{
			Debug.LogError("Duplicate previous node");
			return;
		}
		this.PreviousNodeIDs.Add(id);
	}
	public void RemovePreviousNode(int id)
	{
		if (!this.PreviousNodeIDs.Contains(id))
		{
			Debug.LogError("Previous node not found");
			return;
		}
		this.PreviousNodeIDs.Remove(id);
	}

	// Visualization and Editing
	// Walk up the tree until a decision node is found
	public HamDialogNode GetLastDialogNode(HamTimeline timeline)
	{
		if (this.Type == TimelineNodeType.Dialog) { return this as HamDialogNode; }
		for (int i = 0; i < this.PreviousNodeIDs.Count; ++i)
		{
			HamDialogNode last = timeline.Nodes[this.PreviousNodeIDs[i]].GetLastDialogNode(timeline);
			if (last != null)
			{
				return last;
			}
		}
		return null;
	}

	// Walk up to the parent and determine what choice / branch this node is
	// This is used for placing the node in the overview and determining the node preview contents for previous nodes
	public int GetDescendantIndex(HamTimeline timeline, int previousNodeIndex)
	{
		if (previousNodeIndex >= this.PreviousNodeIDs.Count) { return -1; }
		int prevID = this.PreviousNodeIDs[previousNodeIndex];
		return timeline.Nodes[prevID].GetIndexOfDescendant(this.ID);
	}
	public abstract int GetIndexOfDescendant(int descendantID);
	public abstract void SetNextNode(HamTimeline timeline, HamTimelineNode child, int index = -1);
	public abstract List<int> GetDescendantIDs();

	public abstract void Pack(DataPacker packer);
	public abstract void Unpack(DataUnpacker unpacker);
}

public class HamDialogNode : HamTimelineNode
{
	public int SceneID;
	public List<int> CharacterIDs;
	public int SpeakerID;
	public string Dialog;

	public int NextNodeID;

	public HamDialogNode() { }
	public HamDialogNode(int id, int sceneID, int speakerID, string dialog, List<int> characters) : base(TimelineNodeType.Dialog, id)
	{
		this.SceneID = sceneID;	
		if (characters != null)
		{
			this.CharacterIDs = new List<int>(characters);
		}
		else
		{
			this.CharacterIDs = new List<int>();
		}
		this.SpeakerID = speakerID;
		this.Dialog = dialog;

		this.NextNodeID = HamTimeline.InvalidID;
	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		return (descendantID == this.NextNodeID) ? 0 : -1;
	}

	public override void SetNextNode(HamTimeline timeline, HamTimelineNode node, int index = -1)
	{
		HamTimelineNode previousNode = null;
		if (this.NextNodeID != HamTimeline.InvalidID)
		{
			previousNode = timeline.Nodes[this.NextNodeID];
			previousNode.RemovePreviousNode(this.ID);
		}

		this.NextNodeID = node.ID;
		node.AddPreviousNode(this.ID);

		if (previousNode != null)
		{
			node.SetNextNode(timeline, previousNode, -1);
		}
	}

	public override List<int> GetDescendantIDs()
	{
		List<int> ids = new List<int>();
		if (this.NextNodeID != HamTimeline.InvalidID)
		{
			ids.Add(this.NextNodeID);
		}
		return ids;
	}

	public override void Pack(DataPacker packer)
	{
		packer.Pack(this.SceneID);

		packer.Pack(this.CharacterIDs.Count);
		for (int i = 0; i < this.CharacterIDs.Count; ++i)
		{
			packer.Pack(this.CharacterIDs[i]);
		}

		packer.Pack(this.SpeakerID);
		packer.Pack(this.Dialog);
		packer.Pack(this.NextNodeID);
	}
	public override void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.SceneID);

		int size;
		unpacker.Unpack(out size);
		this.CharacterIDs = new List<int>();
		for (int i = 0; i < size; ++i)
		{
			int id;
			unpacker.Unpack(out id);
			this.CharacterIDs.Add(id);
		}

		unpacker.Unpack(out this.SpeakerID);
		unpacker.Unpack(out this.Dialog);
		unpacker.Unpack(out this.NextNodeID);
	}
}

public class HamBranchNode : HamTimelineNode
{
	public HamBranchNode() { }
	public HamBranchNode(int id) : base(TimelineNodeType.Branch, id)
	{

	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		return -1;
	}

	public override void SetNextNode(HamTimeline timeline, HamTimelineNode node, int index = -1)
	{
	}

	public override List<int> GetDescendantIDs()
	{
		List<int> ids = new List<int>();
		return ids;
	}

	public override void Pack(DataPacker packer)
	{

	}
	public override void Unpack(DataUnpacker unpacker)
	{
		
	}
}

public class HamDecisionNode : HamTimelineNode
{
	public class Decision
	{
		public string DecisionText;
		public bool IsDialog;
		public List<HamPredicate> Predicates;

		public int NextNodeID;

		public Decision() { }
		public Decision(string text, bool isDialog)
		{
			this.DecisionText = text;
			this.IsDialog = isDialog;
			this.Predicates = new List<HamPredicate>();
			this.NextNodeID = HamTimeline.InvalidID;
		}

		public void Pack(DataPacker packer)
		{
			packer.Pack(this.DecisionText);
			packer.Pack(this.IsDialog);

			packer.Pack(this.Predicates.Count);
			for (int i = 0; i < this.Predicates.Count; ++i)
			{
				this.Predicates[i].Pack(packer);
			}

			packer.Pack(this.NextNodeID);
		}
		public void Unpack(DataUnpacker unpacker)
		{
			unpacker.Unpack(out this.DecisionText);
			unpacker.Unpack(out this.IsDialog);

			int size;
			unpacker.Unpack(out size);
			this.Predicates = new List<HamPredicate>();
			for (int i = 0; i < size; ++i)
			{
				HamPredicate p = new HamPredicate();
				p.Unpack(unpacker);
				this.Predicates.Add(p);
			}

			unpacker.Unpack(out this.NextNodeID);
		}
	}

	public List<Decision> Decisions;

	public HamDecisionNode() { }
	public HamDecisionNode(int id) : base(TimelineNodeType.Decision, id)
	{
		this.Decisions = new List<Decision>();
	}

	public Decision AddDecision(string text, bool isDialog)
	{
		Decision d = new Decision(text, isDialog);
		this.Decisions.Add(d);
		return d;
	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		for (int i = 0; i < this.Decisions.Count; ++i)
		{
			if (this.Decisions[i].NextNodeID == descendantID)
			{
				return i;
			}
		}
		return -1;
	}

	public override void SetNextNode(HamTimeline timeline, HamTimelineNode node, int index = -1)
	{
		if (index == -1 || index >= this.Decisions.Count)
		{
			// Add a new decision for this linkage
			Decision d = AddDecision("Autogenerated Decision", false);
			d.NextNodeID = node.ID;
			node.AddPreviousNode(this.ID);
		}
		else
		{
			// Replace or set the next node on an existing decision
			Decision d = this.Decisions[index];

			HamTimelineNode previousNode = null;
			if (d.NextNodeID != HamTimeline.InvalidID)
			{
				previousNode = timeline.Nodes[d.NextNodeID];
				previousNode.RemovePreviousNode(this.ID);
			}

			d.NextNodeID = node.ID;
			node.AddPreviousNode(this.ID);

			if (previousNode != null)
			{
				node.SetNextNode(timeline, previousNode, -1);
			}
		}
	}

	public override List<int> GetDescendantIDs()
	{
		List<int> ids = new List<int>();
		for (int i = 0; i < this.Decisions.Count; ++i)
		{
			if (this.Decisions[i].NextNodeID != HamTimeline.InvalidID)
			{
				ids.Add(this.Decisions[i].NextNodeID);
			}
		}
		return ids;
	}

	public override void Pack(DataPacker packer)
	{
		packer.Pack(this.Decisions.Count);
		for (int i = 0; i < this.Decisions.Count; ++i)
		{
			this.Decisions[i].Pack(packer);
		}
	}
	public override void Unpack(DataUnpacker unpacker)
	{
		int numDecisions;
		unpacker.Unpack(out numDecisions);
		this.Decisions = new List<Decision>();
		for (int i = 0; i < numDecisions; ++i)
		{
			Decision d = new Decision();
			d.Unpack(unpacker);
			this.Decisions.Add(d);
		}
	}
}

public class HamConsequenceNode : HamTimelineNode
{
	public HamConsequenceNode() { }
	public HamConsequenceNode(int id) : base(TimelineNodeType.Consequence, id)
	{

	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		return -1;
	}

	public override void SetNextNode(HamTimeline timeline, HamTimelineNode node, int index = -1)
	{
	}

	public override List<int> GetDescendantIDs()
	{
		List<int> ids = new List<int>();
		return ids;
	}

	public override void Pack(DataPacker packer)
	{

	}
	public override void Unpack(DataUnpacker unpacker)
	{
		
	}
}