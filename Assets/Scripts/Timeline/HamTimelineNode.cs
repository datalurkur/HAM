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

	public virtual HamDialogNode GetLastDialogNode(HamTimeline timeline)
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

	public void SetNextNode(HamTimeline timeline, HamTimelineNode node)
	{
		if (this.NextNodeID != HamTimeline.InvalidID)
		{
			timeline.Nodes[this.NextNodeID].PreviousNodeIDs.Remove(this.ID);
		}
		this.NextNodeID = node.ID;
		node.PreviousNodeIDs.Add(this.ID);
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

	public void AddDecision(string text, bool isDialog)
	{
		Decision d = new Decision(text, isDialog);
		this.Decisions.Add(d);
	}

	public void SetNextNode(HamTimeline timeline, int i, HamTimelineNode node)
	{
		Decision d = this.Decisions[i];
		if (d.NextNodeID != HamTimeline.InvalidID)
		{
			timeline.Nodes[d.NextNodeID].PreviousNodeIDs.Remove(this.ID);
		}
		d.NextNodeID = node.ID;
		node.PreviousNodeIDs.Add(this.ID);
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

	public override void Pack(DataPacker packer)
	{

	}
	public override void Unpack(DataUnpacker unpacker)
	{
		
	}
}