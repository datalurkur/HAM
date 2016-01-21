using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

class HamTimelineEditor : EditorWindow
{
    [MenuItem ("HAM/Timeline Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(HamTimelineEditor));
    }

    private enum EditingTab
    {
        SingleNodeEditing,
        OverviewEditing
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

    private string newTimelineName;
    private void TimelineSelection()
    {
    	List<string> timelines = GetAllTimelines();

    	GUILayout.Label("Timeline Selection");

    	GUILayout.BeginVertical();
    	this.newTimelineName = EditorGUILayout.TextField("New Timeline Name", this.newTimelineName);
    	if (this.newTimelineName != null && this.newTimelineName.Length > 0 && !timelines.Contains(this.newTimelineName))
    	{
    		if (GUILayout.Button("Create " + this.newTimelineName))
    		{
    			LoadTimeline(this.newTimelineName, true);
    		}
    	}

    	for (int i = 0; i < timelines.Count; ++i)
    	{
    		GUILayout.BeginVertical();
    		if (GUILayout.Button("Load " + Path.GetFileName(timelines[i]), GUILayout.ExpandWidth(false)))
    		{
    			LoadTimeline(timelines[i]);
    		}
    		GUILayout.EndVertical();
    	}
    }

    private EditingTab activeEditingTab = EditingTab.SingleNodeEditing;
    private void TimelineEditing()
    {
        GUILayout.Label("Editing " + this.ActiveTimeline.Name);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.ExpandWidth(false))) { SaveTimeline(); }
        if (GUILayout.Button("Save and Close", GUILayout.ExpandWidth(false)))
        {
            SaveTimeline(true);
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Single Node Editing", GUILayout.ExpandWidth(false))) { this.activeEditingTab = EditingTab.SingleNodeEditing; }
        if (GUILayout.Button("Overview Editing", GUILayout.ExpandWidth(false))) { this.activeEditingTab = EditingTab.OverviewEditing; }
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        switch (this.activeEditingTab)
        {
        case EditingTab.SingleNodeEditing:
            SingleNodeEditing();
            break;
        case EditingTab.OverviewEditing:
            OverviewEditing();
            break;
        }
        GUILayout.EndVertical();
    }

    private int selectedNode = HamTimeline.InvalidID;
    private void SingleNodeEditing()
    {
        if (this.selectedNode == HamTimeline.InvalidID)
        {
            this.selectedNode = this.ActiveTimeline.OriginNodeID;
        }
        HamTimelineNode node = this.ActiveTimeline.Nodes[this.selectedNode];
        switch (node.Type)
        {
        case TimelineNodeType.Dialog:
            DialogNodeEditing(node as HamDialogNode);
            break;
        case TimelineNodeType.Branch:
            BranchNodeEditing(node as HamBranchNode);
            break;
        case TimelineNodeType.Decision:
            DecisionNodeEditing(node as HamDecisionNode);
            break;
        }
    }

    private void DialogNodeEditing(HamDialogNode node)
    {
        GUILayout.Label("Dialog Node");

        GUILayout.Label("Speaker");
        if (GUILayout.Button(this.ActiveTimeline.Characters[node.SpeakerID].Name, GUILayout.ExpandWidth(false)))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.ActiveTimeline.Characters.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.ActiveTimeline.Characters[id].Name),
                    (id == node.SpeakerID),
                    (userData) => { node.SpeakerID = (int)userData; },
                    id
                );
            }
            menu.ShowAsContext();
        }

        GUILayout.Label("Dialog Content");
        node.Dialog = GUILayout.TextArea(node.Dialog);
    }

    private void BranchNodeEditing(HamBranchNode node)
    {
        GUILayout.Label("Branch Node");
    }

    private void DecisionNodeEditing(HamDecisionNode node)
    {
        GUILayout.Label("Decision Node");
    }

    private void OverviewEditing()
    {

    }

    // Saving and Loading
    // ==================================================================================
    private List<string> GetAllTimelines()
    {
    	string path = GetTimelinePath();
		return new List<string>(Directory.GetFiles(path)).Where(t => !t.Contains(".meta")).ToList();
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
		serializer.Serialize(stream, this.ActiveTimeline);
		stream.Close();

		if (close)
		{
			this.ActiveTimeline = null;
            Repaint();
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