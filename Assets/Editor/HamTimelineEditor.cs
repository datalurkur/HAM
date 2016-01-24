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

    // Consts
    // ==================================================================================
    private const int kTopBarHeight = 100;
    // ==================================================================================

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
        //GUILayout.BeginArea(new Rect(0, 0, this.position.width, this.position.height), Style("FullWindow"));
        if (this.activeTimeline == null) { TimelineSelection(); }
        else { TimelineEditing(); }
        //GUILayout.EndArea();
        GUI.skin = null;
    }

    private HamTimeline activeTimeline;
    private EditingTab activeEditingTab = EditingTab.SingleNodeEditing;
    private int selectedNode = HamTimeline.InvalidID;
    private int selectedCharacter = HamTimeline.InvalidID;
    private int selectedScene = HamTimeline.InvalidID;
    private Vector2 scrollPosition;

    private void ResetEditorWindow()
    {
        this.activeTimeline = null;
        this.activeEditingTab = EditingTab.SingleNodeEditing;
        this.selectedNode = HamTimeline.InvalidID;
        this.selectedCharacter = HamTimeline.InvalidID;
        this.selectedScene = HamTimeline.InvalidID;
    }

    private void TimelineSelection()
    {
        GUILayout.Label("Select Timeline", Style("Title"));

        List<string> timelines = GetAllTimelines();

        if (GUILayout.Button("Create New Timeline"))
        {
            ModalTextWindow.Popup("Name New Timeline", LoadTimeline);
        }

        GUILayout.Label("Load", Style("SubTitle"));
        for (int i = 0; i < timelines.Count; ++i)
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button(Path.GetFileName(timelines[i])))
            {
                LoadTimeline(timelines[i]);
            }
            GUILayout.EndVertical();
        }
    }

    private void TimelineEditing()
    {
        Rect available = new Rect(0, kTopBarHeight, position.width, position.height - kTopBarHeight);
        GUILayout.BeginArea(available);
        switch (this.activeEditingTab)
        {
        case EditingTab.SingleNodeEditing:
            SingleNodeEditing();
            break;
        case EditingTab.OverviewEditing:
            OverviewEditing(available);
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
        GUILayout.EndArea();

        // Render the controls for saving and loading last, so that doing a save or load in the middle of a render doesn't throw exceptions
        RenderTopBar();
    }
    
    private void RenderTopBar()
    {
        GUILayout.BeginArea(new Rect(0, 0, position.width, kTopBarHeight));
        GUILayout.Label("Editing " + this.activeTimeline.Name, Style("Title"));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) { SaveTimeline(); }
        if (GUILayout.Button("Save and Close")) { SaveTimeline(true); }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        GUI.enabled = (this.activeEditingTab != EditingTab.SingleNodeEditing);
        if (GUILayout.Button("Single Node Editing")) { this.activeEditingTab = EditingTab.SingleNodeEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.OverviewEditing);
        if (GUILayout.Button("Overview Editing")) { this.activeEditingTab = EditingTab.OverviewEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.CharacterEditing);
        if (GUILayout.Button("Character Editing")) { this.activeEditingTab = EditingTab.CharacterEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.VariableEditing);
        if (GUILayout.Button("Variable Editing")) { this.activeEditingTab = EditingTab.VariableEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.SceneEditing);
        if (GUILayout.Button("Scene Editing")) { this.activeEditingTab = EditingTab.SceneEditing; }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        GUILayout.EndArea();
    }

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

        PreviousNodesRow(node);

        // Node Editing
        GUILayout.BeginVertical(Style("box"));
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

        GUILayout.Label("Characters Present");
        for (int i = 0; i < node.CharacterIDs.Count; ++i)
        {
            HamCharacter c = this.activeTimeline.Characters[node.CharacterIDs[i]];
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(200));
            GUILayout.Label(c.Name);
            if (GUILayout.Button("-", Style("SmallButton")))
            {
                node.CharacterIDs.Remove(c.ID);
            }
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add Character"))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Characters.Keys)
            {
                if (!node.CharacterIDs.Contains(id))
                {
                    menu.AddItem(
                        new GUIContent(this.activeTimeline.Characters[id].Name),
                        false,
                        (userData) => { node.CharacterIDs.Add((int)userData); },
                        id
                    );
                }
            }
            menu.ShowAsContext();
        }
        GUILayout.EndVertical();
        GUILayout.Space(4);

        HamTimelineNode newNode;
        if (NextNodeBlock(node, node.NextNodeID, out newNode))
        {
            node.SetNextNode(this.activeTimeline, newNode);
            this.selectedNode = newNode.ID;
            Repaint();
        }
    }

    private void BranchNodeEditing(HamBranchNode node)
    {
        GUILayout.Label("Branch Node", Style("SubTitle"));
    }

    private void DecisionNodeEditing(HamDecisionNode node)
    {
        GUILayout.Label("Decision Node", Style("SubTitle"));

        PreviousNodesRow(node);

        GUILayout.BeginHorizontal();
        for (int i = 0; i < node.Decisions.Count; ++i)
        {
            SingleDecisionNodeEditing(node, i);
        }
        GUILayout.BeginVertical(Style("box"));
        if (GUILayout.Button("Add Decision", Style("FlexButton")))
        {
            node.AddDecision("New Decision", false);
            Repaint();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void SingleDecisionNodeEditing(HamDecisionNode node, int i)
    {
        HamDecisionNode.Decision d = node.Decisions[i];
        HamTimelineNode newNode;
        GUILayout.BeginVertical();

        GUILayout.BeginVertical(Style("box"));
        GUILayout.Label("Decision Text");
        d.DecisionText = GUILayout.TextField(d.DecisionText);

        d.IsDialog = GUILayout.Toggle(d.IsDialog, "Is Dialog");
        GUILayout.EndVertical();

        if (NextNodeBlock(node, d.NextNodeID, out newNode))
        {
            node.SetNextNode(this.activeTimeline, i, newNode);
            this.selectedNode = newNode.ID;
            Repaint();
        }
        GUILayout.EndVertical(); 
    }

    private void OverviewEditing(Rect available)
    {
        Rect centerColumn = new Rect(0, 0, available.width, available.height);
        GUILayout.BeginArea(centerColumn, Style("box"));
        GUILayout.EndArea();
    }

    private bool NextNodeBlock(HamTimelineNode node, int nextNodeID, out HamTimelineNode newNode)
    {
        newNode = null;

        // Modification of Next Node
        GUILayout.BeginHorizontal(Style("box"));
        if (GUILayout.Button("New Dialog Node", Style("FlexButton")))
        {
            HamDialogNode last = node.GetLastDialogNode(this.activeTimeline);
            if (last != null)
            {
                newNode = this.activeTimeline.AddDialogNode(last.SceneID, last.SpeakerID, "New Dialog", last.CharacterIDs);
            }
            else
            {
                newNode = this.activeTimeline.AddDialogNode(this.activeTimeline.DefaultSceneID, this.activeTimeline.NarratorID, "New Dialog");
            }
            return true;
        }
        if (GUILayout.Button("New Decision Node", Style("FlexButton")))
        {
            newNode = this.activeTimeline.AddDecisionNode();
            return true;
        }
        /*
        if (GUILayout.Button("New Branch Node", Style("FlexButton")))
        {

        }
        if (GUILayout.Button("New Consequence Node", Style("FlexButton")))
        {

        }
        */
        GUILayout.EndHorizontal();

        // Preview of Next Node
        GUILayout.BeginVertical(Style("box"));
        GUILayout.Label("Next Node");
        if (nextNodeID != HamTimeline.InvalidID)
        {
            HamTimelineNode nextNode = this.activeTimeline.Nodes[nextNodeID];
            NodePreview(nextNode);
        }
        else
        {
            GUILayout.Label("No Next Node Set");
        }
        GUILayout.EndVertical();

        return false;
    }

    private void PreviousNodesRow(HamTimelineNode node)
    {
        // Preview of Previous Node(s)
        GUILayout.BeginVertical(Style("box"));
        GUILayout.Label("Previous Node(s)");
        if (node.PreviousNodeIDs.Count > 0)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < node.PreviousNodeIDs.Count; ++i)
            {
                NodePreview(this.activeTimeline.Nodes[node.PreviousNodeIDs[i]]);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No Previous Nodes");
        }
        GUILayout.EndVertical();
    }

    private void NodePreview(HamTimelineNode node)
    {
        string previewLabel = String.Format("{0} (No Preview)", node.Type);
        switch (node.Type)
        {
            case TimelineNodeType.Dialog:
            {
                HamDialogNode n = (HamDialogNode)node;
                previewLabel = String.Format(
                    "{0} says \"{1}\"",
                    this.activeTimeline.Characters[n.SpeakerID].Name,
                    n.Dialog
                );
            }
            break;
        case TimelineNodeType.Decision:
            break;
        case TimelineNodeType.Branch:
            break;
        case TimelineNodeType.Consequence:
            break;
        }
        if (GUILayout.Button(previewLabel, Style("FlexButton")))
        {
            this.selectedNode = node.ID;
            Repaint();
        }
    }

    private void CharacterEditing()
    {
        // Grab the active character
        if (this.selectedCharacter == HamTimeline.InvalidID || !this.activeTimeline.Characters.ContainsKey(this.selectedCharacter))
        {
            this.selectedCharacter = this.activeTimeline.NarratorID;
        }
        HamCharacter character = this.activeTimeline.Characters[this.selectedCharacter];

        GUILayout.Label("Character Editing", Style("SubTitle"));

        // Display the character selection / add stripe
        GUILayout.BeginHorizontal(Style("box"));
        GUILayout.BeginVertical(GUILayout.MaxWidth(75));
        GUILayout.Label("Characters");
        GUILayout.EndVertical();
        if (GUILayout.Button(character.Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Characters.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.activeTimeline.Characters[id].Name),
                    (id == this.selectedCharacter),
                    (userData) =>
                    {
                        this.selectedCharacter = (int)userData;
                        Repaint();
                    },
                    id
                );
            }
            menu.ShowAsContext();
        }
        if (GUILayout.Button("+", Style("SmallButton")))
        {
            ModalTextWindow.Popup("New Character Name", (name) =>
            {
                this.selectedCharacter = this.activeTimeline.AddCharacter(name).ID;
            });
        }
        GUILayout.EndHorizontal();

        // Display the selected character details
        GUILayout.BeginHorizontal(Style("box"));
        GUILayout.BeginVertical(GUILayout.MaxWidth(75));
        GUILayout.Label("Name");
        GUILayout.EndVertical();
        character.Name = GUILayout.TextField(character.Name);
        GUILayout.EndHorizontal();
    }

    private void VariableEditing()
    { 
        GUILayout.Label("Variable Editing", Style("SubTitle"));
        GUILayout.BeginHorizontal(Style("box"));
        for (int i = 0; i < (int)VariableType.NumTypes; ++i)
        {
            VariableType varType = (VariableType)i;
            string varLabel = varType.ToString();
            if (GUILayout.Button("Add " + varLabel))
            {
                ModalTextWindow.Popup("New " + varLabel + " Name", (name) =>
                {
                    this.activeTimeline.AddVariable(name, varType);
                });
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical(Style("box"));
        foreach (int id in this.activeTimeline.Variables.Keys)
        {
            GUILayout.BeginHorizontal();
            HamTimelineVariable v = this.activeTimeline.Variables[id];

            GUILayout.BeginVertical(GUILayout.MaxWidth(75));
            v.Name = GUILayout.TextField(v.Name);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.MaxWidth(50));
            if (GUILayout.Button(v.Type.ToString()))
            {
                GenericMenu menu = new GenericMenu();
                for (int j = 0; j < (int)VariableType.NumTypes; ++j)
                {
                    VariableType type = (VariableType)j;
                    menu.AddItem(new GUIContent(type.ToString()), v.Type == type, (t) => { v.SetType((VariableType)t); }, type);
                }
                menu.ShowAsContext();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.MaxWidth(50));
            switch (v.Type)
            {
                case VariableType.Boolean:
                {
                    bool bVal = v.Get<bool>();
                    if (GUILayout.Button(bVal.ToString()))
                    {
                        v.Set(!bVal);
                    }
                    break;
                }
                case VariableType.Integer:
                {
                    int iVal = v.Get<int>();
                    string stringVal = GUILayout.TextField(iVal.ToString());
                    int cVal;
                    if (Int32.TryParse(stringVal, out cVal) && cVal != iVal)
                    {
                        v.Set(cVal);
                    }
                    break; 
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    private void SceneEditing()
    {
        if (this.selectedScene == HamTimeline.InvalidID || !this.activeTimeline.Scenes.ContainsKey(this.selectedScene))
        {
            this.selectedScene = this.activeTimeline.DefaultSceneID;
        }
        HamScene scene = this.activeTimeline.Scenes[this.selectedScene];
        GUILayout.BeginHorizontal(Style("box"));
        GUILayout.BeginVertical(GUILayout.MaxWidth(75));
        GUILayout.Label("Scenes");
        GUILayout.EndVertical();
        if (GUILayout.Button(scene.Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (int id in this.activeTimeline.Scenes.Keys)
            {
                menu.AddItem(
                    new GUIContent(this.activeTimeline.Scenes[id].Name),
                    (id == this.selectedScene),
                    (userData) =>
                    {
                        this.selectedScene = (int)userData;
                        Repaint();
                    },
                    id
                );
            }
            menu.ShowAsContext();
        }
        if (GUILayout.Button("+", Style("SmallButton")))
        {
            ModalTextWindow.Popup("New Scene Name", (name) =>
            {
                this.selectedScene = this.activeTimeline.AddScene(name).ID;
            });
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(Style("box"));
        GUILayout.BeginVertical(GUILayout.MaxWidth(75));
        GUILayout.Label("Name");
        GUILayout.EndVertical();
        scene.Name = GUILayout.TextField(scene.Name);
        GUILayout.EndHorizontal();
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
            ResetEditorWindow();
            Repaint();
        }
    }
    private void LoadTimeline(string name)
    {
        ResetEditorWindow();
        string path = Path.Combine(GetTimelinePath(), name);
        if (!File.Exists(path))
        {
            this.activeTimeline = new HamTimeline();
            this.activeTimeline.Name = name;
            this.activeTimeline.DefaultInit();
            SaveTimeline();
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
        Repaint();
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