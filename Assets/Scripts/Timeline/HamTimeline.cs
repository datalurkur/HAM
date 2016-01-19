using System.Xml;
using System.Xml.Serialization;

// The timeline contains the story content and logic
// It is comprised of timeline nodes which contain a list of linear events
// Each node type defines how the timeline progresses once it has been executed
// The timeline nodes can reference the world state for decision making
[XmlRoot("Timeline")]
public class HamTimeline
{
	[XmlAttribute("Name")]
	public string Name;

	[XmlArray("Variables")]
	[XmlArrayItem("Variable")]
	public HamTimelineVariable[] Variables;

	//[XmlArray("Characters")]
	//[XmlArrayItem("Character")]
	public HamCharacter[] Characters;

	[XmlArray("Nodes")]
	[XmlArrayItem("Node")]
	public HamTimelineNode[] Nodes;
}