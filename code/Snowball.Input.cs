using Sandbox;
using System;

public partial class Snowball : ModelEntity
{
	/// <summary>
	/// The current shot power...
	/// </summary>
	public const float MovementSpeed = 700f;

	[Net, Predicted] public int TargetScale { get; set; } = 1;

	[Net, Local] public float CurrentDistance { get; set; } = 0;

	public bool CurrentGrounded = false;
	public bool LastGrounded = false;

	[Net, Predicted] public float Charge { get; set; } = 1;
	[Net, Local] public bool ChargeCooldown { get; set; } = false;

	private MovementData SmallMovement { get; set; }
	private MovementData MediumMovement { get; set; }
	private MovementData BigMovement { get; set; }
	[Net, Local] public MovementData CurrentMovement { get; set; }

	[Net, Predicted] public bool Charging { get; set; } = false;

	private void UpdateGrounded()
	{
		LastGrounded = CurrentGrounded;

		if(Velocity.y > 0.1f)
		{
			CurrentGrounded = false;
			return;
		}

		TraceResult t = Trace.Ray( Position, Position + (Vector3.Down * Radius ) + Velocity.y )
			.Ignore( this )
			.WorldOnly()
			.Run();

		CurrentGrounded = t.Hit;

	}

	private void UpdateCurrentMovement()
	{

		switch( TargetScale )
		{
			case 1:
				CurrentMovement = SmallMovement;
				break;
			case 2:
				CurrentMovement = MediumMovement;
				break;
			case 3:
				CurrentMovement = BigMovement;
				break;
		}
	}

	public override void Simulate( Client cl )
	{

		base.Simulate( cl );

		using(Prediction.Off())
		{

			Update();

			Scale = Saandy.Math2d.Lerp( Scale, TargetScale, Time.Delta * 10 );

		}

		if ( IsServer )
		{

			//Scale += Time.Delta * Time.Delta;

			//MovePawn( SnowballGame.MoveDir, MovementSpeed );
		}

	}

	public void Update()
	{

		UpdateCurrentMovement();
		UpdateGrounded();
		PhysicsBody.Mass = CurrentMovement.mass;

		// SCALING
		if ( TargetScale < 3 )
		{
			if ( LastGrounded && CurrentGrounded )
			{
				CurrentDistance += (LastPosition - Position).Length;
			}

			if ( CurrentDistance >= CurrentMovement.distanceToGrow )
			{
				TargetScale += 1;
				CurrentDistance -= CurrentMovement.distanceToGrow;
			}
		}

		LastPosition = Position;

		// SLOW DOWN
		if ( Input.Down( InputButton.Duck ) )
		{
			SnowballGame.SetSpeedmultiplier( 0.9f );
		}
		else
		{
			SnowballGame.SetSpeedmultiplier( 0.995f );
		}
		//

		// CHARGE
		if(!ChargeCooldown && Input.Down(InputButton.Attack2))
		{
			Charge -= (Time.Delta);
			Charge = Math.Clamp( Charge, 0f, 1f );
			PhysicsBody.GravityScale = 0.3f;
		}
		else if( !ChargeCooldown && Input.Released( InputButton.Attack2 ) ) // On Charge
		{
			OnCharge();
		}

		// MOVEMENT
		else
		{
			PhysicsBody.GravityScale = 1f;

			if (Charge >= 1)
			{
				Charging = false;
				ChargeCooldown = false;
				Charge = 1;

			} else
			{
				Charge += (Time.Delta * 0.3f);
			}

			Vector2 moveDir = SnowballGame.MoveDir;
			MovePawn( moveDir, CurrentMovement.speed );
		}

		if ( Charging )
		{
			

			if(Velocity.Length < 50)
			{
				Charging = false;
			}	
		}

		//

		if ( Input.Pressed( InputButton.Flashlight ) )
		{
			OnKilled();
			OnKilledMessage( Client.PlayerId, Client.Name, 0, "", "Killed Themselves." );
		}

	}

	public override void OnKilled()
	{
		base.OnKilled();
		Client.Camera = new DeathCamera();
		SnowballGame.PlayerDead = true;
		
	}

	/// <summary>
	/// Called clientside from OnKilled on the server to add kill messages to the killfeed. 
	/// </summary>
	[ClientRpc]
	public virtual void OnKilledMessage( long leftid, string left, long rightid, string right, string method )
	{
		Sandbox.UI.KillFeed.Current?.AddEntry( leftid, left, rightid, right, method );
	}

	private void OnCharge()
	{
		Velocity = SnowballGame.CameraForward * 1000 * (1 - Charge);
		Charging = true;
		ChargeCooldown = true;

		if ( TargetScale > 1 )
		{
			TargetScale--;
		}

		CurrentDistance = 0;
	}

	public override void BuildInput( InputBuilder input )
	{
		Host.AssertClient();
		
		SnowballGame.SetMoveDir(GetMoveDir());

		//CameraYaw = SnowballGame.Instance.FollowCamera.Angles.yaw;
	}

	public Vector2 GetMoveDir()
	{
		Vector2 dir = Vector2.Zero;
		if ( Input.Down(InputButton.Forward) )
		{
			dir.y++;
		}
		if ( Input.Down( InputButton.Back ) )
		{
			dir.y--;
		}
		if ( Input.Down( InputButton.Right ) )
		{
			dir.x++;
		}
		if ( Input.Down( InputButton.Left ) )
		{
			dir.x--;
		}

		dir = dir.Normal;

		if ( Cam != null && dir.Length > 0.01f )
		{
			//DebugOverlay.Line( Position, Position + (Vector3)(dir * 300), Color.Blue );
			dir = Saandy.Math2d.RotateByAngle( dir, -Cam.Angles.yaw + 90 );
			DebugOverlay.Line( Position, Position + (Vector3)(dir * 300), Color.Red );

		}


		return dir;

		//vel.x = dir.x * movementSpeed * Time.deltaTime;
		//vel.z = dir.y * movementSpeed * Time.deltaTime;

		//if ( !cc.isGrounded )
		//{
		//	vel.y -= gravity * mass * Time.fixedDeltaTime;
		//}

		//cc.Move( vel );
	}
		
	public void MovePawn( Vector3 direction, float speed )
	{

		//var sound = "minigolf.swing" + Rand.Int( 1, 3 );
		//Sound.FromWorld( sound, Position ).SetVolume( 1.0f + speed ).SetPitch( Rand.Float( 0.8f, 1.2f ) );

		Direction = direction.EulerAngles;

		Velocity += direction * speed * Time.Delta;
		Velocity *= SnowballGame.SpeedMultiplier;
		AngularVelocity *= SnowballGame.SpeedMultiplier * 0f;

	}

	public struct MovementData
	{
		public float speed;
		public float mass;
		public float distanceToGrow;

		public MovementData( float speed, float mass, float distanceToGrow )
		{			
			this.speed = speed;
			this.mass = mass;
			this.distanceToGrow = distanceToGrow;
		}
	}

}
