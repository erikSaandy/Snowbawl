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

	private static MovementData SmallMovement { get; set; }
	private static MovementData MediumMovement { get; set; }
	private static MovementData BigMovement { get; set; }
	[Net, Local] public MovementData CurrentMovement { get; set; }

	[Net, Predicted] public bool Charging { get; set; } = false;

	public bool HasAirJump = false;
	public const float DoubleJumpForce = 300;

	public Particles DeathFunnelParticles = null;

	private void UpdateGrounded()
	{
		LastGrounded = CurrentGrounded;

		float dst = Radius + 4;

		TraceResult t = Trace.Ray( Position, Position + (Vector3.Down * dst) )
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

		// AIR JUMP //
		if(CurrentGrounded)
		{
			HasAirJump = true;
		}

		if(HasAirJump)
		{
			if(Input.Pressed(InputButton.Jump))
			{
				float mag = Velocity.Length - DoubleJumpForce;
				
				Velocity = (Direction.Direction * mag) + (Vector3.Up * DoubleJumpForce) ;
				HasAirJump = false;
			}
		}
		// //

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
		if ( Input.Down( InputButton.Duck ) && CurrentGrounded )
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
			OnBoost();
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

		// DEATH FUNNEL PARTICLE //
		if(Velocity.Length > CurrentMovement.killSpeed)
		{
			if ( DeathFunnelParticles == null )
			{
				DeathFunnelParticles = Particles.Create( "particles/deathfunnel.vpcf", this, "origin", true );
			}

			DeathFunnelParticles.SetForward( 1, -Velocity.Normal );
			DeathFunnelParticles.SetPosition( 2, Vector3.One * Scale );

			OnAttack();

		}
		else
		{

			if(DeathFunnelParticles != null)
			{
				DeathFunnelParticles.Destroy( true );
				DeathFunnelParticles = null;
			}

		}

		//

		if ( Input.Pressed( InputButton.Flashlight ) )
		{
			OnKilled();
			OnKilledMessage( Client.PlayerId, Client.Name, 0, "", "Killed Themselves." );
		}

	}
	
	public void OnAttack()
	{
		Vector3 offset = (Position - LastPosition).Normal * 4;
		TraceResult[] t = Trace.Ray( LastPosition + offset, Position + offset )
			.Ignore( this )
			.EntitiesOnly()
			.Radius( Radius )
			.RunAll();

		if ( t == null ) return;

		foreach (TraceResult tr in t)
		{
			DebugOverlay.TraceResult( tr, 0.01f );
			if (tr.Entity is not Snowball) { return; }
			Snowball other = tr.Entity as Snowball;
			Log.Info( other.Client.Name  );
			other.OnKilled(Client, this);

		}
	}

	public override void OnKilled()
	{
		base.OnKilled();
		Client.Camera = new DeathCamera();
		SnowballGame.PlayerDead = true;
		
	}

	/// <summary>
	/// An entity, which is a pawn, and has a client, has been killed.
	/// </summary>
	public virtual void OnKilled( Client other, Entity otherPawn )
	{
		Host.AssertServer();

		Log.Info( $"{Client.Name} was killed" );

		OnKilledMessage( other.PlayerId, other.Name, Client.PlayerId, Client.Name, "Smashed" );

		OnKilled();
	}

	/// <summary>
	/// Called clientside from OnKilled on the server to add kill messages to the killfeed. 
	/// </summary>
	[ClientRpc]
	public virtual void OnKilledMessage( long leftid, string left, long rightid, string right, string method )
	{
		Sandbox.UI.KillFeed.Current?.AddEntry( leftid, left, rightid, right, method );
	}

	private void OnBoost()
	{
		float mag = Velocity.Length;

		Velocity = SnowballGame.CameraForward * 1000 * (1 - Charge) + (SnowballGame.CameraForward * mag);
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

}
