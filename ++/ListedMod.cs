using Godot;
using System;

public class ListedMod : Control
{
	[Export]
	public bool IsGame;
	public string Path;
	public override void _Ready()
	{
		GetNode<CheckBox>("Enabled").Text = Name;
		GetNode<Button>("Sort/Up").Connect("pressed", this, nameof(Up));
		GetNode<Button>("Sort/Down").Connect("pressed", this, nameof(Down));
		if (IsGame)
		{
			Path = "SS+";
			GetNode<CheckBox>("Enabled").Disabled = true;
			GetNode<CheckBox>("Enabled").Pressed = true;
		}
	}
	public void Up() { this.GetParent().MoveChild(this, Math.Max(0, this.GetIndex() - 1)); }
	public void Down() { this.GetParent().MoveChild(this, Math.Min(this.GetParent().GetChildCount(), this.GetIndex() + 1)); }
}
