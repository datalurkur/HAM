using UnityEngine;
using System.Collections;

public enum VariableType
{
	Boolean,
	Integer,
	Enumeration
}

public abstract class HamTimelineVariable
{
	public VariableType Type;
	public string Name;

	public HamTimelineVariable()
	{
		this.Type = VariableType.Boolean;
		this.Name = "Invalid Variable";
	}

	public HamTimelineVariable(VariableType type, string name)
	{
		this.Type = type;
		this.Name = name;
	}
}

public class HamBooleanVariable : HamTimelineVariable
{
	public HamBooleanVariable(string name) : base(VariableType.Boolean, name)
	{

	}
}
