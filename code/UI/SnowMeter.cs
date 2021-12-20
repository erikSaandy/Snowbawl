using Sandbox;
using Sandbox.UI;

[UseTemplate]
public partial class SnowMeter : Panel
{
	Panel Bar { get; set; }
	Label Value { get; set; }
	Label Scale { get; set; }

	private float FontSize = 32;

	public override void Tick() 
	{
		if ( Local.Pawn is not Snowball ball ) return;
			
		float p = (ball.CurrentDistance / ball.CurrentMovement.distanceToGrow) * 100;
		Bar.Style.Width = Length.Percent( p );
		Bar.Style.Dirty();
		Bar.SetClass( "is-visible", p > 0.0f );
		
		Value.Text = ball.TargetScale == 3 ? "" : p.ToString( "#0" ) + "%";
		Value.Style.FontSize =32 * Saandy.ClientExtensions.GetClientSnowball().Scale;

		Scale.Text = " x" + (Saandy.ClientExtensions.GetClientSnowball().TargetScale);

		int targetSize = ball.TargetScale == 3 ? 96 : 32;
		FontSize = Saandy.Math2d.Lerp( FontSize, targetSize, Time.Delta * 10 );
		Scale.Style.FontSize = FontSize;

		//if ( ball.LastShotPower > 0.0f )
		//{
		//	LastPower.Style.Left = Length.Percent( ball.LastShotPower * 100 );
		//	LastPower.Style.Opacity = 1;
		//	LastPower.Style.Dirty();
		//}
	}
}
