using System;
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

	public bool UniquelyParented
	{
		get { return this.PreviousNodeIDs.Count < 2; }
	}

	public abstract void SetDescendant(int id, int index);
	public abstract int GetDescendant(int index);
	public abstract int GetFreeDescendantSlot();

	public abstract int GetIndexOfDescendant(int descendantID);
	public abstract List<int> GetDescendantIDs();

	public abstract void Pack(DataPacker packer);
	public abstract void Unpack(DataUnpacker unpacker);

	public abstract string Describe();
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

	public override void SetDescendant(int id, int index)
	{
		this.NextNodeID = id;
	}

	public override int GetDescendant(int index)
	{
		return this.NextNodeID;
	}

	public override int GetFreeDescendantSlot()
	{
		if (this.NextNodeID == HamTimeline.InvalidID)
		{
			return 0;
		}
		else
		{
			return -1;
		}
	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		return (descendantID == this.NextNodeID) ? 0 : -1;
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

	public override string Describe()
	{
		return String.Format("Dialog {0} : {1}", this.ID, this.Dialog);
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
	public List<HamPredicate> Predicates;
	public int DefaultNextID;

	public HamBranchNode() { }
	public HamBranchNode(int id) : base(TimelineNodeType.Branch, id)
	{
		this.DefaultNextID = HamTimeline.InvalidID;
		this.Predicates = new List<HamPredicate>();
	}

	public HamPredicate AddPredicate()
	{
		HamPredicate newPredicate = new HamPredicate();
		this.Predicates.Add(newPredicate);
		return newPredicate;
	}

	public override void SetDescendant(int id, int index)
	{
		if (index == this.Predicates.Count)
		{
			this.DefaultNextID = id;
		}
		else if (index > this.Predicates.Count)
		{
			AddPredicate().NextNodeID = id;
		}
		else
		{
			this.Predicates[index].NextNodeID = id;
		}
	}

	public override int GetDescendant(int index)
	{
		if (index == this.Predicates.Count)
		{
			return this.DefaultNextID;
		}
		else
		{
			return this.Predicates[index].NextNodeID;
		}
	}

	public override int GetFreeDescendantSlot()
	{
		if (this.DefaultNextID == HamTimeline.InvalidID)
		{
			return this.Predicates.Count;
		}
		else
		{
			for (int i = 0; i < this.Predicates.Count; ++i)
			{
				if (this.Predicates[i].NextNodeID == HamTimeline.InvalidID)
				{
					return i;
				}
			}
		}
		return this.Predicates.Count + 1;
	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		for (int i = 0; i < this.Predicates.Count; ++i)
		{
			if (this.Predicates[i].NextNodeID == descendantID) { return i; }
		}
		if (this.DefaultNextID == descendantID) { return this.Predicates.Count; }
		return -1;
	}

	public override List<int> GetDescendantIDs()
	{
		List<int> ids = new List<int>();
		for (int i = 0; i < this.Predicates.Count; ++i)
		{
			if (this.Predicates[i].NextNodeID != HamTimeline.InvalidID)
			{
				ids.Add(this.Predicates[i].NextNodeID);
			}	
		}
		if (this.DefaultNextID != HamTimeline.InvalidID)
		{
			ids.Add(this.DefaultNextID);
		}
		return ids;
	}

	public override string Describe()
	{
		return String.Format("Branch {0} : {1} branches", this.ID, this.Predicates.Count);
	}

	public override void Pack(DataPacker packer)
	{
		packer.Pack(this.DefaultNextID);
		packer.Pack(this.Predicates.Count);
		for (int i = 0; i < this.Predicates.Count; ++i)
		{
			this.Predicates[i].Pack(packer);
		}
	}
	public override void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.DefaultNextID);
		int size;
		this.Predicates = new List<HamPredicate>();
		unpacker.Unpack(out size);
		for (int i = 0; i < size; ++i)
		{
			HamPredicate predicate = new HamPredicate();
			predicate.Unpack(unpacker);
			this.Predicates.Add(predicate);
		}
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
		AddDecision("", false);
	}

	public Decision AddDecision(string text, bool isDialog)
	{
		Decision d = new Decision(text, isDialog);
		this.Decisions.Add(d);
		return d;
	}

	public override void SetDescendant(int id, int index)
	{
		if (index >= this.Decisions.Count)
		{
			AddDecision("", false).NextNodeID = id;
		}
		else
		{
			this.Decisions[index].NextNodeID = id;
		}
	}

	public override int GetDescendant(int index)
	{
		return this.Decisions[index].NextNodeID;
	}

	public override int GetFreeDescendantSlot()
	{
		for (int i = 0; i < this.Decisions.Count; ++i)
		{
			if (this.Decisions[i].NextNodeID == HamTimeline.InvalidID)
			{
				return i;
			}
		}
		return this.Decisions.Count;
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

	public override string Describe()
	{
		return String.Format("Decision {0} : {1} choices", this.ID, this.Decisions.Count);
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
	public int NextNodeID;
	public List<HamOperation> Operations;

	public HamConsequenceNode() { }
	public HamConsequenceNode(int id) : base(TimelineNodeType.Consequence, id)
	{
		this.NextNodeID = HamTimeline.InvalidID;
		this.Operations = new List<HamOperation>();
	}

	public void AddOperation()
	{
		this.Operations.Add(new HamOperation());
	}

	public override void SetDescendant(int id, int index)
	{
		this.NextNodeID = id;
	}

	public override int GetDescendant(int index)
	{
		return this.NextNodeID;
	}

	public override int GetFreeDescendantSlot()
	{
		if (this.NextNodeID == HamTimeline.InvalidID)
		{
			return 0;
		}
		else
		{
			return -1;
		}
	}

	public override int GetIndexOfDescendant(int descendantID)
	{
		return (descendantID == this.NextNodeID) ? 0 : -1;
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

	public override string Describe()
	{
		return String.Format("Consequence {0} : {1} operations", this.ID, this.Operations.Count);
	}

	public override void Pack(DataPacker packer)
	{
		packer.Pack(this.NextNodeID);
		packer.Pack(this.Operations.Count);
		for (int i = 0; i < this.Operations.Count; ++i)
		{
			this.Operations[i].Pack(packer);
		}
	}
	public override void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.NextNodeID);
		int size;
		unpacker.Unpack(out size);
		this.Operations = new List<HamOperation>();
		for (int i = 0; i < size; ++i)
		{
			HamOperation operation = new HamOperation();
			operation.Unpack(unpacker);
			this.Operations.Add(operation);
		}
	}
}