using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
	{
	/// <summary>
	/// Tag component indicating that district layout has been completed
	/// Used to gate downstream systems like WFC that depend on district placement
	/// </summary>
	public struct DistrictLayoutDoneTag : IComponentData
		{
		/// <summary>
		/// Number of districts that were successfully laid out
		/// </summary>
		public int DistrictCount;

		/// <summary>
		/// Number of connections generated between districts 
		/// </summary>
		public int ConnectionCount;

		public DistrictLayoutDoneTag(int districtCount = 0, int connectionCount = 0)
			{
			DistrictCount = districtCount;
			ConnectionCount = connectionCount;
			}
		}

	/// <summary>
	/// Tag component for entities that need district layout processing
	/// Entities with this tag and Coordinates == (0,0) will be processed by DistrictLayoutSystem
	/// </summary>
	public struct DistrictLayoutPendingTag : IComponentData { }
	}
