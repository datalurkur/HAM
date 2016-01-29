using UnityEngine;
using System;
using System.Collections;

public class HamOperation
{
	public void Pack(DataPacker packer)
	{
		packer.Pack(this.VariableID);
		packer.Pack((byte)this.Operator);
		if (this.VariableID != HamTimeline.InvalidID)
		{
			this.Operand.Pack(packer);
		}
	}

	public void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.VariableID);
		byte oper;
		unpacker.Unpack(out oper);
		this.Operator = (VariableOperation)oper;
		if (this.VariableID != HamTimeline.InvalidID)
		{
			this.Operand = new VariableValue();
			this.Operand.Unpack(unpacker);
		}
	}

	public int VariableID;
	public VariableOperation Operator;
	public VariableValue Operand;

	public HamOperation()
	{
		this.VariableID = HamTimeline.InvalidID;
		this.Operator = VariableOperation.Set;
		this.Operand = null;
	}

	public void SetVariable(HamTimeline timeline, int id)
	{
		if (id == HamTimeline.InvalidID)
		{
			this.Operand = null;
		}
		else
		{
			this.Operand = new VariableValue(timeline.Variables[id]);
		}
		this.VariableID = id;
	}	

	public string Label(HamTimeline timeline)
	{
		if (this.VariableID == HamTimeline.InvalidID)
		{
			return "No Operation";
		}
		HamTimelineVariable variable = timeline.Variables[this.VariableID];
		return String.Format("{0} {1} {2}", this.Operator.ToString(), variable.Name, this.Operand.Label());
	}

	public void Execute()
	{
		// TODO - Implement me
		throw new ArgumentException();
	}
}
