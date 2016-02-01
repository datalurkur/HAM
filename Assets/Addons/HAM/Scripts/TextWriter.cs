using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TextWriter : MonoBehaviour
{
	// Characters per second
	public float Speed = 1f;

	public Text TextContainer;

	public bool Writing
	{
		get { return this.textDirty; }
	}

	private string textToWrite;
	private int currentIndex;
	private float timeToNextCharacter;

	private string textBuffer;

	private bool textDirty;

	protected void Awake()
	{
		this.textBuffer = "";
		this.currentIndex = 0;
		this.textDirty = false;
		this.timeToNextCharacter = 0f;
	}

	protected void Update()
	{
		if (!this.textDirty) { return; }

		this.timeToNextCharacter -= Time.deltaTime;
		while (this.timeToNextCharacter < 0f && this.textDirty)
		{
			this.textBuffer += this.textToWrite[this.currentIndex];
			this.currentIndex += 1;

			this.TextContainer.text = this.textBuffer;

			if (this.currentIndex >= this.textToWrite.Length)
			{
				this.textDirty = false;
			}
			this.timeToNextCharacter += (1f / this.Speed);
		}
	}

	public void ClearText()
	{
		this.textBuffer = "";
		this.TextContainer.text = "";
	}

	public void WriteText(string text, bool clearPrevious)
	{
		if (clearPrevious)
		{
			this.textBuffer = "";
		}

		if (text.Length > 0)
		{
			this.textToWrite = text;
			this.currentIndex = 0;
			this.timeToNextCharacter = (1f / this.Speed);

			this.textDirty = true;
		}
	}
}
