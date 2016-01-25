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
        SingleNodeEditing,
        OverviewEditing,
        CharacterEditing,
        SceneEditing,
        VariableEditing
    }
    // ==================================================================================

    // Consts
    // ==================================================================================
    private const int kTopBarHeight = 100;
    private const int kNodeSizeX = 250;
    private const int kNodeSizeY = 75;
    private const int kNodeSpacingX = 275;
    private const int kNodeSpacingY = 100;
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
    private Vector2 overviewOffset = Vector2.zero;

    private void ResetEditorWindow()
    {
        this.activeTimeline = null;
        this.activeEditingTab = EditingTab.SingleNodeEditing;
        this.selectedNode = HamTimeline.InvalidID;
        this.selectedCharacter = HamTimeline.InvalidID;
        this.selectedScene = HamTimeline.InvalidID;
        this.overviewOffset = Vector2.zero;
    }

    private void SetSelectedNode(HamTimelineNode node)
    {
        this.selectedNode = node.ID;
        Vector2 position;
        if (GetOverviewPosition(node, out position))
        {
            this.overviewOffset = -position;
        }
    }

    private bool GetOverviewPosition(HamTimelineNode node, out Vector2 position)
    {
        if (node.OverviewPosition.HasValue)
        {
            position = new Vector2(
                node.OverviewPosition.Value.x * kNodeSpacingX,
                node.OverviewPosition.Value.y * kNodeSpacingY
            );
            return true;
        }
        else
        {
            position = Vector2.zero;
            return false;
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
        if (this.activeEditingTab == EditingTab.OverviewEditing)
        {
            this.wantsMouseMove = true;
            if (Event.current != null && Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
        }
        else
        {
            this.wantsMouseMove = false;
        }

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
        HamTimelineNode node;
        if (this.selectedNode == HamTimeline.InvalidID)
        {
            node = this.activeTimeline.OriginNode;
            SetSelectedNode(node);
        }
        else
        {
            node = this.activeTimeline.Nodes[this.selectedNode];
        }
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
        GUILayout.Space(4);

        HamTimelineNode newNode;
        if (NextNodeBlock(node, node.NextNodeID, out newNode))
        {
            node.SetNextNode(this.activeTimeline, newNode);
            SetSelectedNode(newNode);
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
            SetSelectedNode(newNode);
            Repaint();
        }
        GUILayout.EndVertical(); 
    }

    private void OverviewEditing(Rect available)
    {
        if (Event.current != null && Event.current.type == EventType.MouseDrag)
        {
            this.overviewOffset += Event.current.delta;
        }

        Vector2 offset = this.overviewOffset + new Vector2(available.width / 2f, available.height / 2f);
        ComputeNodePlacement();

        Rect centerColumn = new Rect(0, 0, available.width, available.height);
        GUILayout.BeginArea(centerColumn, Style("box"));
        List<NodeConnection> nodeConnections = new List<NodeConnection>();
        foreach (HamTimelineNode node in this.activeTimeline.Nodes.Values)
        {
            Vector2 nodePosition;
            if (!GetOverviewPosition(node, out nodePosition))
            {
                Debug.LogError("No overview position found for node");
                continue;
            }

            Rect nodeRect = new Rect(
                nodePosition.x + offset.x - kNodeSizeX / 2f,
                nodePosition.y + offset.y - kNodeSizeY / 2f,
                kNodeSizeX,
                kNodeSizeY
            );

            GUILayout.BeginArea(nodeRect);
            switch (node.Type)
            {
            case TimelineNodeType.Dialog:
                RenderDialogNode(nodePosition, offset, (HamDialogNode)node, nodeConnections);
                break;
            case TimelineNodeType.Decision:
                RenderDecisionNode(nodePosition, offset, (HamDecisionNode)node, nodeConnections);
                break;
            case TimelineNodeType.Branch:
                break;
            case TimelineNodeType.Consequence:
                break;
            }
            GUILayout.EndArea();
        }
        Handles.BeginGUI();
        for (int i = 0; i < nodeConnections.Count; ++i)
        {
            nodeConnections[i].Render();
        }
        Handles.EndGUI();
        GUILayout.EndArea();
    }

    private void RenderDialogNode(Vector2 nodePosition, Vector2 offset, HamDialogNode node, List<NodeConnection> connections)
    {
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
            HamTimelineNode nextNode = this.activeTimeline.Nodes[node.NextNodeID];
            Vector2 nextNodePosition;
            if (GetOverviewPosition(nextNode, out nextNodePosition))
            {
                Vector2 outputPosition = nodePosition + offset + new Vector2(0f, kNodeSizeY / 2f);
                Vector2 inputPosition  = nextNodePosition + offset - new Vector2(0f, kNodeSizeY / 2f);
                Color connectionColor = Color.white;
                if (node.ID == this.selectedNode)
                {
                    connectionColor = Color.green;
                }
                else if (node.NextNodeID == this.selectedNode)
                {
                    connectionColor = Color.red;
                }
                connections.Add(new NodeConnection(outputPosition, inputPosition, connectionColor));
            }
            else
            {
                Debug.LogError("No overview position found for next node");
            }
        }
    }

    private void RenderDecisionNode(Vector2 nodePosition, Vector2 offset, HamDecisionNode node, List<NodeConnection> connections)
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

        // Decision area
        GUILayout.BeginHorizontal();
        for (int i = 0; i < node.Decisions.Count; ++i)
        {
            float decisionX = ulCorner.x + (decisionChunkSize * i);
            HamDecisionNode.Decision d = node.Decisions[i];
            GUILayout.BeginVertical(Style("GenericNode"), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(decisionChunkSize));
            GUILayout.Label(d.IsDialog ? String.Format("\"{0}\"", d.DecisionText) : d.DecisionText);
            GUILayout.EndVertical();

            if (d.NextNodeID != HamTimeline.InvalidID)
            {
                HamTimelineNode nextNode = this.activeTimeline.Nodes[d.NextNodeID];
                Vector2 nextNodePosition;
                if (GetOverviewPosition(nextNode, out nextNodePosition))
                {
                    Vector2 inputPosition  = nextNodePosition + offset - new Vector2(0f, kNodeSizeY / 2f);
                    Vector2 outputPosition = new Vector2(decisionX + (decisionChunkSize / 2f), yMax);
                    Color connectionColor = Color.white;
                    if (node.ID == this.selectedNode)
                    {
                        connectionColor = Color.green;
                    }
                    else if (d.NextNodeID == this.selectedNode)
                    {
                        connectionColor = Color.red;
                    }
                    connections.Add(new NodeConnection(outputPosition, inputPosition, connectionColor));
                }
                else
                {
                    Debug.LogError("No overview position found for next node");
                }
            }
        }
        GUILayout.EndHorizontal();
    }

    private void ComputeNodePlacement()
    {
        // Place the root at the origin
        HamTimelineNode root = this.activeTimeline.OriginNode;
        if (!root.OverviewPosition.HasValue)
        {
            root.OverviewPosition = Vector2.zero;
        }

        List<int> unvisited = this.activeTimeline.Nodes.Keys.ToList();
        List<int> descendants = new List<int>();

        int maxDepth = (int)root.OverviewPosition.Value.y;

        // Start at the root and propagate down
        Queue<HamTimelineNode> traversal = new Queue<HamTimelineNode>();
        traversal.Enqueue(root);
        while (traversal.Count > 0 && unvisited.Count > 0)
        {
            HamTimelineNode n = null;

            if (traversal.Count > 0)
            {
                // Pick the next node off the traversal queue
                n = traversal.Dequeue();
                if (!unvisited.Contains(n.ID)) { continue; }
                unvisited.Remove(n.ID);
                maxDepth = (int)Mathf.Max(maxDepth, n.OverviewPosition.Value.y);
            }
            else
            {
                // The traversal queue is empty but there are still unvisited nodes
                // This means there are islands
                // Find the root of the next island
                for (int i = 0; i < unvisited.Count; ++i)
                {
                    HamTimelineNode island = this.activeTimeline.Nodes[unvisited[i]];
                    if (island.PreviousNodeIDs.Count == 0)
                    {
                        // We found the root of an island - set its initial position and start walking the tree
                        if (!n.OverviewPosition.HasValue)
                        {
                            // TODO - For now, we're placing islands linearly below the main tree
                            // It would be super sweet if islands could be laid next to the main tree neatly,
                            // but that would require code to guarantee that their nodes don't overlap (read: mehhhh complicated)
                            maxDepth += 1;
                            n.OverviewPosition = new Vector2(0, maxDepth);
                        }
                        n = island;
                    }
                }

                if (n == null)
                {
                    Debug.LogError("Unvisited nodes exist with no island roots - the node linkage has been corrupted");
                    return;
                }
            }

            // Collect the descendants
            switch (n.Type)
            {
                case TimelineNodeType.Dialog:
                {
                    HamDialogNode d = (HamDialogNode)n;
                    if (d.NextNodeID != HamTimeline.InvalidID)
                    {
                        descendants.Add(d.NextNodeID);
                    }
                    break;
                }
                case TimelineNodeType.Decision:
                {
                    HamDecisionNode d = (HamDecisionNode)n;
                    for (int i = 0; i < d.Decisions.Count; ++i)
                    {
                        if (d.Decisions[i].NextNodeID != HamTimeline.InvalidID)
                        {
                            descendants.Add(d.Decisions[i].NextNodeID);
                        }
                    }
                    break;
                }
                case TimelineNodeType.Branch:
                {
                    break;
                }
                case TimelineNodeType.Consequence:
                {
                    break;
                }
            }
            
            // Compute the positions of the descendants relative to this node
            float xOffset = (descendants.Count % 2 == 0) ?
                ((descendants.Count - 1) / 2f) :
                (float)(descendants.Count / 2);
            for (int i = 0; i < descendants.Count; ++i)
            {
                HamTimelineNode m = this.activeTimeline.Nodes[descendants[i]];
                if (!m.OverviewPosition.HasValue)
                {
                    m.OverviewPosition = n.OverviewPosition.Value + new Vector2(i - xOffset, 1f);
                }
                traversal.Enqueue(m);
            }
            descendants.Clear();
        }

        if (unvisited.Count > 0)
        {
            Debug.LogWarning("Timeline contains islands");
        }
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
            NodeButtonPreview(nextNode, null);
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
                int descendantIndex = node.GetDescendantIndex(this.activeTimeline, i);
                NodeButtonPreview(this.activeTimeline.Nodes[node.PreviousNodeIDs[i]], descendantIndex);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No Previous Nodes");
        }
        GUILayout.EndVertical();
    }

    private void NodeButtonPreview(HamTimelineNode node, int? previousIndex)
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
                break;
            }
            case TimelineNodeType.Decision:
            {
                HamDecisionNode n = (HamDecisionNode)node;
                if (previousIndex.HasValue)
                {
                    HamDecisionNode.Decision d = n.Decisions[previousIndex.Value];
                    if (d.IsDialog)
                    {
                        previewLabel = String.Format("Decision: \"{0}\"", d.DecisionText);
                    }
                    else
                    {
                        previewLabel = String.Format("Decision: {0}", d.DecisionText);
                    }
                }
                else
                {
                    previewLabel = String.Format("Decision ({0} Choices)", n.Decisions.Count);
                }
                break;
            }
            case TimelineNodeType.Branch:
                break;
            case TimelineNodeType.Consequence:
                break;
        }
        if (GUILayout.Button(previewLabel, Style("FlexButton")))
        {
            SetSelectedNode(node);
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
        foreach (HamTimelineVariable v in this.activeTimeline.Variables.Values)
        {
            GUILayout.BeginHorizontal();

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