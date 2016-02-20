using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;
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
	public GameObject MenuCommon;
	public HamMainMenu MainMenu;
	public HamNewMenu NewMenu;
	public HamLoadMenu LoadMenu;
	public HamSaveMenu SaveMenu;

	private HamAnimatedMenu currentMenu = null;

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
				StartCoroutine(SwapMenus(this.MainMenu));
				break;
			case GameState.NewMenu:
			{
				List<string> timelineNames = new List<string>();
				foreach (string key in this.availableTimelines.Keys)
				{
					timelineNames.Add(key);
				}
				this.NewMenu.RegenerateButtons(timelineNames);
				StartCoroutine(SwapMenus(this.NewMenu));
				break;
			}
			case GameState.LoadMenu:
				StartCoroutine(SwapMenus(this.LoadMenu));
				break;
			case GameState.SaveMenu:
				StartCoroutine(SwapMenus(this.SaveMenu));
				break;
			default:
				StartCoroutine(SwapMenus(null));
				break;
		}
		this.Stage.StageContainer.SetActive(this.state == GameState.PlayGame);
		this.MenuCommon.SetActive(this.state != GameState.PlayGame);
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

	protected IEnumerator SwapMenus(HamAnimatedMenu newMenu)
	{
		if (this.currentMenu != null)
		{
			this.currentMenu.MenuActive = false;
			while (this.currentMenu.MenuHydrated) { yield return null; }
		}
		if (newMenu != null)
		{
			newMenu.MenuActive = true;
		}
		this.currentMenu = newMenu;
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