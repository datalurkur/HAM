using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

// The timeline contains the story content and logic
// It is comprised of timeline nodes which contain a list of linear events
// Each node type defines how the timeline progresses once it has been executed
public class HamTimeline
{
	public string Name;

	public List<HamTimelineVariable> Variables;
	public List<HamCharacter> Characters;
	public List<HamTimelineNode> Nodes;

	public HamTimeline()
	{
		this.Variables = new List<HamTimelineVariable>();
		this.Characters = new List<HamCharacter>();
		this.Nodes = new List<HamTimelineNode>();

		// Always create the narrator
		HamCharacter narrator = new HamCharacter("Narrator");
		this.Characters.Add(narrator);
	}
}