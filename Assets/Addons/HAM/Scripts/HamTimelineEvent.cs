using UnityEngine;
using System.Collections.Generic;

public enum HamEventType
{
	SceneChanges,
	CharacterEnters,
	CharacterLeaves,
	Dialog,
	Choice,
	TimelineEnds
}

public class HamTimelineEvent
{
	public HamEventType Type;

	public HamTimelineEvent(HamEventType type) { this.Type = type; }
}

public class HamSceneChangesEvent : HamTimelineEvent
{
	public HamScene Scene;

	public HamSceneChangesEvent(HamScene scene) : base(HamEventType.SceneChanges)
	{
		this.Scene = scene;
	}
}

public class HamCharacterEntersEvent : HamTimelineEvent
{
	public HamCharacter Character;

	public HamCharacterEntersEvent(HamCharacter character) : base(HamEventType.CharacterEnters)
	{
		this.Character = character;
	}
}

public class HamCharacterLeavesEvent : HamTimelineEvent
{
	public HamCharacter Character;

	public HamCharacterLeavesEvent(HamCharacter character) : base(HamEventType.CharacterLeaves)
	{
		this.Character = character;
	}
}

public class HamDialogEvent : HamTimelineEvent
{
	public HamCharacter Speaker;
	public string Dialog;

	public HamDialogEvent(HamCharacter speaker, string dialog) : base(HamEventType.Dialog)
	{
		this.Speaker = speaker;
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

public class HamTimelineEndsEvent : HamTimelineEvent
{
	public HamTimelineEndsEvent() : base(HamEventType.TimelineEnds) {}
}