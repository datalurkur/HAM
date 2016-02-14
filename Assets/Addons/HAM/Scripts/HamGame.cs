using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class HamGame : MonoBehaviour
{
	public static HamGame Instance;

	public enum GameState
	{
		MainMenu = 0,
		NewMenu  = 1,
		LoadMenu = 2,
		SaveMenu = 3,
		PlayGame = 4
	}
	private GameState state = GameState.MainMenu;

	public readonly string kVerticalAxis = "Vertical";
	public readonly string kHorizontalAxis = "Horizontal";
	public readonly string kSubmitButton = "Submit";
	public readonly string kCancelButton = "Cancel";

	public const float kRepeatDelay = 0.5f;
	public const float kRepeatSpeed = 0.2f;

	public HamStage Stage;
	public HamMainMenu MainMenu;
	public HamNewMenu NewMenu;
	public HamLoadMenu LoadMenu;
	public HamSaveMenu SaveMenu;

	private HamTimelineInstance timelineInstance;

	private Dictionary<string, string> availableTimelines;

	protected void Awake()
	{
		Instance = this;

		this.availableTimelines = new Dictionary<string, string>();
		List<string> paths = HamTimeline.GetAllTimelinePaths();
		for (int i = 0; i < paths.Count; ++i)
		{
			string name = HamTimeline.LoadName(paths[i]);
			this.availableTimelines.Add(name, paths[i]);
		}
	}

	protected void OnDestroy()
	{
		Instance = null;
	}

	protected void Start()
	{
		SwitchState(GameState.MainMenu);
	}

	protected void Update()
	{
		switch (this.state)
		{
			case GameState.PlayGame:
			{
				if (!this.Stage.Selecting)
				{
					if (Input.GetButtonDown(kSubmitButton))
					{
						Advance();
					}
				}
				break;
			}
		}
	}

	public void SwitchState(int newState) { SwitchState((GameState)newState); }

	public void SwitchState(GameState newState)
	{
		this.state = newState;
		switch (this.state)
		{
			case GameState.MainMenu:
				break;
			case GameState.NewMenu:
			{
				List<string> timelineNames = new List<string>();
				foreach (string key in this.availableTimelines.Keys)
				{
					timelineNames.Add(key);
				}
				this.NewMenu.RegenerateButtons(timelineNames);
				break;
			}
			case GameState.LoadMenu:
				break;
			case GameState.SaveMenu:
				break;
		}
		UpdateMenuVisibility();
	}

	public void Quit() { Application.Quit(); }

	public void StartNewGame(string timelineName)
	{
		string timelinePath = this.availableTimelines[timelineName];

		this.state = GameState.PlayGame;
		this.timelineInstance = new HamTimelineInstance(timelinePath, OnHamEvent);
		this.timelineInstance.Start();
		SwitchState(GameState.PlayGame);
	}

	public void Advance()
	{
		if (!this.Stage.Animating)
		{
			this.timelineInstance.Advance();
		}
	}

	protected void UpdateMenuVisibility()
	{
		this.MainMenu.ContentContainer.SetActive(this.state == GameState.MainMenu);
		this.NewMenu.ContentContainer.SetActive(this.state == GameState.NewMenu);
		this.LoadMenu.ContentContainer.SetActive(this.state == GameState.LoadMenu);
		this.SaveMenu.ContentContainer.SetActive(this.state == GameState.SaveMenu);
		this.Stage.StageContainer.SetActive(this.state == GameState.PlayGame);
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
			case HamEventType.TimelineEnds:
				SwitchState(GameState.MainMenu);
				break;
		}
	}
}