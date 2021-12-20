using Sandbox;
using Sandbox.UI;

[UseTemplate]
public partial class ChargeMeter : Panel
{
	Panel Bar { get; set; }

	[Net, Local] public Color ColorLow { get; } = new Color( 250 / 255f, 79 / 255f, 79 / 255f );
	[Net, Local] public Color ColorHigh { get; } = new Color( 111 / 255f, 235 / 255f, 39 / 255f );
	[Net, Local] public Color ColorCooldown { get; } = new Color( 70 / 255f, 143 / 255f, 226 / 255f );

	public Color ColorCurrent = new Color();


	public override void Tick() 
	{
		if ( Local.Pawn is not Snowball ball ) return;

		float p = (1 - ball.Charge);
		Bar.Style.Width = Length.Percent( p * 100 );
		Bar.Style.Dirty();
		Bar.SetClass( "is-visible", p > 0.0f );

		Bar.Style.BackgroundColor = ball.ChargeCooldown ? ColorCooldown : Saandy.Math2d.Lerp( ColorLow, ColorHigh, p );

		//if ( ball.LastShotPower > 0.0f )
		//{
		//	LastPower.Style.Left = Length.Percent( ball.LastShotPower * 100 );
		//	LastPower.Style.Opacity = 1;
		//	LastPower.Style.Dirty();
		//}
	}
}
