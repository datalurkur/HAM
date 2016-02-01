using UnityEngine;
using System.Collections.Generic;

public class HamTimelineInstance
{
	public void Pack(DataPacker packer)
	{
		this.timeline.Pack(packer);
		packer.Pack(this.variables.Count);
		foreach (int key in this.variables.Keys)
		{
			packer.Pack(key);
			this.variables[key].Pack(packer);
		}
		packer.Pack(this.currentNodeID);
		packer.Pack(this.currentSceneID);
		packer.Pack(this.currentCharactersInScene.Count);
		for (int i = 0; i < this.currentCharactersInScene.Count; ++i)
		{
			packer.Pack(this.currentCharactersInScene[i]);
		}
		packer.Pack(this.nodeHistory.Count);
		for (int i = 0; i < this.nodeHistory.Count; ++i)
		{
			packer.Pack(this.nodeHistory[i]);
		}
	}
	public void Unpack(DataUnpacker unpacker)
	{
		this.timeline = new HamTimeline();
		this.timeline.Unpack(unpacker);

		int varSize;
		unpacker.Unpack(out varSize);
		for (int i = 0; i < varSize; ++i)
		{
			int key;
			unpacker.Unpack(out key);
			VariableValue val = new VariableValue();
			val.Unpack(unpacker);
			this.variables[key] = val;
		}
		unpacker.Unpack(out this.currentNodeID);
		unpacker.Unpack(out this.currentSceneID);

		int charSize;
		unpacker.Unpack(out charSize);
		for (int i = 0; i < charSize; ++i)
		{
			int next;
			unpacker.Unpack(out next);
			this.currentCharactersInScene.Add(next);
		}

		int historySize;
		unpacker.Unpack(out historySize);
		for (int i = 0; i < historySize; ++i)
		{
			int history;
			unpacker.Unpack(out history);
			this.nodeHistory.Add(history);
		}
	}

	public delegate void TimelineEvent(HamTimelineEvent eventData);
	public TimelineEvent OnTimelineEvent;

	private HamTimeline timeline;
	private Dictionary<int, VariableValue> variables;
	private int currentNodeID;

	private int currentSceneID;
	private List<int> currentCharactersInScene;

	private List<int> nodeHistory;

	public HamTimelineInstance(string timelinePath, TimelineEvent onTimelineEvent)
	{
		this.timeline = HamTimeline.Load(timelinePath);
		this.currentNodeID = HamTimeline.InvalidID;
		this.variables = new Dictionary<int, VariableValue>();

		this.nodeHistory = new List<int>();
		this.currentNodeID = HamTimeline.InvalidID;
		this.currentCharactersInScene = new List<int>();

		InitializeVariables();

		this.OnTimelineEvent = onTimelineEvent;
	}

	public void Advance(int choice = -1)
	{
		do
		{
			DetermineNextNode(choice);
		} while(!ProcessCurrentNode());
	}

	private void InitializeVariables()
	{
		foreach (HamTimelineVariable variable in this.timeline.Variables.Values)
		{
			this.variables[variable.ID] = new VariableValue((VariableValue)variable);
		}
	}

	private bool EvaluatePredicate(HamPredicate p)
	{
		if (!this.variables.ContainsKey(p.VariableID))
		{
			Debug.LogError("No instance value found for variable");
			return false;
		}
		VariableValue instanceValue = this.variables[p.VariableID];
		return this.timeline.EvaluatePredicate(p, instanceValue);
	}

	private void DetermineNextNode(int choice)
	{
		if (this.currentNodeID == HamTimeline.InvalidID)
		{
			this.currentNodeID = this.timeline.OriginNodeID;
			return;
		}

		HamTimelineNode currentNode = this.timeline.Nodes[this.currentNodeID];
		switch (currentNode.Type)
		{
			case TimelineNodeType.Dialog:
			{
				HamDialogNode d = (HamDialogNode)currentNode;
				this.currentNodeID = d.NextNodeID;
				break;
			}
			case TimelineNodeType.Decision:
			{
				HamDecisionNode d = (HamDecisionNode)currentNode;
				if (choice == -1)
				{
					Debug.LogError("Expected a decision");
					return;
				}
				this.currentNodeID = d.Decisions[choice].NextNodeID;
				break;
			}
			case TimelineNodeType.Branch:
			{
				HamBranchNode b = (HamBranchNode)currentNode;
				bool branched = false;
				for (int i = 0; i < b.Predicates.Count; ++i)
				{
					if (EvaluatePredicate(b.Predicates[i]))
					{
						this.currentNodeID = b.Predicates[i].NextNodeID;
						branched = true;
						break;
					}
				}
				if (!branched)
				{
					this.currentNodeID = b.DefaultNextID;
				}
				break;
			}
			case TimelineNodeType.Consequence:
			{
				HamConsequenceNode c = (HamConsequenceNode)currentNode;
				for (int i = 0; i < c.Operations.Count; ++i)
				{
					if (!this.variables.ContainsKey(c.Operations[i].VariableID))
					{
						Debug.LogError("No instance value found for variable");
						continue;
					}
					VariableValue instanceValue = this.variables[c.Operations[i].VariableID];
					c.Operations[i].Execute(instanceValue);
				}
				this.currentNodeID = c.NextNodeID;
				break;
			}
			default:
			{
				Debug.LogError("Unknown node type");
				break;
			}
		}
	}

	private bool ProcessCurrentNode()
	{
		if (this.currentNodeID == HamTimeline.InvalidID) { return true; }

		this.nodeHistory.Add(this.currentNodeID);
		HamTimelineNode currentNode = this.timeline.Nodes[this.currentNodeID];
		switch (currentNode.Type)
		{
			case TimelineNodeType.Dialog:
			{
				HamDialogNode d = (HamDialogNode)currentNode;
				if (this.currentSceneID != d.SceneID)
				{
					this.OnTimelineEvent(new HamSceneChangesEvent(this.timeline.Scenes[d.SceneID]));
					this.currentSceneID = d.SceneID;
				}
				for (int i = 0; i < this.currentCharactersInScene.Count; ++i)
				{
					if (!d.CharacterIDs.Contains(this.currentCharactersInScene[i]))
					{
						this.OnTimelineEvent(new HamCharacterLeavesEvent(this.timeline.Characters[this.currentCharactersInScene[i]]));
					}
				}
				for (int i = 0; i < d.CharacterIDs.Count; ++i)
				{
					if (!this.currentCharactersInScene.Contains(d.CharacterIDs[i]))
					{
						this.OnTimelineEvent(new HamCharacterEntersEvent(this.timeline.Characters[d.CharacterIDs[i]]));
					}
				}
				this.currentCharactersInScene = d.CharacterIDs;
				this.OnTimelineEvent(new HamDialogEvent(this.timeline.Characters[d.SpeakerID], d.Dialog));
				return true;
			}
			case TimelineNodeType.Decision:
			{
				HamDecisionNode d = (HamDecisionNode)currentNode;
				HamChoiceEvent evt = new HamChoiceEvent();
				for (int i = 0; i < d.Decisions.Count; ++i)
				{
					bool reqsMet = true;
					for (int j = 0; j < d.Decisions[i].Predicates.Count; ++j)
					{
						if (!EvaluatePredicate(d.Decisions[i].Predicates[j]))
						{
							reqsMet = false;
							break;
						}
					}
					if (reqsMet)
					{
						evt.AddChoice(i, d.Decisions[i]);
					}
				}
				this.OnTimelineEvent(evt);
				return true;
			}
			case TimelineNodeType.Branch:
			{
				return false;
			}
			case TimelineNodeType.Consequence:
			{
				return false;
			}
			default:
			{
				Debug.LogError("Unknown node type");
				return true;
			}
		}
	}
}
