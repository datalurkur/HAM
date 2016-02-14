using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class HamGame : MonoBehaviour
{
	public readonly string kVerticalAxis = "Vertical";
	public readonly string kHorizontalAxis = "Horizontal";
	public readonly string kSubmitButton = "Submit";
	public readonly string kCancelButton = "Cancel";

	public const float kRepeatDelay = 0.5f;
	public const float kRepeatSpeed = 0.2f;

	public string TimelinePath = "Timelines";
	public HamStage Stage;
	private HamTimelineInstance timelineInstance;

	protected void Start()
	{
		this.timelineInstance = new HamTimelineInstance(this.TimelinePath, OnHamEvent);
		this.timelineInstance.Start();
	}

	protected void Update()
	{
		if (Input.GetKeyDown(KeyCode.F))
		{
			GameObject current = EventSystem.current.currentSelectedGameObject;
			Debug.Log("Currently selected object: " + (current == null ? "NONE" : current.name));
		}

		if (!this.Stage.Selecting)
		{
			if (Input.GetButtonDown(kSubmitButton))
			{
				Advance();
			}
		}
	}

	public void Advance()
	{
		if (!this.Stage.Animating)
		{
			this.timelineInstance.Advance();
		}
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