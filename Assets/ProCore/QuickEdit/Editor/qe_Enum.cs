
namespace QuickEdit
{

	/**
	 * Where this model came from:
	 *	- Imported from FBX or other
	 *	- Saved to .asset from Unity
	 *	- Procedurally built in scene.
	 */
	public enum ModelSource
	{
		Imported,
		Asset,
		Scene
	}

	public enum ElementMode
	{
		Vertex,
		Edge,
		Face
	}
}