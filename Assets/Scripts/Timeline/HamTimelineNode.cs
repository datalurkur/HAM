using System.Collections.Generic;

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

		node.Pack(packer);
	}
	public static void Unpack(out HamTimelineNode node, DataUnpacker unpacker)
	{
		byte typeByte;
		unpacker.Unpack(out typeByte);
		TimelineNodeType type = (TimelineNodeType)typeByte;
		int id;
		unpacker.Unpack(out id);

		switch (type)
		{
		case TimelineNodeType.Dialog:
			node = new HamDialogNode(id);
			break;
		case TimelineNodeType.Branch:
			node = new HamBranchNode(id);
			break;
		case TimelineNodeType.Decision:
			node = new HamDecisionNode(id);
			break;
		case TimelineNodeType.Consequence:
			node = new HamConsequenceNode(id);
			break;
		default:
			node = null;
			return;
		}

		node.Unpack(unpacker);
	}

	public TimelineNodeType Type;
	public int ID;

	public HamTimelineNode()
	{
		this.Type = TimelineNodeType.Dialog;
		this.ID = HamTimeline.InvalidID;
	}

	public HamTimelineNode(TimelineNodeType type, int id)
	{
		this.Type = type;
		this.ID = id;
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

	public HamDialogNode(int id) : base(TimelineNodeType.Dialog, id)
	{
		this.SceneID = HamTimeline.InvalidID;
		this.CharacterIDs = new List<int>();
		this.SpeakerID = HamTimeline.InvalidID;
		this.Dialog = "Invalid Dialog";

		this.NextNodeID = HamTimeline.InvalidID;
	}

	public HamDialogNode(int id, int sceneID, int speakerID, string dialog) : base(TimelineNodeType.Dialog, id)
	{
		this.SceneID = sceneID;	
		this.CharacterIDs = new List<int>();
		this.SpeakerID = speakerID;
		this.Dialog = dialog;

		this.NextNodeID = HamTimeline.InvalidID;
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
	public HamBranchNode(int id) : base(TimelineNodeType.Branch, id)
	{

	}

	/*
	public HamBranchNode(int id) : base(TimelineNodeType.Branch, id)
	{

	}
	*/

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

		public Decision()
		{
			this.DecisionText = "Invalid Decision Text";
			this.IsDialog = false;
			this.Predicates = new List<HamPredicate>();
		}
		public Decision(string text, bool isDialog)
		{
			this.DecisionText = text;
			this.IsDialog = isDialog;
			this.Predicates = new List<HamPredicate>();
		}
		public void Pack(DataPacker packer)
		{
			packer.Pack(this.DecisionText);
			packer.Pack(this.IsDialog);
			// TODO - Pack Predicates
		}
		public void Unpack(DataUnpacker unpacker)
		{
			unpacker.Unpack(out this.DecisionText);
			unpacker.Unpack(out this.IsDialog);
			// TODO - Unpack Predicates
		}
	}

	public List<Decision> Decisions;

	public HamDecisionNode(int id) : base(TimelineNodeType.Decision, id)
	{
		this.Decisions = new List<Decision>();
	}

	/*
	public HamDecisionNode(int id) : base(TimelineNodeType.Decision, id)
	{
		this.Decisions = new List<Decision>();
	}
	*/

	public override void Pack(DataPacker packer)
	{
		// TODO - Pack Decisions
	}
	public override void Unpack(DataUnpacker unpacker)
	{
		// TODO - Unpack Decisions	
	}
}

public class HamConsequenceNode : HamTimelineNode
{
	public HamConsequenceNode(int id) : base(TimelineNodeType.Consequence, id)
	{

	}

	/*
	public HamConsequenceNode(int id) : base(TimelineNodeType.Consequence, id)
	{

	}
	*/

	public override void Pack(DataPacker packer)
	{

	}
	public override void Unpack(DataUnpacker unpacker)
	{
		
	}
}