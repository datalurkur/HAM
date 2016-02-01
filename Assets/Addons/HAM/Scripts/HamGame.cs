using UnityEngine;
using System;
using System.Collections.Generic;

public class HamGame : MonoBehaviour
{
	public string TimelinePath = "Timelines";
	public HamStage Stage;
	private HamTimelineInstance timelineInstance;

	protected void Start()
	{
		this.timelineInstance = new HamTimelineInstance(this.TimelinePath, OnHamEvent);
		this.timelineInstance.Advance();
	}

	protected void Update()
	{
		// TODO - Change these to be generic controls - function on both controller and keyboard / mouse
		if (this.Stage.Selecting)
		{
			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				this.Stage.HighlightPrevious();
			}
			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				this.Stage.HighlightNext();
			}
			if (Input.GetKeyDown(KeyCode.Space))
			{
				this.Stage.SelectCurrent();
			}
		}
		else
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				AdvanceNormally();
			}
		}
	}

	private void AdvanceNormally()
	{
		this.timelineInstance.Advance();
	}

	protected void OnHamEvent(HamTimelineEvent eventData)
	{
		switch (eventData.Type)
		{
			case HamEventType.SceneChanges:
			{
				HamSceneChangesEvent evt = (HamSceneChangesEvent)eventData;
				this.Stage.SceneBar.text = evt.Scene.Name;
				break;
			}
			case HamEventType.CharacterEnters:
			{
				HamCharacterEntersEvent evt = (HamCharacterEntersEvent)eventData;
				Debug.Log("Character " + evt.Character.Name + " enters");
				break;
			}
			case HamEventType.CharacterLeaves:
			{
				HamCharacterLeavesEvent evt = (HamCharacterLeavesEvent)eventData;
				Debug.Log("Character " + evt.Character.Name + " leaves");
				break;
			}
			case HamEventType.Dialog:
			{
				HamDialogEvent evt = (HamDialogEvent)eventData;
				this.Stage.SpeakerBar.text = evt.Speaker.Name;
				this.Stage.Writer.WriteText(evt.Dialog, true);
				break;
			}
			case HamEventType.Choice:
			{
				HamChoiceEvent evt = (HamChoiceEvent)eventData;
				Dictionary<int, string> decisions = new Dictionary<int, string>();
				foreach (int key in evt.Choices.Keys)
				{
					HamDecisionNode.Decision d = evt.Choices[key];
					string decisionText = evt.Choices[key].IsDialog ? String.Format("\"{0}\"", d.DecisionText) : d.DecisionText;
					decisions.Add(key, decisionText);
				}
				this.Stage.AwaitSelection(decisions, (key) => { this.timelineInstance.Advance((int)key); });
				break;
			}
		}
	}
}