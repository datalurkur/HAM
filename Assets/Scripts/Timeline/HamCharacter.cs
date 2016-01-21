public class HamCharacter
{
	public int ID;
	public string Name;	

	public HamCharacter()
	{
		this.ID = HamTimeline.InvalidID;
		this.Name = "Invalid Character";
	}
	
	public HamCharacter(int id, string name)
	{
		this.ID = id;
		this.Name = name;
	}
}