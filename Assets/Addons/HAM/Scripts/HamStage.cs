using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HamStage : MonoBehaviour
{
	public GameObject ClickablePrefab;

	public delegate void SelectionMade(int key);

	public RawImage SceneBackground;
	public RawImage[] CharacterPortraits;
	public Text SceneBar;
	public Text SpeakerBar;
	public TextWriter Writer;

	public GameObject ClickableContainer;

	public bool Selecting;

	private int currentIndex;

	private List<GameObject> clickables;
	private List<int> selectionOrder;
	private SelectionMade selectionCallback;

	protected void Start()
	{
		this.Selecting = false;
		this.clickables = new List<GameObject>();
		this.selectionOrder = new List<int>();
	}

	public void AwaitSelection(Dictionary<int, string> selections, SelectionMade callback)
	{
		this.Writer.ClearText();
		foreach (int key in selections.Keys)
		{
			int thisKey = key;

			GameObject clickable = Instantiate(this.ClickablePrefab);
			clickable.transform.SetParent(this.ClickableContainer.transform, false);

			clickable.GetComponent<Text>().text = selections[key];

			EventTrigger trigger = clickable.AddComponent<EventTrigger>();

			EventTrigger.Entry enterEntry = new EventTrigger.Entry();
			enterEntry.eventID = EventTriggerType.PointerEnter;
			enterEntry.callback.AddListener((eventData) => { HighlightSelection(thisKey); });
			trigger.triggers.Add(enterEntry);

			EventTrigger.Entry clickEntry = new EventTrigger.Entry();
			clickEntry.eventID = EventTriggerType.PointerClick;
			clickEntry.callback.AddListener((eventData) => { MakeSelection(thisKey); });
			trigger.triggers.Add(clickEntry);

			this.clickables.Add(clickable);
			this.selectionOrder.Add(thisKey);
		}
		this.currentIndex = -1;
		this.Selecting = true;
		this.selectionCallback = callback;
	}

	// Driver events (from keyboard)
	public void HighlightNext()
	{
		this.currentIndex += 1;
		if (this.currentIndex >= this.selectionOrder.Count)
		{
			this.currentIndex = 0;
		}
		HighlightSelection(this.selectionOrder[this.currentIndex]);
	}
	public void HighlightPrevious()
	{
		this.currentIndex -= 1;
		if (this.currentIndex < 0)
		{
			this.currentIndex = this.selectionOrder.Count - 1;
		}
		HighlightSelection(this.selectionOrder[this.currentIndex]);
	}
	public void SelectCurrent()
	{
		if (this.currentIndex == -1) { return; }
		MakeSelection(this.selectionOrder[this.currentIndex]);
	}

	// Callback events (from click)
	public void HighlightSelection(int key)
	{
		Debug.Log("Selection " + key + " highlighted");
	}

	public void MakeSelection(int key)
	{
		Debug.Log("Selection " + key + " made");
		this.selectionCallback(key);
		this.selectionCallback = null;

		for (int i = 0; i < this.clickables.Count; ++i)
		{
			Destroy(this.clickables[i]);
		}	
		this.clickables.Clear();
		this.selectionOrder.Clear();
		this.Selecting = false;
	}
}
