using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
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
        OverviewEditing,
        CharacterEditing,
        SceneEditing,
        VariableEditing
    }

    // Style helpers
    // ==================================================================================
    private GUISkin skin;
    private GUISkin Skin
    {
        get
        {
            if (this.skin == null)
            {
                this.skin = AssetDatabase.LoadAssetAtPath("Assets/Editor/Skin/HamSkin.guiskin", typeof(GUISkin)) as GUISkin;
            }
            return this.skin;
        }
    }
    private GUIStyle Style(string name)
    {
        return this.Skin.GetStyle(name);
    }
    // ==================================================================================

    // GUI Renderers
    // ==================================================================================
    protected void OnGUI()
    {
        this.titleContent = new GUIContent("Timeline Editor");

        GUI.skin = Skin;
        if (this.activeTimeline == null) { TimelineSelection(); }
        else { TimelineEditing(); }
        GUI.skin = null;
    }

    private HamTimeline activeTimeline;
    private string newTimelineName;
    private void TimelineSelection()
    {
        GUILayout.Label("Select Timeline", Style("Title"));

        List<string> timelines = GetAllTimelines();

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
            if (GUILayout.Button("Load " + Path.GetFileName(timelines[i])))
            {
                LoadTimeline(timelines[i]);
            }
            GUILayout.EndVertical();
        }
    }

    private EditingTab activeEditingTab = EditingTab.SingleNodeEditing;
    private void TimelineEditing()
    {
        GUILayout.Label("Editing " + this.activeTimeline.Name, Style("Title"));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) { SaveTimeline(); }
        if (GUILayout.Button("Save and Close"))
        {
            SaveTimeline(true);
            return;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Single Node Editing")) { this.activeEditingTab = EditingTab.SingleNodeEditing; }
        if (GUILayout.Button("Overview Editing")) { this.activeEditingTab = EditingTab.OverviewEditing; }
        if (GUILayout.Button("Character Editing")) { this.activeEditingTab = EditingTab.CharacterEditing; }
        if (GUILayout.Button("Variable Editing")) { this.activeEditingTab = EditingTab.VariableEditing; }
        if (GUILayout.Button("Scene Editing")) { this.activeEditingTab = EditingTab.SceneEditing; }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        GUILayout.BeginVertical(Style("box"));
        switch (this.activeEditingTab)
        {
        case EditingTab.SingleNodeEditing:
            SingleNodeEditing();
            break;
        case EditingTab.OverviewEditing:
            OverviewEditing();
            break;
        case EditingTab.CharacterEditing:
            CharacterEditing();
            break;
        case EditingTab.VariableEditing:
            VariableEditing();
            break;
        case EditingTab.SceneEditing:
            SceneEditing();
            break;
        }
        GUILayout.EndVertical();
    }

    private int selectedNode = HamTimeline.InvalidID;
    private void SingleNodeEditing()
    {
        if (this.selectedNode == HamTimeline.InvalidID)
        {
            this.selectedNode = this.activeTimeline.OriginNodeID;
        }
        HamTimelineNode node = this.activeTimeline.Nodes[this.selectedNode];
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
        GUILayout.Label("Dialog Node", Style("SubTitle"));

        GUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
        GUILayout.Label("Scene");
        if (GUILayout.Button(this.activeTimeline.Scenes[node.SceneID].Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Scenes.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.activeTimeline.Scenes[id].Name),
                    (id == node.SceneID),
                    (userData) => { node.SceneID = (int)userData; },
                    id
                );
            }
            menu.ShowAsContext();
        }
        GUILayout.EndHorizontal(); 

        GUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
        GUILayout.Label("Speaker");
        if (GUILayout.Button(this.activeTimeline.Characters[node.SpeakerID].Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Characters.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.activeTimeline.Characters[id].Name),
                    (id == node.SpeakerID),
                    (userData) => { node.SpeakerID = (int)userData; },
                    id
                );
            }
            menu.ShowAsContext();
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Dialog Content");
        node.Dialog = GUILayout.TextArea(node.Dialog);
    }

    private void BranchNodeEditing(HamBranchNode node)
    {
        GUILayout.Label("Branch Node", Style("SubTitle"));
    }

    private void DecisionNodeEditing(HamDecisionNode node)
    {
        GUILayout.Label("Decision Node", Style("SubTitle"));
    }

    private void OverviewEditing()
    {

    }

    private int selectedCharacter = HamTimeline.InvalidID;
    private void CharacterEditing()
    {
        if (this.selectedCharacter == HamTimeline.InvalidID || !this.activeTimeline.Characters.ContainsKey(this.selectedCharacter))
        {
            this.selectedCharacter = this.activeTimeline.NarratorID;
        }
        HamCharacter character = this.activeTimeline.Characters[this.selectedCharacter];
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
        GUILayout.Label("Characters");
        if (GUILayout.Button(character.Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Characters.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.activeTimeline.Characters[id].Name),
                    (id == this.selectedCharacter),
                    (userData) => { this.selectedCharacter = (int)userData; },
                    id
                );
            }
            menu.ShowAsContext();
        }
        if (GUILayout.Button("+", Style("SmallButton")))
        {
            this.selectedCharacter = this.activeTimeline.AddCharacter("New Character").ID;
        }
        GUILayout.EndHorizontal();

        character.Name = EditorGUILayout.TextField("Name", character.Name);
    }

    private void VariableEditing()
    {

    }

    private int selectedScene = HamTimeline.InvalidID;
    private void SceneEditing()
    {
        if (this.selectedScene == HamTimeline.InvalidID || !this.activeTimeline.Scenes.ContainsKey(this.selectedScene))
        {
            this.selectedScene = this.activeTimeline.DefaultSceneID;
        }
        HamScene scene = this.activeTimeline.Scenes[this.selectedScene];
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
        GUILayout.Label("Scenes");
        if (GUILayout.Button(scene.Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Scenes.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.activeTimeline.Scenes[id].Name),
                    (id == this.selectedScene),
                    (userData) => { this.selectedScene = (int)userData; },
                    id
                );
            }
            menu.ShowAsContext();
        }
        if (GUILayout.Button("+", Style("SmallButton")))
        {
            this.selectedScene = this.activeTimeline.AddScene("New Scene").ID;
        }
        GUILayout.EndHorizontal();

        scene.Name = EditorGUILayout.TextField("Name", scene.Name);
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
        if (this.activeTimeline == null)
        {
            Debug.LogError("No active timeline");
            return;
        }

        string path = Path.Combine(GetTimelinePath(), this.activeTimeline.Name);

        HamTimeline.Save(this.activeTimeline, path);

        if (close)
        {
            this.activeTimeline = null;
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
                this.activeTimeline = new HamTimeline();
                this.activeTimeline.Name = name;
                this.activeTimeline.DefaultInit();
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
                this.activeTimeline = HamTimeline.Load(path);
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