using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;

class HamTimelineEditor : EditorWindow
{
    [MenuItem ("HAM/Timeline Editor")]
    public static void  ShowWindow()
    {
        EditorWindow.GetWindow(typeof(HamTimelineEditor));
    }
    
    private HamTimeline ActiveTimeline;

    protected void OnGUI()
    {
    	if (this.ActiveTimeline == null)
    	{
    		TimelineSelection();
    	}
    	else
    	{
    		TimelineEditing();
    	}
    }

    private void TimelineSelection()
    {
    	GUILayout.Label("Select a timeline for editing");

    	string[] timelines = GetAllTimelines();
    	for (int i = 0; i < timelines.Length; ++i)
    	{
    		EditorGUILayout.BeginVertical();
    		GUILayout.Label(timelines[i]);
    		EditorGUILayout.EndVertical();
    	}
    }

    private void TimelineEditing()
    {

    }

    // Saving and Loading
    // ==================================================================================
    private string[] GetAllTimelines()
    {
    	string path = GetTimelinePath();
		return Directory.GetFiles(path);
    }
    private void SaveTimeline(bool close = false)
    {
    	if (this.ActiveTimeline == null)
    	{
    		Debug.LogError("No active timeline");
    		return;
    	}

    	string path = Path.Combine(GetTimelinePath(), this.ActiveTimeline.Name);

    	XmlSerializer serializer = new XmlSerializer(typeof(HamTimeline));
		FileStream stream = new FileStream(path, FileMode.Create);
		serializer.Serialize(stream, this);
		stream.Close();

		if (close)
		{
			this.ActiveTimeline = null;
		}
    }
    private void LoadTimeline(string name, bool createIfNotExist = false)
    {
    	string path = Path.Combine(GetTimelinePath(), name);
    	if (!File.Exists(path))
    	{
    		if (createIfNotExist)
    		{
    			this.ActiveTimeline = new HamTimeline();
    			this.ActiveTimeline.Name = name;
    			SaveTimeline();
    		}
    		else
    		{
    			Debug.LogError("Timeline " + name + " does not exist");
    		}
    	}
    	else
    	{
    		try
    		{
		    	XmlSerializer serializer = new XmlSerializer(typeof(HamTimeline));
			 	FileStream stream = new FileStream(path, FileMode.Open);
			 	this.ActiveTimeline = serializer.Deserialize(stream) as HamTimeline;
				stream.Close();
    		}
    		catch(Exception e)
    		{
    			Debug.LogError("Failed to load timeline " + name + ": " + e.Message);
    		}
    	}
    }
    private string GetTimelinePath()
    {
    	string path = Path.Combine(Application.dataPath, "Timelines");
    	if (!Directory.Exists(path))
    	{
    		Directory.CreateDirectory(path);
    	}
		return path;
    }
    // ==================================================================================
}