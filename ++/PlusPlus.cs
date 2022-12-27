using Godot;
using Godot.Collections;
using System;

public class PlusPlus : Node
{
	private bool loaded = false;
	private Control options;
	private Control modlist;
	public override void _Ready()
	{
		options = GetNode<Control>("Panel/VBoxContainer");
		modlist = options.GetNode<Control>("ModList/Content");

		options.GetNode<LineEdit>("Path/PathText").Connect("text_changed", this, nameof(CheckPathText));
		options.GetNode<Button>("Path/PathSelect").Connect("pressed", options.GetNode<FileDialog>("Path/Dialog"), "popup", new Godot.Collections.Array(new Rect2(0, 0, 480, 360)));
		options.GetNode<FileDialog>("Path/Dialog").Connect("dir_selected", this, nameof(NewPath));

		options.GetNode<Button>("Load").Connect("pressed", this, nameof(LoadGame));

		CallDeferred(nameof(LoadPrevious));
	}
	public void LoadPrevious()
	{
		var file = new File();
		if (!file.FileExists(OS.GetExecutablePath().GetBaseDir().PlusFile("settings"))) return;
		file.Open(OS.GetExecutablePath().GetBaseDir().PlusFile("settings"), File.ModeFlags.Read);
		NewPath(file.GetLine());
		file.Close();
	}
	public void SavePrevious()
	{
		var file = new File();
		file.Open(OS.GetExecutablePath().GetBaseDir().PlusFile("settings"), File.ModeFlags.Write);
		file.StoreLine(options.GetNode<LineEdit>("Path/PathText").Text);
		file.Close();
	}
	public void Disable()
	{
		options.GetNode<LineEdit>("Path/PathText").Editable = false;
		options.GetNode<Button>("Path/PathSelect").Disabled = true;
		options.GetNode<Button>("Load").Disabled = true;
		options.GetNode<Button>("SkipLoading").Disabled = true;
		options.GetNode<Button>("NativeDialog").Disabled = true;
		options.GetNode<Button>("Discord").Disabled = true;
	}
	public void NewPath(string path)
	{
		options.GetNode<LineEdit>("Path/PathText").Text = path;
		CheckPathText(path);
	}
	public void CheckPathText(string path)
	{
		var badPath = !CheckPath(path);
		options.GetNode<Label>("Valid").Visible = badPath;
		options.GetNode<Button>("Load").Disabled = badPath;
	}
	public bool CheckPath(string path)
	{
		if (!System.IO.Directory.Exists(path))
		{
			loaded = false;
			return false;
		}
		if (!System.IO.File.Exists(path.PlusFile("SoundSpacePlus.pck")))
		{
			loaded = false;
			return false;
		}
		return true;
	}
	public void NodeInit(string nodePath)
	{
		var node = GetNode(nodePath);
		node.Notification(NotificationEnterTree);
		node.Notification(NotificationPostEnterTree);
		node.Notification(NotificationReady);
	}
	public void LoadGame()
	{
		if (loaded) return;
		loaded = true;

		string path = options.GetNode<LineEdit>("Path/PathText").Text;
		if (!CheckPath(path))
		{
			loaded = false;
			return;
		}

		Disable();

		SavePrevious();

		ProjectSettings.SetSetting("application/config/discord_rpc", !options.GetNode<CheckBox>("Discord").Pressed);
		ProjectSettings.SetSetting("application/config/disable_native_file_dialogs", options.GetNode<CheckBox>("NativeDialog").Pressed);

		var version = ProjectSettings.GetSetting("application/config/version");
		ProjectSettings.SetSetting("application/config/version", $"{version} (Modded)");
		var res = ProjectSettings.LoadResourcePack(path.PlusFile("SoundSpacePlus.pck"), true);

		AudioServer.SetBusLayout(GD.Load<AudioBusLayout>("res://default_bus_layout.tres"));

		CallDeferred(nameof(RunGame));
	}
	public void RunGame()
	{
		var skip = options.GetNode<CheckBox>("SkipLoading").Pressed;

		InitScripts(skip);

		if (skip) SSPInit();
		else GetTree().ChangeScene("res://init.tscn");
	}
	public void InitScripts(bool noGlobal = false)
	{
		GetNode<Node>("/root/Globals").SetScript(GD.Load("res://classes/Globals.gd"));
		GetNode<Node>("/root/Online").SetScript(GD.Load("res://classes/Online.gd"));
		GetNode<Node>("/root/SSP").SetScript(GD.Load("res://classes/SoundSpacePlus.gd"));
		GetNode<Node>("/root/RQueue").SetScript(GD.Load("res://classes/ResourceQueue.gd"));
		GetNode<Node>("/root/Discord").SetScript(GD.Load("res://addons/discord_game_sdk/discord.gd"));
		GetNode<Node>("/root/Dance").SetScript(GD.Load("res://classes/Dance.gd"));
		if (!noGlobal) NodeInit("/root/Globals");
		NodeInit("/root/Online");
		NodeInit("/root/SSP");
		NodeInit("/root/RQueue");
		NodeInit("/root/Discord");
		NodeInit("/root/Dance");
	}
	public void SSPInit() // Replaces init.tscn
	{
		var ssp = GetNode<Node>("/root/SSP");
		var globals = GetNode<Node>("/root/Globals");
		// Globals.gd -- this isn't gonna be run on the steam ver surely :clueless:
		globals.Set("speed_multi", new Godot.Collections.Array(1, 1 / 1.35, 1 / 1.25, 1 / 1.15, 1.15, 1.25, 1.35, ssp.Get("custom_speed"), 1.45));

		((RegEx)globals.Get("url_regex")).Compile("((http|https)://)(www.)?[a-zA-Z0-9@:%._\\+~#?&//=]{2,256}" + "\\.[a-z]{2,6}\\b([-a-zA-Z0-9@:%._\\+~#?&//=]*)");
		var confirm_prompt = GD.Load<PackedScene>("res://confirm.tscn").Instance();
		globals.Set("confirm_prompt", confirm_prompt);
		GetTree().Root.CallDeferred("add_child", confirm_prompt);
		var file_sel = GD.Load<PackedScene>("res://filesel.tscn").Instance();
		globals.Set("file_sel", file_sel);
		GetTree().Root.CallDeferred("add_child", file_sel);
		var notify_gui = GD.Load<PackedScene>("res://notification_gui.tscn").Instance();
		globals.Set("notify_gui", notify_gui);
		GetTree().Root.CallDeferred("add_child", notify_gui);

		var fps_disp = (Label)globals.Get("fps_disp");
		fps_disp.MarginLeft = 15;
		fps_disp.MarginTop = 15;
		fps_disp.MarginRight = 0;
		fps_disp.MarginBottom = 0;
		fps_disp.Set("custom_fonts/font", GD.Load("res://font/debug2.tres"));
		GetTree().Root.CallDeferred("add_child", fps_disp);
		globals.Set("fps_visible", true);

		// init.gd
		var thread = new Thread();
		ssp.Set("is_init", false);
		ssp.Connect("init_stage_reached", this, nameof(StageReached));
		thread.Start(ssp, "do_init");
	}
	public void StageReached(string stage)
	{
		GetNode<Label>("Panel/State").Text = stage;
	}
	public void StageReached(string stage, bool done)
	{
		if (done)
		{
			GetNode<Node>("/root/SSP").Disconnect("init_stage_reached", this, nameof(StageReached));
			GetTree().CallDeferred("change_scene", "res://menu2.tscn");
		}
	}
}
