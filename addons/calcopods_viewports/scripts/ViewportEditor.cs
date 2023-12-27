using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace CalcopodsViewports;

[Tool]
public partial class ViewportEditor : Control
{
	private LineEdit _pathInput;
	private Label _selectedLabel;
	private PopupMenu _selectMenu;
	private Button _applyBtn;

	private readonly List<ViewportSettings> _viewportSettings = new();


    public override void _EnterTree()
    {
		_pathInput = GetNode<LineEdit>("%PathInput");
        _selectedLabel = GetNode<Label>("%SelectedLabel");
		_selectMenu = GetNode<PopupMenu>("%SelectMenu");
		_applyBtn = GetNode<Button>("%ApplyBtn");
    }

    public override void _Ready()
    {
		_selectMenu.IndexPressed += HandleItemSelected;
		_selectMenu.AboutToPopup += HandlePopupOpen;

		_applyBtn.ButtonDown += HandleApplySettings;
    }

	// TODO(calco): Make work with select
	private void HandleApplySettings()
	{
		string path = _pathInput.Text;
		ViewportSettings settings = (ViewportSettings) ResourceLoader.Load<Resource>(path);
		
		// TODO(calco): later
		ProjectSettings.SetSetting("display/window/size/viewport_width", settings.HighRes.X);
		ProjectSettings.SetSetting("display/window/size/viewport_height", settings.HighRes.Y);
	}

    private void HandleItemSelected(long index)
    {
		GD.Print($"SELECTED: {index}");
		GD.Print($"{_viewportSettings[(int)index].LowRes} -> {_viewportSettings[(int)index].HighRes}");
    }

	private void HandlePopupOpen()
	{
		_viewportSettings.Clear();
		
		List<string> files = new();
		GetAllFilesFromDir(files, "res://resources");

		foreach (string file in files)
		{
			GD.Print($"Checking: {file}");
			try {
				ViewportSettings hmmm = ResourceLoader.Load<ViewportSettings>(file);
				_viewportSettings.Add(hmmm);
				GD.Print("WOOOOOORKS");
			}
			catch {
				GD.Print("failed");
			}
		}

		for (int i = 0; i < _selectMenu.ItemCount; ++i)
			_selectMenu.RemoveItem(i);
		
		foreach (ViewportSettings settings in _viewportSettings)
			_selectMenu.AddItem(settings.ResourcePath);
	}

	private void GetAllFilesFromDir(List<string> strings, string dirpath)
	{
		GD.Print($"Getting files from: {dirpath}");

		string[] dirs = DirAccess.GetDirectoriesAt(dirpath);
		foreach (string dir in dirs)
			GetAllFilesFromDir(strings, $"{dirpath}/{dir}");
		
		string[] files = DirAccess.GetFilesAt(dirpath);
		foreach (string file in files)
			strings.Add($"{dirpath}/{file}");
	}
}
