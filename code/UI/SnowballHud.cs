using Sandbox;
using Sandbox.UI;

[Library]
public partial class SnowBallHud : HudEntity<RootPanel>
{
	public Panel CrosshairPanel { get; protected set; }


	public SnowBallHud()
	{
		if ( !IsClient )
			return;

		RootPanel.StyleSheet.Load( "/ui/SnowballHud.scss" );

		RootPanel.AddChild<NameTags>();

		RootPanel.AddChild<ChatBox>();
		RootPanel.AddChild<VoiceList>();
		RootPanel.AddChild<KillFeed>();
		RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
		RootPanel.AddChild<SnowMeter>();
		RootPanel.AddChild<ChargeMeter>();

		RootPanel.AddChild<CrosshairCanvas>();
		CrosshairPanel = new SnowballCrosshair();
		CrosshairCanvas.SetCrosshair( CrosshairPanel );

		//RootPanel.AddChild<CurrentTool>();
		//RootPanel.AddChild<SpawnMenu>();
	}
}
