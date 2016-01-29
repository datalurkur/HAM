using UnityEngine;
using System;
using System.Collections;

public class HamPredicate
{
	public void Pack(DataPacker packer)
	{
		packer.Pack(this.VariableID);
		packer.Pack((byte)this.Comparison);
		if (this.VariableID != HamTimeline.InvalidID)
		{
			this.CompareValue.Pack(packer);
		}
		packer.Pack(this.NextNodeID);
	}

	public void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.VariableID);
		byte comparisonByte;
		unpacker.Unpack(out comparisonByte);
		this.Comparison = (VariableComparison)comparisonByte;
		if (this.VariableID == HamTimeline.InvalidID)
		{
			this.CompareValue = null;
		}
		else
		{
			this.CompareValue = new VariableValue();
			this.CompareValue.Unpack(unpacker);
		}
		unpacker.Unpack(out this.NextNodeID);
	}

	public int VariableID;
	public VariableComparison Comparison;
	public VariableValue CompareValue;
	public int NextNodeID;

	public HamPredicate()
	{
		this.VariableID = HamTimeline.InvalidID;	
		this.NextNodeID = HamTimeline.InvalidID;
	}

	public void SetVariable(HamTimeline timeline, int id)
	{
		if (id == HamTimeline.InvalidID)
		{
			this.CompareValue = null;
		}
		else
		{
			this.CompareValue = new VariableValue(timeline.Variables[id]);
		}
		this.VariableID = id;
	}

	public string Label(HamTimeline timeline)
	{
		if (this.VariableID == HamTimeline.InvalidID)
		{
			return "No Comparison";
		}
		HamTimelineVariable variable = timeline.Variables[this.VariableID];
		return String.Format("{0} {1} {2}", variable.Name, this.Comparison.ToString(), this.CompareValue.Label());
	}

	public bool Evaluate(HamTimeline timeline)
	{
		if (this.VariableID == HamTimeline.InvalidID)
		{
			return false;
		}
		HamTimelineVariable variable = timeline.Variables[this.VariableID];
		return variable.Compare(this.Comparison, this.CompareValue);
	}
}