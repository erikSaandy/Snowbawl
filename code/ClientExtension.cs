using Sandbox;
using System.Linq;

namespace Saandy
{
	public static partial class ClientExtensions
	{
		public static Snowball GetClientSnowball() => Local.Pawn as Snowball;
		public static Entity GetEntity( this Client self ) => Entity.FindByIndex( self.NetworkIdent );
		public static bool IsHost( this Client self ) => Global.IsListenServer && self.NetworkIdent == 1;

	}
}
