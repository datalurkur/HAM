using UnityEngine;
using System;
using System.Collections.Generic;

public class HamGame : MonoBehaviour
{
	public string TimelinePath = "Timelines";
	private HamTimelineInstance timelineInstance;

	protected void Start()
	{
		this.timelineInstance = new HamTimelineInstance(this.TimelinePath, OnHamEvent);
	}

	protected void Update()
	{
		// DEBUG CODE
		if (Input.GetKeyDown(KeyCode.Space))
		{
			AdvanceNormally();
		}
		if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			MakeChoice(0);
		}
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			MakeChoice(1);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			MakeChoice(2);
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			MakeChoice(3);
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			MakeChoice(4);
		}
	}

	private void AdvanceNormally()
	{
		this.timelineInstance.Advance();
	}

	private void MakeChoice(int choice)
	{
		this.timelineInstance.Advance(choice);
	}

	protected void OnHamEvent(HamTimelineEvent eventData)
	{
		switch (eventData.Type)
		{
			case HamEventType.SceneChanges:
			{
				HamSceneChangesEvent evt = (HamSceneChangesEvent)eventData;
				Debug.Log("Scene changes to " + evt.SceneID);
				break;
			}
			case HamEventType.CharacterEnters:
			{
				HamCharacterEntersEvent evt = (HamCharacterEntersEvent)eventData;
				Debug.Log("Character " + evt.CharacterID + " enters");
				break;
			}
			case HamEventType.CharacterLeaves:
			{
				HamCharacterLeavesEvent evt = (HamCharacterLeavesEvent)eventData;
				Debug.Log("Character " + evt.CharacterID + " leaves");
				break;
			}
			case HamEventType.Dialog:
			{
				HamDialogEvent evt = (HamDialogEvent)eventData;
				Debug.Log("Character " + evt.SpeakerID + " says " + evt.Dialog);
				break;
			}
			case HamEventType.Choice:
			{
				HamChoiceEvent evt = (HamChoiceEvent)eventData;
				Debug.Log("Choice:");
				foreach (int key in evt.Choices.Keys)
				{
					HamDecisionNode.Decision d = evt.Choices[key];
					string decisionText = evt.Choices[key].IsDialog ? String.Format("\"{0}\"", d.DecisionText) : d.DecisionText;
					Debug.Log(String.Format("{0} - {1}", key, decisionText));
				}
				break;
			}
		}
	}
}