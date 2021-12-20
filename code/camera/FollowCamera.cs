using Sandbox;
using System;

public partial class FollowCamera : Camera
{
	public Angles Angles { get { return Rotation.Angles(); } set { TargetAngles = value; } }
	public new float FieldOfView => 80.0f;

	Angles TargetAngles;
	[Net, Predicted] public static Rotation TargetRot { get; set; }

	private float Distance;
	private float TargetDistance;

	[Net, Predicted] public float Yaw { get; set; } = 0;

	public float MinDistance => 100.0f;
	public float MaxDistance => 300.0f;
	public float DistanceStep => 10.0f;

	public Snowball Ball;

	public void Simulate(Client cl)
	{
	}


	public FollowCamera()
	{
		Distance = 150;
		TargetDistance = Distance;
	}

	public override void Update()
	{

		Ball = Local.Pawn as Snowball;

		if ( !Ball.IsValid() ) return;

		Position = Ball.Position;
		Vector3 targetPos = Ball.Position + Vector3.Up * (24 + (Ball.CollisionBounds.Center.z * Ball.Scale)) + (Rotation.Left * 52);


		TargetRot = Rotation.From( TargetAngles );

		Rotation = Rotation.Slerp( Rotation, TargetRot, RealTime.Delta * 10.0f );

		TargetDistance = TargetDistance.LerpTo( Distance, RealTime.Delta * 5.0f );
		targetPos += Rotation.Backward * TargetDistance;

		var tr = Trace.Ray( Ball.Position, targetPos )
		.Ignore( Ball )
		.WorldOnly()
		.Radius( 8 )
		.Run();

		Position = tr.EndPos;

	}

	public override void BuildInput(InputBuilder input)
	{
		SnowballGame.SetCameraForward( Rotation.Forward );

		Distance = Math.Clamp(Distance + (-input.MouseWheel * DistanceStep), MinDistance, MaxDistance);

		TargetAngles.yaw += input.AnalogLook.yaw;
		Yaw = TargetAngles.yaw;

		TargetAngles.pitch += input.AnalogLook.pitch;

		TargetAngles = TargetAngles.Normal;

		TargetAngles.pitch = TargetAngles.pitch.Clamp(-45, 90);

	}

}
