using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HamNewMenu : MonoBehaviour
{
	public GameObject ButtonPrefab;
	public GameObject ContentContainer;
	public GameObject ButtonContainer;

	public void RegenerateButtons(List<string> timelineNames)
	{
		// Clean up any old buttons
		foreach (Transform child in ButtonContainer.transform)
		{
			Destroy(child.gameObject);
		}

		for (int i = 0; i < timelineNames.Count; ++i)
		{
			string timelineName = timelineNames[i];
			GameObject buttonObj = Instantiate(this.ButtonPrefab);
			buttonObj.transform.SetParent(this.ButtonContainer.transform, false);

			Button button = buttonObj.GetComponentInChildren<Button>();
			button.onClick.AddListener(() => { HamGame.Instance.StartNewGame(timelineName); });

			Text text = buttonObj.GetComponentInChildren<Text>();
			text.text = timelineName;
		}
	}
}
