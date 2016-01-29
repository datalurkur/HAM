using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

class HamTimelineEditor : EditorWindow
{
    // Window Setup
    // ==================================================================================
    [MenuItem ("HAM/Timeline Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(HamTimelineEditor));
    }
    // ==================================================================================

    // Enumerations
    // ==================================================================================
    private enum EditingTab
    {
        NodeEditing,
        CharacterEditing,
        SceneEditing,
        VariableEditing
    }
    public enum SelectionMode
    {
        SelectNode,
        LinkNodes
    }
    // ==================================================================================

    // Consts
    // ==================================================================================
    private const int kTopBarHeight = 95;
    private const int kStatusBarHeight = 25;
    private const int kSideBarWidth = 350;
    private const int kNodeSizeX = 250;
    private const int kNodeSizeY = 80;
    private const int kNodeSpacingX = 270;
    private const int kNodeSpacingY = 120;
    private const int kTangentStrength = 25;
    // ==================================================================================

    // Classes and Structs
    // ==================================================================================
    private class NodeConnection
    {
        private Vector2 StartPoint;
        private Vector2 EndPoint;
        private Color Color;
        private float Width;

        private Vector2 StartTangent;
        private Vector2 EndTangent;

        public NodeConnection(Vector2 start, Vector2 end, Color color)
        {
            this.StartPoint = start;
            this.EndPoint = end;
            this.Color = color;
            this.Width = 2.5f;
            if (Mathf.Abs(start.x - end.x) < 1f)
            {
                this.StartTangent = start;
                this.EndTangent   = end;
            }
            else
            {
                this.StartTangent = start + new Vector2(0, kTangentStrength);
                this.EndTangent   = end   - new Vector2(0, kTangentStrength);
            }
        }

        public NodeConnection(Vector2 start, Vector2 end) : this(start, end, Color.white) { }

        public void Render()
        {
            Handles.DrawBezier(this.StartPoint, this.EndPoint, this.StartTangent, this.EndTangent, this.Color, null, this.Width); 
        }
    }

    private class SelectionContext
    {
        public HamTimelineNode Node;
        public SelectionMode Mode;
        private int DescendantIndex;

        public SelectionContext()
        {
            this.Node = null;
            this.Mode = SelectionMode.SelectNode;
            this.DescendantIndex = 0;
        }

        public bool NodeSelected(int id)
        {
            return (this.Node != null && this.Node.ID == id);
        }

        public void ClickedNode(HamTimeline timeline, HamTimelineNode node)
        {
            switch (this.Mode)
            {
            case SelectionMode.SelectNode:
                this.Node = node;
                break;
            case SelectionMode.LinkNodes:
                if (this.Node != node)
                {
                    timeline.LinkNodes(this.Node, node, this.DescendantIndex);
                }
                this.Mode = SelectionMode.SelectNode;
                break;
            }
        }

        public void BeginLinking(HamTimelineNode node, int index = 0)
        {
            this.Node = node;
            this.DescendantIndex = index;
            this.Mode = SelectionMode.LinkNodes;
        }
    }
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

        HandleTimelineEvents();
    }

    private HamTimeline activeTimeline;
    private EditingTab activeEditingTab = EditingTab.NodeEditing;
    private SelectionContext selection = new SelectionContext();
    private int selectedCharacter = HamTimeline.InvalidID;
    private int selectedScene = HamTimeline.InvalidID;
    private Vector2 overviewOffset = Vector2.zero;
    private Dictionary<int,Vector2> overviewNodePlacement;
    private List<NodeConnection> connections = new List<NodeConnection>();

    private void ResetEditorWindow()
    {
        this.activeTimeline = null;
        this.activeEditingTab = EditingTab.NodeEditing;
        this.selectedCharacter = HamTimeline.InvalidID;
        this.selectedScene = HamTimeline.InvalidID;
        this.overviewOffset = Vector2.zero;
    }

    private Vector2 GetOverviewPosition(HamTimelineNode node)
    {
        if (this.overviewNodePlacement == null || !this.overviewNodePlacement.ContainsKey(node.ID)) { return Vector2.zero; }
        Vector2 placement = this.overviewNodePlacement[node.ID];
        return new Vector2(
            placement.x * kNodeSpacingX,
            placement.y * kNodeSpacingY
        );
    }

    private void HandleTimelineEvents()
    {
        if (Event.current == null) { return; }
        switch (Event.current.type)
        {
        case EventType.MouseDrag:
            this.overviewOffset += Event.current.delta;
            Repaint();
            break;
        case EventType.MouseMove:
            Repaint();
            break;
        }
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
        this.wantsMouseMove = true;

        Rect topbar = new Rect(kSideBarWidth, 0, position.width - kSideBarWidth, kTopBarHeight);
        Rect statusbar = new Rect(kSideBarWidth, position.height - kStatusBarHeight, position.width - kSideBarWidth, kStatusBarHeight);
        Rect sidebar = new Rect(0, 0, kSideBarWidth, position.height);
        Rect overview = new Rect(kSideBarWidth, kTopBarHeight, position.width - kSideBarWidth, position.height - kTopBarHeight - kStatusBarHeight);

        GUILayout.BeginArea(sidebar, Style("Box"));
        switch (this.activeEditingTab)
        {
        case EditingTab.NodeEditing:
            NodeEditing();
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

        GUILayout.BeginArea(overview);
        RenderOverview(overview);
        GUILayout.EndArea();

        GUILayout.BeginArea(statusbar, Style("box"));
        string statusLabel = "";
        switch (this.selection.Mode)
        {
        case SelectionMode.SelectNode:
            statusLabel = "Node Selection";
            break;
        case SelectionMode.LinkNodes:
            statusLabel = "Select a Node To Link";
            break;
        }
        GUILayout.Label(statusLabel);
        GUILayout.EndArea();

        // Render the controls for saving and loading last, so that doing a save or load in the middle of a render doesn't throw exceptions
        GUILayout.BeginArea(topbar, Style("Box"));
        RenderTopBar();
        GUILayout.EndArea();
    }
    
    private void RenderTopBar()
    {
        GUILayout.Label("Editing " + this.activeTimeline.Name, Style("Title"));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save")) { SaveTimeline(); }
        if (GUILayout.Button("Save and Close")) { SaveTimeline(true); }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = (this.activeEditingTab != EditingTab.NodeEditing);
        if (GUILayout.Button("Node")) { this.activeEditingTab = EditingTab.NodeEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.CharacterEditing);
        if (GUILayout.Button("Character")) { this.activeEditingTab = EditingTab.CharacterEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.VariableEditing);
        if (GUILayout.Button("Variable")) { this.activeEditingTab = EditingTab.VariableEditing; }
        GUI.enabled = (this.activeEditingTab != EditingTab.SceneEditing);
        if (GUILayout.Button("Scene")) { this.activeEditingTab = EditingTab.SceneEditing; }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void NodeEditing()
    {
        if (this.selection.Node == null)
        {
            GUILayout.Label("No Node Selected", Style("SubTitle"));
            return;
        }

        switch (this.selection.Node.Type)
        {
        case TimelineNodeType.Dialog:
            DialogNodeEditing(this.selection.Node as HamDialogNode);
            break;
        case TimelineNodeType.Branch:
            BranchNodeEditing(this.selection.Node as HamBranchNode);
            break;
        case TimelineNodeType.Decision:
            DecisionNodeEditing(this.selection.Node as HamDecisionNode);
            break;
        case TimelineNodeType.Consequence:
            ConsequenceNodeEditing(this.selection.Node as HamConsequenceNode);
            break;
        }
    }

    private void DialogNodeEditing(HamDialogNode node)
    {
        GUILayout.Label("Dialog Node", Style("SubTitle"));

        // Node Editing
        GUILayout.BeginVertical(Style("box"));
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
        GUILayout.Label("Scene");
        if (GUILayout.Button(this.activeTimeline.Scenes[node.SceneID].Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (HamScene scene in this.activeTimeline.Scenes.Values)
            {
                menu.AddItem(
                    new GUIContent(scene.Name),
                    (scene.ID == node.SceneID),
                    (userData) => { node.SceneID = (int)userData; },
                    scene.ID 
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
            foreach (HamCharacter character in this.activeTimeline.Characters.Values)
            {
                menu.AddItem(
                    new GUIContent(character.Name),
                    (character.ID == node.SpeakerID),
                    (userData) => { node.SpeakerID = (int)userData; },
                    character.ID 
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
            foreach (HamCharacter character in this.activeTimeline.Characters.Values)
            {
                if (!node.CharacterIDs.Contains(character.ID))
                {
                    menu.AddItem(
                        new GUIContent(character.Name),
                        false,
                        (userData) => { node.CharacterIDs.Add((int)userData); },
                        character.ID 
                    );
                }
            }
            menu.ShowAsContext();
        }
        GUILayout.EndVertical();
    }

    private void BranchNodeEditing(HamBranchNode node)
    {
        GUILayout.Label("Branch Node", Style("SubTitle"));

        GUILayout.BeginVertical();
        for (int i = 0; i < node.Predicates.Count; ++i)
        {
            PredicateEditing(node, i);
        }
        GUILayout.BeginHorizontal(Style("box"));
        if (GUILayout.Button("Add Predicate", Style("FlexButton")))
        {
            node.AddPredicate();
            Repaint();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void PredicateEditing(HamBranchNode node, int index)
    {
        HamPredicate p = node.Predicates[index];
        GUILayout.BeginVertical(Style("box"));

        // Variable Selection / Creation
        if (GUILayout.Button((p.VariableID == HamTimeline.InvalidID) ? "---" : this.activeTimeline.Variables[p.VariableID].Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (HamTimelineVariable v in this.activeTimeline.Variables.Values)
            {
                menu.AddItem(
                    new GUIContent(v.Name),
                    (v.ID == p.VariableID),
                    (userData) =>
                    {
                        p.SetVariable(this.activeTimeline, (int)userData);
                    },
                    v.ID 
                );
            }
            for (int i = 0; i < (int)VariableType.NumTypes; ++i)
            {
                VariableType varType = (VariableType)i;
                string varLabel = varType.ToString();
                menu.AddItem(
                    new GUIContent("Add New " + varLabel + " Variable"),
                    false,
                    () =>
                    {
                        ModalTextWindow.Popup("New " + varLabel + " Name", (name) =>
                        {
                            HamTimelineVariable newVar = this.activeTimeline.AddVariable(name, varType);
                            p.SetVariable(this.activeTimeline, newVar.ID);
                        });
                    }
                );
            }
            menu.ShowAsContext(); 
        }

        // Comparison Selection
        if (GUILayout.Button(p.Comparison.ToString()))
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < (int)VariableComparison.NumComparisons; ++i)
            {
                int thisIndex = i;
                menu.AddItem(
                    new GUIContent(((VariableComparison)i).ToString()),
                    (VariableComparison)i == p.Comparison,
                    (userData) =>
                    {
                        p.Comparison = (VariableComparison)userData;
                    },
                    thisIndex
                );
            }
            menu.ShowAsContext(); 
        }

        // Value Setting
        if (p.VariableID != HamTimeline.InvalidID)
        {
            VariableValueField(p.CompareValue);
        }

        GUILayout.EndVertical(); 
    }

    private void DecisionNodeEditing(HamDecisionNode node)
    {
        GUILayout.Label("Decision Node", Style("SubTitle"));

        GUILayout.BeginVertical();
        for (int i = 0; i < node.Decisions.Count; ++i)
        {
            DecisionEditing(node, i);
        }
        GUILayout.BeginHorizontal(Style("box"));
        if (GUILayout.Button("Add Decision", Style("FlexButton")))
        {
            node.AddDecision("New Decision", false);
            Repaint();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DecisionEditing(HamDecisionNode node, int i)
    {
        HamDecisionNode.Decision d = node.Decisions[i];
        GUILayout.BeginVertical();

        GUILayout.BeginVertical(Style("box"));
        GUILayout.Label("Decision Text");
        d.DecisionText = GUILayout.TextField(d.DecisionText);

        d.IsDialog = GUILayout.Toggle(d.IsDialog, "Is Dialog");
        GUILayout.EndVertical();

        GUILayout.EndVertical(); 
    }

    private void ConsequenceNodeEditing(HamConsequenceNode node)
    {
        GUILayout.Label("Consequence Node", Style("SubTitle"));

        GUILayout.BeginVertical();
        for (int i = 0; i < node.Operations.Count; ++i)
        {
            OperationEditing(node, i);
        }
        GUILayout.BeginHorizontal(Style("box"));
        if (GUILayout.Button("Add Operation", Style("FlexButton")))
        {
            node.AddOperation();
            Repaint();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void OperationEditing(HamConsequenceNode node, int index)
    {
        HamOperation oper = node.Operations[index];
        GUILayout.BeginVertical(Style("box"));

        // Variable Selection / Creation
        if (GUILayout.Button((oper.VariableID == HamTimeline.InvalidID) ? "---" : this.activeTimeline.Variables[oper.VariableID].Name))
        {
            GenericMenu menu = new GenericMenu();
            foreach (HamTimelineVariable v in this.activeTimeline.Variables.Values)
            {
                menu.AddItem(
                    new GUIContent(v.Name),
                    (v.ID == oper.VariableID),
                    (userData) =>
                    {
                        oper.SetVariable(this.activeTimeline, (int)userData);
                    },
                    v.ID 
                );
            }
            for (int i = 0; i < (int)VariableType.NumTypes; ++i)
            {
                VariableType varType = (VariableType)i;
                string varLabel = varType.ToString();
                menu.AddItem(
                    new GUIContent("Add New " + varLabel + " Variable"),
                    false,
                    () =>
                    {
                        ModalTextWindow.Popup("New " + varLabel + " Name", (name) =>
                        {
                            HamTimelineVariable newVar = this.activeTimeline.AddVariable(name, varType);
                            oper.SetVariable(this.activeTimeline, newVar.ID);
                        });
                    }
                );
            }
            menu.ShowAsContext(); 
        }

        // Operation Selection
        if (GUILayout.Button(oper.Operator.ToString()))
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < (int)VariableOperation.NumOperations; ++i)
            {
                int thisIndex = i;
                menu.AddItem(
                    new GUIContent(((VariableOperation)i).ToString()),
                    (VariableOperation)i == oper.Operator,
                    (userData) =>
                    {
                        oper.Operator = (VariableOperation)userData;
                    },
                    thisIndex
                );
            }
            menu.ShowAsContext(); 
        }

        // Value Setting
        if (oper.VariableID != HamTimeline.InvalidID)
        {
            VariableValueField(oper.Operand);
        }

        GUILayout.EndVertical();  
    }

    private void VariableValueField(VariableValue varValue)
    {
        switch (varValue.Type)
        {
            case VariableType.Boolean:
            {
                bool bVal = varValue.Get<bool>();
                if (GUILayout.Button(bVal.ToString()))
                {
                    varValue.Set(!bVal);
                }
                break;
            }
            case VariableType.Integer:
            {
                int iVal = varValue.Get<int>();
                string stringVal = GUILayout.TextField(iVal.ToString());
                int cVal;
                if (Int32.TryParse(stringVal, out cVal) && cVal != iVal)
                {
                    varValue.Set(cVal);
                }
                break; 
            }
        } 
    }

    private void RenderOverview(Rect available)
    {
        if (this.activeTimeline.NodeLinkageDirty)
        {
            HamNodePlacer placer = new HamNodePlacer(this.activeTimeline);
            placer.GetNodePlacement(out this.overviewNodePlacement);
            this.activeTimeline.NodeLinkageDirty = false;
        }

        Vector2 offset = this.overviewOffset + new Vector2(available.width / 2f, available.height / 2f);
        Rect centerColumn = new Rect(0, 0, available.width, available.height);
        GUILayout.BeginArea(centerColumn, Style("box"));

        Handles.BeginGUI();
        for (int i = 0; i < this.connections.Count; ++i)
        {
            this.connections[i].Render();
        }
        Handles.EndGUI();
        GUILayout.EndArea();
        this.connections.Clear();

        foreach (HamTimelineNode node in this.activeTimeline.Nodes.Values)
        {
            Vector2 nodePosition = GetOverviewPosition(node);
            Rect nodeRect = new Rect(
                nodePosition.x + offset.x - kNodeSizeX / 2f,
                nodePosition.y + offset.y - kNodeSizeY / 2f,
                kNodeSizeX,
                kNodeSizeY
            );
#if LINKAGE_DEBUG
            Debug.Log("Rendering " + node.ID + " at " + nodePosition);
#endif

            GUILayout.BeginArea(nodeRect);
            switch (node.Type)
            {
            case TimelineNodeType.Dialog:
                RenderDialogNode(nodePosition, offset, (HamDialogNode)node);
                break;
            case TimelineNodeType.Decision:
                RenderDecisionNode(nodePosition, offset, (HamDecisionNode)node);
                break;
            case TimelineNodeType.Branch:
                RenderBranchNode(nodePosition, offset, (HamBranchNode)node);
                break;
            case TimelineNodeType.Consequence:
                RenderConsequenceNode(nodePosition, offset, (HamConsequenceNode)node);
                break;
            }
            GUILayout.EndArea();

        }
    }

    private void RightClickNodeContext(HamTimelineNode node, int descendantIndex)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(
            new GUIContent("Link To Selection"),
            false,
            (a) => { this.selection.BeginLinking(node, descendantIndex); },
            null
        );
        menu.AddItem(
            new GUIContent("Link To New Dialog"),
            false,
            (a) =>
            {
                HamDialogNode lastDialog = node.GetLastDialogNode(this.activeTimeline);
                HamTimelineNode newNode = this.activeTimeline.AddDialogNode(
                    lastDialog != null ? lastDialog.SceneID : this.activeTimeline.DefaultSceneID,
                    lastDialog != null ? lastDialog.SpeakerID : this.activeTimeline.NarratorID,
                    "",
                    lastDialog != null ? lastDialog.CharacterIDs : null
                );
                this.activeTimeline.LinkNodes(node, newNode, descendantIndex);
            },
            null
        );
        menu.AddItem(
            new GUIContent("Link To New Decision"),
            false,
            (a) =>
            {
                HamTimelineNode newNode = this.activeTimeline.AddDecisionNode();
                this.activeTimeline.LinkNodes(node, newNode, descendantIndex);
            },
            null
        );
        menu.AddItem(
            new GUIContent("Link To New Branch"),
            false,
            (a) =>
            {
                HamTimelineNode newNode = this.activeTimeline.AddBranchNode();
                this.activeTimeline.LinkNodes(node, newNode, descendantIndex);
            },
            null
        );
        menu.AddItem(
            new GUIContent("Link To New Consequence"),
            false,
            (a) =>
            {
                HamTimelineNode newNode = this.activeTimeline.AddConsequenceNode();
                this.activeTimeline.LinkNodes(node, newNode, descendantIndex);
            },
            null
        );
        menu.ShowAsContext();
    }

    private void RenderDialogNode(Vector2 nodePosition, Vector2 offset, HamDialogNode node)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(Style("DialogNode"));
        GUILayout.Label("Dialog");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(Style("GenericNode"));
        GUILayout.Label(this.activeTimeline.Scenes[node.SceneID].Name);
        GUILayout.EndVertical();
        GUILayout.BeginVertical(Style("GenericNode"));
        GUILayout.Label(this.activeTimeline.Characters[node.SpeakerID].Name);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(Style("GenericNode"), GUILayout.ExpandHeight(true));
        GUILayout.Label(node.Dialog);
        GUILayout.EndHorizontal();

        if (node.NextNodeID != HamTimeline.InvalidID)
        {
#if LINKAGE_DEBUG
            Debug.Log("Node " + node.ID + " transitions to " + node.NextNodeID);
#endif
            HamTimelineNode nextNode = this.activeTimeline.Nodes[node.NextNodeID];
            Vector2 nextNodePosition = GetOverviewPosition(nextNode);
            Vector2 outputPosition = nodePosition + offset + new Vector2(0f, kNodeSizeY / 2f);
            Vector2 inputPosition  = nextNodePosition + offset - new Vector2(0f, kNodeSizeY / 2f);
            Color connectionColor = Color.white;
            if (this.selection.NodeSelected(node.ID))
            {
                connectionColor = Color.green;
            }
            else if (this.selection.NodeSelected(node.NextNodeID))
            {
                connectionColor = Color.red;
            }
            this.connections.Add(new NodeConnection(outputPosition, inputPosition, connectionColor));
        }

        GUILayout.EndVertical();
        if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, Style("InvisibleButton")))
        {
            if (Event.current.button == 0)
            {
                this.selection.ClickedNode(this.activeTimeline, node);
            }
            else if (Event.current.button == 1)
            {
                RightClickNodeContext(node, 0);
            }
        }
    }

    private void RenderDecisionNode(Vector2 nodePosition, Vector2 offset, HamDecisionNode node)
    {
        // Determine dimensions of various components
        float decisionChunkSize = kNodeSizeX / node.Decisions.Count;
        Vector2 ulCorner = new Vector2(
            nodePosition.x + offset.x - kNodeSizeX / 2f,
            nodePosition.y + offset.y - kNodeSizeY / 2f
        );
        float yMax = ulCorner.y + kNodeSizeY;

        // Node title
        GUILayout.BeginHorizontal(Style("DecisionNode"));
        GUILayout.Label("Decision");
        GUILayout.EndHorizontal();
        if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, Style("InvisibleButton")))
        {
            if (Event.current.button == 0)
            {
                this.selection.ClickedNode(this.activeTimeline, node);
            }
        }

        // Decision area
        GUILayout.BeginHorizontal();
        for (int i = 0; i < node.Decisions.Count; ++i)
        {
            float decisionX = ulCorner.x + (decisionChunkSize * i);
            HamDecisionNode.Decision d = node.Decisions[i];
            GUILayout.BeginVertical(Style("GenericNode"), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(decisionChunkSize));
            GUILayout.Label(d.IsDialog ? String.Format("\"{0}\"", d.DecisionText) : d.DecisionText);
            GUILayout.EndVertical();

            if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, Style("InvisibleButton")))
            {
                if (Event.current.button == 0)
                {
                    this.selection.ClickedNode(this.activeTimeline, node);
                }
                else if (Event.current.button == 1)
                {
                    RightClickNodeContext(node, i);
                }
            }

            if (d.NextNodeID != HamTimeline.InvalidID)
            {
#if LINKAGE_DEBUG
                Debug.Log("Node " + node.ID + " transitions to " + d.NextNodeID);
#endif
                HamTimelineNode nextNode = this.activeTimeline.Nodes[d.NextNodeID];
                Vector2 nextNodePosition = GetOverviewPosition(nextNode);
                Vector2 inputPosition  = nextNodePosition + offset - new Vector2(0f, kNodeSizeY / 2f);
                Vector2 outputPosition = new Vector2(decisionX + (decisionChunkSize / 2f), yMax);
                Color connectionColor = Color.white;
                if (this.selection.NodeSelected(node.ID))
                {
                    connectionColor = Color.green;
                }
                else if (this.selection.NodeSelected(d.NextNodeID))
                {
                    connectionColor = Color.red;
                }
                this.connections.Add(new NodeConnection(outputPosition, inputPosition, connectionColor));
            }
        }
        GUILayout.EndHorizontal();
    }

    private void RenderBranchNode(Vector2 nodePosition, Vector2 offset, HamBranchNode node)
    {
        // Determine dimensions of various components
        float branchChunkSize = kNodeSizeX / (node.Predicates.Count + 1);
        Vector2 ulCorner = new Vector2(
            nodePosition.x + offset.x - kNodeSizeX / 2f,
            nodePosition.y + offset.y - kNodeSizeY / 2f
        );
        float yMax = ulCorner.y + kNodeSizeY;

        // Node title
        GUILayout.BeginHorizontal(Style("BranchNode"));
        GUILayout.Label("Branch");
        GUILayout.EndHorizontal();
        if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, Style("InvisibleButton")))
        {
            if (Event.current.button == 0)
            {
                this.selection.ClickedNode(this.activeTimeline, node);
            }
        }

        // Branch area
        GUILayout.BeginHorizontal();
        for (int i = 0; i <= node.Predicates.Count; ++i)
        {
            float branchX = ulCorner.x + (branchChunkSize * i);

            GUILayout.BeginVertical(Style("GenericNode"), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(branchChunkSize));
            if (i == node.Predicates.Count)
            {
                GUILayout.Label("Default");
            }
            else
            {
                GUILayout.Label(node.Predicates[i].Label(this.activeTimeline));
            }
            GUILayout.EndVertical();

            if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, Style("InvisibleButton")))
            {
                if (Event.current.button == 0)
                {
                    this.selection.ClickedNode(this.activeTimeline, node);
                }
                else if (Event.current.button == 1)
                {
                    RightClickNodeContext(node, i);
                }
            }

            int nextNodeID = (i == node.Predicates.Count) ? node.DefaultNextID : node.Predicates[i].NextNodeID;

            if (nextNodeID != HamTimeline.InvalidID)
            {
 #if LINKAGE_DEBUG
                Debug.Log("Node " + node.ID + " transitions to " + nextNodeID);
#endif
                HamTimelineNode nextNode = this.activeTimeline.Nodes[nextNodeID];
                Vector2 nextNodePosition = GetOverviewPosition(nextNode);
                Vector2 inputPosition  = nextNodePosition + offset - new Vector2(0f, kNodeSizeY / 2f);
                Vector2 outputPosition = new Vector2(branchX + (branchChunkSize / 2f), yMax);
                Color connectionColor = Color.white;
                if (this.selection.NodeSelected(node.ID))
                {
                    connectionColor = Color.green;
                }
                else if (this.selection.NodeSelected(nextNodeID))
                {
                    connectionColor = Color.red;
                }
                this.connections.Add(new NodeConnection(outputPosition, inputPosition, connectionColor));
            }
        }
        GUILayout.EndHorizontal();
    }

    private void RenderConsequenceNode(Vector2 nodePosition, Vector2 offset, HamConsequenceNode node)
    {
        // Node title
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(Style("ConsequenceNode"));
        GUILayout.Label("Consequence");
        GUILayout.EndHorizontal();


        // Operations area
        GUILayout.BeginHorizontal(Style("GenericNode"), GUILayout.ExpandHeight(true));
        for (int i = 0; i < node.Operations.Count; ++i)
        {
            GUILayout.Label(node.Operations[i].Label(this.activeTimeline));
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        if (node.NextNodeID != HamTimeline.InvalidID)
        {
#if LINKAGE_DEBUG
            Debug.Log("Node " + node.ID + " transitions to " + node.NextNodeID);
#endif
            HamTimelineNode nextNode = this.activeTimeline.Nodes[node.NextNodeID];
            Vector2 nextNodePosition = GetOverviewPosition(nextNode);
            Vector2 outputPosition = nodePosition + offset + new Vector2(0f, kNodeSizeY / 2f);
            Vector2 inputPosition  = nextNodePosition + offset - new Vector2(0f, kNodeSizeY / 2f);
            Color connectionColor = Color.white;
            if (this.selection.NodeSelected(node.ID))
            {
                connectionColor = Color.green;
            }
            else if (this.selection.NodeSelected(node.NextNodeID))
            {
                connectionColor = Color.red;
            }
            this.connections.Add(new NodeConnection(outputPosition, inputPosition, connectionColor));
        }
        
        if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, Style("InvisibleButton")))
        {
            if (Event.current.button == 0)
            {
                this.selection.ClickedNode(this.activeTimeline, node);
            }            
            else if (Event.current.button == 1)
            {
                RightClickNodeContext(node, 0);
            }
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
            foreach (HamCharacter sC in this.activeTimeline.Characters.Values)
            {
                menu.AddItem(
                    new GUIContent(sC.Name),
                    (sC.ID == this.selectedCharacter),
                    (userData) =>
                    {
                        this.selectedCharacter = (int)userData;
                        Repaint();
                    },
                    sC.ID 
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
        GUILayout.BeginVertical(Style("box"));
        for (int i = 0; i < (int)VariableType.NumTypes; ++i)
        {
            VariableType varType = (VariableType)i;
            string varLabel = varType.ToString();
            if (GUILayout.Button("Add " + varLabel, Style("FlexButton")))
            {
                ModalTextWindow.Popup("New " + varLabel + " Name", (name) =>
                {
                    this.activeTimeline.AddVariable(name, varType);
                });
            }
        }
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        foreach (HamTimelineVariable v in this.activeTimeline.Variables.Values)
        {
            GUILayout.BeginVertical(Style("box"));

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
            VariableValueField(v);
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();
    }

    private void SceneEditing()
    {
        GUILayout.Label("Scene Editing", Style("SubTitle"));
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
            foreach (HamScene sS in this.activeTimeline.Scenes.Values)
            {
                menu.AddItem(
                    new GUIContent(sS.Name),
                    (sS.ID == this.selectedScene),
                    (userData) =>
                    {
                        this.selectedScene = (int)userData;
                        Repaint();
                    },
                    sS.ID 
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
                Debug.LogError(e.StackTrace);
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