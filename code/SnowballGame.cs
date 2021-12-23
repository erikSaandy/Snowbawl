using Sandbox;

public partial class SnowballGame : Game
{

	public static SnowballGame Instance { get; set; }

	[Net, Local] public static Vector3 MoveDir { get; set; }
	[Net, Local] public static float SpeedMultiplier { get; set; } = 1;
	[Net, Local] public static Vector3 CameraForward { get; set; }

	[Net, Local] public static bool PlayerDead { get; set; } = false;
	private const float DeathTime = 5;
	[Net, Local] public static float DeathTimer { get; set; }

	public SnowBallHud Hud { get; private set; }

	public SnowballGame()
	{
		Current = this;

		// Singleton
		if(Instance == null)
			Instance = Current as SnowballGame;
		else if(Instance != this){
			this.Delete();
		}

		if ( IsClient )
		{
			// Create the HUD
			_ = new SnowBallHud();
		}
	}

	public override void ClientJoined( Client cl )
	{
		base.ClientJoined( cl );

		SpawnPlayer( cl );

		DeathTimer = DeathTime;

		//(cl.Pawn as Snowball).ResetPosition( Vector3.Zero + Vector3.Up * 20, Angles.Zero );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	public override void Simulate( Client cl )
	{
		if ( PlayerDead )
		{
			Log.Info( cl.Pawn );

			if ( DeathTimer > 0 )
			{
				DeathTimer -= Time.Delta;
			}
			else
			{
				DeathTimer = DeathTime;
				PlayerDead = false;
				SpawnPlayer( cl );

			}
		}

		if ( !cl.Pawn.IsValid() ) return;

		// Block Simulate from running clientside
		// if we're not predictable.
		if ( !cl.Pawn.IsAuthority ) return;

		cl.Pawn.Simulate( cl );

		ICamera c = cl.Pawn.Camera;
		if ( c is FollowCamera )
		{
			(c as FollowCamera).Simulate( cl );
		}

		//if ( cl.Pawn is Snowball )
		//{
		//	if ( Input.Pressed( InputButton.Reload ) )
		//		ResetBall( cl );
		//}
	}

	private void SpawnPlayer(Client cl)
	{
		var player = new Snowball();
		cl.Pawn = player;
		cl.Camera = new FollowCamera();
		(cl.Camera as FollowCamera).Ball = player;

	}

	public override void DoPlayerNoclip( Client player )
	{
		if ( player.Pawn is Player basePlayer )
		{
			if ( basePlayer.DevController is NoclipController )
			{
				Log.Info( "Noclip Mode Off" );
				basePlayer.DevController = null;
			}
			else
			{
				Log.Info( "Noclip Mode On" );
				basePlayer.DevController = new NoclipController();
			}
		}
	}

	public override void BuildInput( InputBuilder input )
	{
		Host.AssertClient();

		Event.Run( "buildinput", input );

		//if ( input.Pressed( InputButton.View ) && Local.Pawn.IsValid() && !(Local.Pawn as Ball).InPlay && !(Local.Pawn as Ball).Cupped && FreeCamTimeLeft > 0.0f )
		//{
		//	if ( FreeCamera == null )
		//		FreeCamera = new FreeCamera();
		//	else
		//		FreeCamera = null;
		//}

		// the camera is the primary method here

		Local.Pawn?.BuildInput( input );

		Local.Client?.Camera?.BuildInput( input );
	}

	public override CameraSetup BuildCamera( CameraSetup camSetup )
	{
		var cam = FindActiveCamera();

		if ( LastCamera != cam )
		{
			LastCamera?.Deactivated();
			LastCamera = cam as Camera;
			LastCamera?.Activated();
		}

		Local.Client?.Camera?.Build( ref camSetup );


		PostCameraSetup( ref camSetup );

		return camSetup;

	}

	/// <summary>
	/// Which camera should we be rendering from?
	/// </summary>
	public override ICamera FindActiveCamera()
	{
		if ( Local.Client.DevCamera != null ) return Local.Client.DevCamera;
		if ( Local.Client.Camera != null ) return Local.Client.Camera;
		if ( Local.Pawn != null ) return Local.Pawn.Camera;

		return null;
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		if ( Local.Pawn != null )
		{
			// VR anchor default is at the pawn's location
			VR.Anchor = Local.Pawn.Transform;

			Local.Pawn.PostCameraSetup( ref camSetup );
		}

		BaseViewModel.UpdateAllPostCamera( ref camSetup );

		CameraModifier.Apply( ref camSetup );
	}

	/// <summary>
	/// Client has disconnected from the server. Remove their entities etc.
	/// </summary>
	public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
	{
		Log.Info( $"\"{cl.Name}\" has left the game ({reason})" );
		Sandbox.UI.ChatBox.AddInformation( To.Everyone, $"{cl.Name} has left ({reason})", $"avatar:{cl.PlayerId}" );

		if ( cl.Pawn.IsValid() )
		{
			cl.Pawn.Delete();
			cl.Pawn = null;
		}

	}

	[ServerCmd]
	public static void SetMoveDir( Vector3 dir )
	{
		MoveDir = dir;
	}

	[ServerCmd]
	public static void SetCameraForward( Vector3 fwd )
	{
		CameraForward = fwd;
	}

	[ServerCmd]
	public static void SetSpeedmultiplier( float val )
	{
		SpeedMultiplier = val;
	}

	[ClientCmd( "debug_write" )]
	public static void Write()
	{
		ConsoleSystem.Run( "quit" );
	}
}
