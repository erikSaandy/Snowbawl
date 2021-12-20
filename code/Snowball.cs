using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

public partial class Snowball : ModelEntity
{
	public Vector3 LastPosition { get; set; }
	public Angles LastAngles { get; set; }
	public Angles Direction { get; set; }

	static readonly Model Model = Model.Load( "models/snowball/snowball.vmdl" );

	public float Radius => 20.32f * Scale;

	[Net, Predicted] public FollowCamera Cam => Client.Camera as FollowCamera;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( Model );
		SetupPhysicsFromSphere( PhysicsMotionType.Dynamic, Vector3.Zero, 16f );

		CollisionGroup = CollisionGroup.Always;
		EnableTraceAndQueries = false;

		Transmit = TransmitType.Always;

		Predictable = true;

		Tags.Add( "snowball" );

		SmallMovement = new MovementData( 700, 1000, 2000 );
		MediumMovement = new MovementData( 500, 1200, 1000 );
		BigMovement = new MovementData( 300, 1400, 0 );

		//Camera = new FollowCamera( );
		//(Camera as FollowCamera).Ball = this;

		Velocity = Vector3.Zero;
		WaterLevel.Clear();
		LifeState = LifeState.Alive;
		Health = 100;
		Game.Current?.MoveToSpawnpoint( this );
		ResetInterpolation();

	}


	/// <summary>
	/// This entity is probably a pawn, and would like to be placed on a spawnpoint.
	/// If you were making a team based game you'd want to choose the spawn based on team.
	/// Or not even call this. Up to you. Added as a convenience.
	/// </summary>
	public virtual void MoveToSpawnpoint( Entity pawn )
	{
		var spawnpoint = Entity.All
								.OfType<SpawnPoint>()               // get all SpawnPoint entities
								.OrderBy( x => Guid.NewGuid() )     // order them by random
								.FirstOrDefault();                  // take the first one

		if ( spawnpoint == null )
		{
			Log.Warning( $"Couldn't find spawnpoint for {pawn}!" );	
			return;
		}

		pawn.Transform = spawnpoint.Transform;
		pawn.Position += (Vector3.Up * Radius);
		(pawn.Camera as FollowCamera).Rotation = pawn.Transform.Rotation;

	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

	}

	public void ResetPosition( Vector3 position, Angles direction )
	{
		Position = position;
		Velocity = Vector3.Zero;
		ResetInterpolation();

		Direction = direction;

		// Tell the player we reset the ball
		PlayerResetPosition( To.Single( this ), position, direction );
	}


	[ClientRpc]
	protected void PlayerResetPosition( Vector3 position, Angles angles )
	{
		Cam.Angles = new( 14, angles.yaw, 0 );
	}
}
