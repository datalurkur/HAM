using UnityEditor;
using UnityEngine;

public abstract class ModalWindow : EditorWindow
{
    public const int kTitleBarHeight = 20;
    public const int kButtonAreaHeight = 20;

    public string Title;

    protected void OnGUI()
    {
    	bool done = false;
        GUILayout.BeginArea(new Rect(0, 0, position.width, kTitleBarHeight));
        GUILayout.Label(this.Title);
        GUILayout.EndArea();
 
        GUILayout.BeginArea(new Rect(0, kTitleBarHeight, position.width, position.height - kTitleBarHeight - kButtonAreaHeight));
        Draw();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, position.height - kButtonAreaHeight, position.width, kButtonAreaHeight));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("OK") || WasEnterPressed())
        {
        	Confirm();
        	done = true;
        }
        if (GUILayout.Button("Cancel"))
        {
        	done = true;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        if (done) { this.Close(); }
    }

    protected abstract void Draw();
    protected abstract void Confirm();

    private bool WasEnterPressed()
    {
    	return (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Return);
    }
}

public class ModalTextWindow : ModalWindow
{
	public static void Popup(string title, Callback callback)
	{
		ModalTextWindow w = CreateInstance<ModalTextWindow>();
		w.Title = title;
		w.OnConfirm = callback;
        w.position = new Rect(Screen.width / 2, Screen.height / 2, 200, 75);
        // ShowPopup doesn't properly absorb keyboard events - another window immediately steals focus when a keyboard event comes through
		//w.ShowPopup();
        w.Show();
	}

	public delegate void Callback(string textData);
    public string TextData = "";

	private Callback OnConfirm;
	private readonly string kControlLabel = "ModalTextData";

    protected override void Draw()
    {
        GUI.SetNextControlName(kControlLabel);
        this.TextData = GUILayout.TextField(this.TextData);
        GUI.FocusControl(kControlLabel);
    }

    protected override void Confirm()
    {
    	if (this.TextData.Length > 0)
    	{
	    	this.OnConfirm(this.TextData);
    	}
    }
}