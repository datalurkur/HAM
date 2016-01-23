using UnityEngine;
using System.Collections;

public enum VariableType
{
	Boolean,
	Integer,
	Enumeration
}

public enum VariableComparison
{
	Equal,
	NotEqual,
	LessThan,
	GreaterThan,
	LessThanEqual,
	GreaterThanEqual
}

public class HamTimelineVariable
{
	public void Pack(DataPacker packer)
	{
		// TODO - Pack variable
	}

	public void Unpack(DataUnpacker unpack)
	{
		// TODO - Unpack variable
	}

	public int ID;
	public VariableType Type;
	public string Name;

	public HamTimelineVariable()
	{
		this.ID = HamTimeline.InvalidID;
		this.Type = VariableType.Boolean;
		this.Name = "Invalid Variable";
	}

	public HamTimelineVariable(int id, VariableType type, string name)
	{
		this.ID = id;
		this.Type = type;
		this.Name = name;
	}
}