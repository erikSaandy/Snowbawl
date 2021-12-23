using System;
using Sandbox;

public class DeathCamera : Camera
{
	Angles LookAngles;
	Vector3 MoveInput;

	Vector3 TargetPos;
	Rotation TargetRot;

	float MoveSpeed;
	float LerpMode = 0;

	float velY = 0;
	bool grounded = false;

	public DeathCamera()
	{
		TargetPos = CurrentView.Position;
		TargetRot = CurrentView.Rotation;

		Position = TargetPos;
		Rotation = TargetRot;
		LookAngles = Rotation.Angles();

		velY = 3;
	}

	public override void Update()
	{
		var player = Local.Client;
		if (player == null) return;

		var tr = Trace.Ray(Position, Position + Rotation.Forward * 4096).UseHitboxes().Run();

		// DebugOverlay.Box( tr.EndPos, Vector3.One * -1, Vector3.One, Color.Red );

		Viewer = null;
		{
			var lerpTarget = tr.EndPos.Distance(Position);

			DoFPoint = lerpTarget;// DoFPoint.LerpTo( lerpTarget, Time.Delta * 10 );
		}

		if ( !grounded )
		{
			velY -= Time.Delta * 30;
			Position = new Vector3( Position.x, Position.y, Position.z + velY );

			tr = Trace.Ray( Position, Position + (Vector3.Down * 20)).WorldOnly().Run();
			grounded = tr.Hit;

		}

		FreeRotation();
	}
		
	public override void BuildInput(InputBuilder input)
	{

		MoveInput = input.AnalogMove;

		MoveSpeed = 1;
		if (input.Down(InputButton.Run)) MoveSpeed = 5;
		if (input.Down(InputButton.Duck)) MoveSpeed = 0.2f;

		if (input.Down(InputButton.Slot1)) LerpMode = 0.0f;
		if (input.Down(InputButton.Slot2)) LerpMode = 0.5f;
		if (input.Down(InputButton.Slot3)) LerpMode = 0.9f;

		if (input.Down(InputButton.Use))
			DoFBlurSize = Math.Clamp(DoFBlurSize + (Time.Delta * 3.0f), 0.0f, 100.0f);

		if (input.Down(InputButton.Menu))
			DoFBlurSize = Math.Clamp(DoFBlurSize - (Time.Delta * 3.0f), 0.0f, 100.0f);

		LookAngles += input.AnalogLook;
		LookAngles.roll = 0;

		input.ClearButton(InputButton.Attack1);

		input.StopProcessing = true;
	}

	void FreeRotation()
	{

		TargetRot = Rotation.From(LookAngles);
		Rotation = Rotation.Slerp(Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode));
	}
}
