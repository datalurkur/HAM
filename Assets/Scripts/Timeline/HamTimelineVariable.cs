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
	GreaterThanEqual,
	NumComparisons
}

public enum VariableOperation
{
	Set = 0,
	Modify
}

public class VariableValue
{
	public virtual void Pack(DataPacker packer)
	{
		packer.Pack((byte)this.Type);
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

	public virtual void Unpack(DataUnpacker unpacker)
	{
		byte type;
		unpacker.Unpack(out type);
		this.Type = (VariableType)type;
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

	public VariableType Type;
	private object variableValue;

	public VariableValue() {}
	public VariableValue(VariableType type)
	{
		SetType(type);
	}
	public VariableValue(VariableValue other)
	{
		SetType(other.Type, other);
	}

	public void SetType(VariableType type, VariableValue defaultValue = null)
	{
		this.Type = type;

		switch (this.Type)
		{
		case VariableType.Boolean:
			this.variableValue = (defaultValue == null) ? new bool() : defaultValue.Get<bool>();
			break;
		case VariableType.Integer:
			this.variableValue = (defaultValue == null) ? new int() : defaultValue.Get<int>();
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

	public string Label()
	{
		switch (this.Type)
		{
		case VariableType.Boolean:
			return Get<bool>().ToString();
		case VariableType.Integer:
			return Get<int>().ToString();
		default:
			return "Unknown";
		}
	}

	public bool Compare(VariableComparison comparison, VariableValue other)
	{
		if (this.Type != other.Type)
		{
			Debug.LogError("Can't compare different data types");
			return false;
		}

		switch (this.Type)
		{
			case VariableType.Boolean:
			{
				switch (comparison)
				{
				case VariableComparison.Equal:
					return Get<bool>() == other.Get<bool>();
				case VariableComparison.NotEqual:
					return Get<bool>() != other.Get<bool>();
				}
				break;	
			}
			case VariableType.Integer:
			{
				switch (comparison)
				{
				case VariableComparison.Equal:
					return Get<int>() == other.Get<int>();
				case VariableComparison.NotEqual:
					return Get<int>() != other.Get<int>();
				case VariableComparison.LessThan:
					return Get<int>() <  other.Get<int>();
				case VariableComparison.GreaterThan:
					return Get<int>() >  other.Get<int>();
				case VariableComparison.LessThanEqual:
					return Get<int>() <= other.Get<int>();
				case VariableComparison.GreaterThanEqual:
					return Get<int>() >= other.Get<int>();
				}
				break;			
			}
		}

		Debug.LogError("Failed to compare variables");
		return false;
	}
}

public class HamTimelineVariable : VariableValue
{
	public override void Pack(DataPacker packer)
	{
		packer.Pack(this.ID);
		base.Pack(packer);
		packer.Pack(this.Name);
	}

	public override void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.ID);
		base.Unpack(unpacker);
		unpacker.Unpack(out this.Name);
	}

	public int ID;
	public string Name;

	public HamTimelineVariable() { }
	public HamTimelineVariable(int id, VariableType type, string name) : base(type)
	{
		this.ID = id;
		this.Name = name;
	}
}