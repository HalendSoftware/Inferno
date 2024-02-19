public sealed class Priority : Component
{
	[Property, Description("A basic component to allow filtering game objects using this based on priority")]
	public int PriorityLevel { get; set; } = 0;
}