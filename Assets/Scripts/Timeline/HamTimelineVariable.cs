using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

public enum VariableType
{
	Boolean = 0,
	Integer,
	NumTypes
}

public enum VariableComparison
{
	Equal = 0,
	NotEqual,
	LessThan,
	GreaterThan,
	LessThanEqual,
	GreaterThanEqual
}

public enum VariableOperation
{
	Set = 0,
	Modify
}

public class HamTimelineVariable
{
	public void Pack(DataPacker packer)
	{
		packer.Pack(this.ID);
		packer.Pack((byte)this.Type);
		packer.Pack(this.Name);
		switch (this.Type)
		{
		case VariableType.Boolean:
			packer.Pack((bool)this.variableValue);
			break;
		case VariableType.Integer:
			packer.Pack((int)this.variableValue);
			break;
		}
	}

	public void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.ID);
		byte type;
		unpacker.Unpack(out type);
		this.Type = (VariableType)type;
		unpacker.Unpack(out this.Name);
		switch (this.Type)
		{
			case VariableType.Boolean:
			{
				bool v;
				unpacker.Unpack(out v);
				this.variableValue = v;
				break;
			}
			case VariableType.Integer:
			{
				int i;
				unpacker.Unpack(out i);
				this.variableValue = i;
				break;
			}
		}
	}

	public int ID;
	public VariableType Type;
	public string Name;

	private object variableValue;

	public HamTimelineVariable() { }
	public HamTimelineVariable(int id, VariableType type, string name)
	{
		this.ID = id;
		this.Name = name;

		SetType(type);
	}

	public void SetType(VariableType type)
	{
		this.Type = type;

		switch (this.Type)
		{
		case VariableType.Boolean:
			this.variableValue = new bool();
			break;
		case VariableType.Integer:
			this.variableValue = new int();
			break;
		}
	}

	public void Set<T>(T val)
	{
		if (val.GetType() != this.variableValue.GetType())
		{
			throw new ArgumentException();
		}
		this.variableValue = val;
	}

	public T Get<T>()
	{
		if (typeof(T) != this.variableValue.GetType())
		{
			throw new ArgumentException();
		}
		return (T)this.variableValue;
	}
}