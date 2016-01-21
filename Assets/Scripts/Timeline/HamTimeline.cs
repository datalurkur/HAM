using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

// The timeline contains the story content and logic
// It is comprised of timeline nodes which contain a list of linear events
// Each node type defines how the timeline progresses once it has been executed
public class HamTimeline
{
	public string Name;
	public int OriginNodeID;
	public int NarratorID;

	public SerializeableDictionary<int, HamTimelineVariable> Variables;
	public SerializeableDictionary<int, HamScene> Scenes;
	public SerializeableDictionary<int, HamCharacter> Characters;
	public SerializeableDictionary<int, HamTimelineNode> Nodes;

	public static int InvalidID = -1;
	private int idCount = 0;

	public HamTimeline()
	{
		this.Variables = new SerializeableDictionary<int, HamTimelineVariable>();
		this.Scenes = new SerializeableDictionary<int, HamScene>();
		this.Characters = new SerializeableDictionary<int, HamCharacter>();
		this.Nodes = new SerializeableDictionary<int, HamTimelineNode>();

		// Always create the narrator
		this.NarratorID = AddCharacter("Narrator").ID;

		// Barebones defaults
		int defaultSceneID = AddScene("Default Scene").ID;
		this.OriginNodeID = AddDialogNode(defaultSceneID, this.NarratorID, "Default Intro Narration").ID;
	}

	public HamScene AddScene(string name)
	{
		int id = idCount++;
		HamScene scene = new HamScene(id, name);
		this.Scenes[id] = scene;
		return scene;
	}

	public HamCharacter AddCharacter(string name)
	{
		int id = idCount++;
		HamCharacter character = new HamCharacter(id, name);
		this.Characters[id] = character;
		return character;
	}

	public HamTimelineNode AddDialogNode(int sceneID, int speakerID, string dialog)
	{
		int id = idCount++;
		HamTimelineNode node = new HamDialogNode(id, sceneID, speakerID, dialog);
		this.Nodes[id] = node;
		return node;
	}
}