using System.Collections.Generic;
using System.Xml.Serialization;

public enum TimelineNodeType
{
	Dialog,
	Branch,
	Decision
}

[XmlInclude(typeof(HamDialogNode))]
[XmlInclude(typeof(HamBranchNode))]
[XmlInclude(typeof(HamDecisionNode))]
public abstract class HamTimelineNode
{
	public TimelineNodeType Type;
	public int ID;

	public int SceneID;
	public List<int> CharacterIDs;

	public HamTimelineNode()
	{
		this.Type = TimelineNodeType.Dialog;
		this.ID = HamTimeline.InvalidID;
		this.SceneID = HamTimeline.InvalidID;	
		this.CharacterIDs = new List<int>();
	}

	public HamTimelineNode(TimelineNodeType type, int id, int sceneID)
	{
		this.Type = type;
		this.ID = id;

		this.SceneID = sceneID;
		this.CharacterIDs = new List<int>();
	}
}

public class HamDialogNode : HamTimelineNode
{
	public int NextNodeID;
	public int SpeakerID;
	public string Dialog;

	public HamDialogNode()
	{
		this.NextNodeID = HamTimeline.InvalidID;
		this.SpeakerID = HamTimeline.InvalidID;
		this.Dialog = "Invalid Dialog";
	}

	public HamDialogNode(int id, int sceneID, int speakerID, string dialog) : base(TimelineNodeType.Dialog, id, sceneID)
	{
		this.NextNodeID = HamTimeline.InvalidID;
		this.SpeakerID = speakerID;
		this.Dialog = dialog;
	}
}

public class HamBranchNode : HamTimelineNode
{
	public HamBranchNode()
	{

	}

	public HamBranchNode(int id, int sceneID) : base(TimelineNodeType.Branch, id, sceneID)
	{

	}
}

public class HamDecisionNode : HamTimelineNode
{
	public HamDecisionNode()
	{

	}

	public HamDecisionNode(int id, int sceneID) : base(TimelineNodeType.Decision, id, sceneID)
	{

	}
}