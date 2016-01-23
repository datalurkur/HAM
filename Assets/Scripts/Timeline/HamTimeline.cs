using UnityEngine;
using System.IO;
using System.Collections.Generic;

// The timeline contains the story content and logic
// It is comprised of timeline nodes which contain a list of linear events
// Each node type defines how the timeline progresses once it has been executed
public class HamTimeline : Packable
{
	public static void Save(HamTimeline timeline, string path)
	{
		DataPacker packer = new BinaryDataPacker();
		timeline.Pack(packer);
		File.WriteAllBytes(path, packer.GetBytes());
	}
	public static HamTimeline Load(string path)
	{
		byte[] data = File.ReadAllBytes(path);
		DataUnpacker unpacker = new BinaryDataUnpacker(data);
		HamTimeline timeline = new HamTimeline();
		timeline.Unpack(unpacker);
		return timeline;
	}

	public void Pack(DataPacker packer)
	{
		packer.Pack(this.IDCount);
		packer.Pack(this.Name);
		packer.Pack(this.OriginNodeID);
		packer.Pack(this.NarratorID);
		packer.Pack(this.DefaultSceneID);

		packer.Pack(this.Variables.Count);
		for (int i = 0; i < this.Variables.Count; ++i)
		{
			this.Variables[i].Pack(packer);
		}

		packer.Pack(this.Scenes.Count);
		foreach (HamScene scene in this.Scenes.Values)
		{
			scene.Pack(packer);
		}

		packer.Pack(this.Characters.Count);
		foreach (HamCharacter character in this.Characters.Values)
		{
			character.Pack(packer);
		}

		packer.Pack(this.Nodes.Count);
		foreach (HamTimelineNode node in this.Nodes.Values)
		{
			HamTimelineNode.Pack(node, packer);
		}
	}
	public void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.IDCount);
		unpacker.Unpack(out this.Name);
		unpacker.Unpack(out this.OriginNodeID);
		unpacker.Unpack(out this.NarratorID);
		unpacker.Unpack(out this.DefaultSceneID);

		int numVars;
		unpacker.Unpack(out numVars);
		for (int i = 0; i < numVars; ++i)
		{
			HamTimelineVariable variable = new HamTimelineVariable();
			variable.Unpack(unpacker);
			this.Variables[variable.ID] = variable;
		}

		int numScenes;
		unpacker.Unpack(out numScenes);
		for (int i = 0; i < numScenes; ++i)
		{
			HamScene scene = new HamScene();
			scene.Unpack(unpacker);
			this.Scenes[scene.ID] = scene;
		}

		int numCharacters;
		unpacker.Unpack(out numCharacters);
		for (int i = 0; i < numCharacters; ++i)
		{
			HamCharacter character = new HamCharacter();
			character.Unpack(unpacker);
			this.Characters[character.ID] = character;
		}

		int numNodes;
		unpacker.Unpack(out numNodes);
		for (int i = 0; i < numNodes; ++i)
		{
			HamTimelineNode node;
			HamTimelineNode.Unpack(out node, unpacker);
			this.Nodes[node.ID] = node;
		}
	}

	public static int InvalidID = -1;

	public int IDCount;
	
	public string Name;
	public int OriginNodeID;
	public int NarratorID;
	public int DefaultSceneID;

	public Dictionary<int, HamTimelineVariable> Variables;
	public Dictionary<int, HamScene> Scenes;
	public Dictionary<int, HamCharacter> Characters;
	public Dictionary<int, HamTimelineNode> Nodes;

	public HamTimeline()
	{
		this.Variables = new Dictionary<int, HamTimelineVariable>();
		this.Scenes = new Dictionary<int, HamScene>();
		this.Characters = new Dictionary<int, HamCharacter>();
		this.Nodes = new Dictionary<int, HamTimelineNode>();
	}

	public void DefaultInit()
	{
		this.IDCount = 0;

		// Always create the narrator
		this.NarratorID = AddCharacter("Narrator").ID;

		// Barebones defaults
		this.DefaultSceneID = AddScene("Default Scene").ID;
		this.OriginNodeID = AddDialogNode(this.DefaultSceneID, this.NarratorID, "Default Intro Narration").ID;	
	}

	public HamScene AddScene(string name)
	{
		int id = this.IDCount++;
		HamScene scene = new HamScene(id, name);
		this.Scenes[id] = scene;
		return scene;
	}

	public HamCharacter AddCharacter(string name)
	{
		int id = this.IDCount++;
		HamCharacter character = new HamCharacter(id, name);
		this.Characters[id] = character;
		return character;
	}

	public HamTimelineNode AddDialogNode(int sceneID, int speakerID, string dialog, List<int> characters = null)
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamDialogNode(id, sceneID, speakerID, dialog, characters);
		this.Nodes[id] = node;
		return node;
	}

	public HamTimelineNode AddBranchNode()
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamBranchNode(id);
		this.Nodes[id] = node;
		return node;
	}

	public HamTimelineNode AddDecisionNode()
	{
		int id = this.IDCount++;
		HamTimelineNode node = new HamDecisionNode(id);
		this.Nodes[id] = node;
		return node;
	}

	public void RemoveNode(int nodeID)
	{
		if (nodeID == this.OriginNodeID)
		{
			// TODO - Pop up a box that tells the user why this didn't work
			return;
		}	

		// TODO - Write this function durr
	}
}