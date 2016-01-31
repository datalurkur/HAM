using UnityEngine;
using System.Collections.Generic;

public enum HamEventType
{
	SceneChanges,
	CharacterEnters,
	CharacterLeaves,
	Dialog,
	Choice
}

public class HamTimelineEvent
{
	public HamEventType Type;

	public HamTimelineEvent(HamEventType type) { this.Type = type; }
}

public class HamSceneChangesEvent : HamTimelineEvent
{
	public int SceneID;

	public HamSceneChangesEvent(int newSceneID) : base(HamEventType.SceneChanges)
	{
		this.SceneID = newSceneID;
	}
}

public class HamCharacterEntersEvent : HamTimelineEvent
{
	public int CharacterID;

	public HamCharacterEntersEvent(int characterID) : base(HamEventType.CharacterEnters)
	{
		this.CharacterID = characterID;
	}
}

public class HamCharacterLeavesEvent : HamTimelineEvent
{
	public int CharacterID;

	public HamCharacterLeavesEvent(int characterID) : base(HamEventType.CharacterLeaves)
	{
		this.CharacterID = characterID;
	}
}

public class HamDialogEvent : HamTimelineEvent
{
	public int SpeakerID;
	public string Dialog;

	public HamDialogEvent(int speakerID, string dialog) : base(HamEventType.Dialog)
	{
		this.SpeakerID = speakerID;
		this.Dialog = dialog;
	}
}

public class HamChoiceEvent : HamTimelineEvent
{
	public Dictionary<int, HamDecisionNode.Decision> Choices;

	public HamChoiceEvent() : base(HamEventType.Choice)
	{
		this.Choices = new Dictionary<int, HamDecisionNode.Decision>();
	}

	public void AddChoice(int id, HamDecisionNode.Decision decision)
	{
		this.Choices[id] = decision;
	}
}