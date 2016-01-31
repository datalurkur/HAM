public class HamScene : Packable
{
	public void Pack(DataPacker packer)
	{
		packer.Pack(this.ID);
		packer.Pack(this.Name);
	}

	public void Unpack(DataUnpacker unpacker)
	{
		unpacker.Unpack(out this.ID);
		unpacker.Unpack(out this.Name);
	}

	public int ID;
	public string Name;

	public HamScene()
	{
		this.ID = HamTimeline.InvalidID;
		this.Name = "Invalid Scene";
	}

	public HamScene(int id, string name)
	{
		this.ID = id;
		this.Name = name;
	}

}