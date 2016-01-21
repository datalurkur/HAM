public class HamScene
{
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