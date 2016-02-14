using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HamStage : MonoBehaviour
{
	public GameObject ClickablePrefab;

	public GameObject StageContainer;

	public delegate void SelectionMade(int key);

	public bool Animating
	{
		get
		{
			return this.Selecting || this.Writer.Writing;
		}
	}

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
		bool first = true;
		foreach (int key in selections.Keys)
		{
			GameObject clickable = Instantiate(this.ClickablePrefab);
			clickable.name = "Decision_" + selections[key];
			clickable.transform.SetParent(this.ClickableContainer.transform, false);

			clickable.GetComponentInChildren<Text>().text = selections[key];
			int thisKey = key;
			clickable.GetComponentInChildren<Button>().onClick.AddListener(() => { MakeSelection(thisKey); });

			clickables.Add(clickable);

			if (first)
			{
				EventSystem.current.SetSelectedGameObject(clickable);
				first = false;
			}
		}
		this.Selecting = true;
		this.selectionCallback = callback;
	}

	public void MakeSelection(int key)
	{
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
