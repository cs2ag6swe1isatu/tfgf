// Question DBUI connects a Godot control node to the QuestionDBManager script
// Todo: add edit, remove, create question in the ui (after CRUD works)
using Godot;
using System;
using System.Text;

public partial class QuestionDBUI : Control
{
    [Export] private QuestionDBManager _db;

    private OptionButton _optCategory;
    private OptionButton _optDifficulty;
    private Button _btnLoad;
    private Button _btnDownload;
    private Label _lblStatus;
    private VBoxContainer _vBoxQuestions;

    public override async void _Ready()
    {
        _optCategory = GetNode<OptionButton>("MarginContainer/VBoxContainer/CategoryOption");
        _optDifficulty = GetNode<OptionButton>("MarginContainer/VBoxContainer/DifficultyOption");
        _btnLoad = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/LoadButton");
        _btnDownload = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/DownloadButton");
        _lblStatus = GetNode<Label>("MarginContainer/VBoxContainer/StatusLabel");
        _vBoxQuestions = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/VBoxContainer");

         _optCategory.Clear();
        _optCategory.AddItem("Loading API Categories...", 0);
        _optCategory.Disabled = true; 
        UpdateStatus("Fetching Category List...");
        var categories = await _db.FetchCategoriesAsync();

        _optCategory.Clear(); 
        if (categories.Count > 0)
        {
            foreach (var cat in categories)
            {
                _optCategory.AddItem(cat.Name, cat.Id);
            }
            _optCategory.Disabled = false; 
            UpdateStatus("Ready.");
        }
        else
        {
            _optCategory.AddItem("Failed to load categories.", 0);
            UpdateStatus("Error loading API categories.");
        }
        
        _optDifficulty.AddItem("Easy");
        _optDifficulty.AddItem("Medium");
        _optDifficulty.AddItem("Hard");

        _btnLoad.Pressed += OnLoadPressed;
        _btnDownload.Pressed += OnDownloadPressed;
        _optCategory.ItemSelected += OnFilterChanged;
        _optDifficulty.ItemSelected += OnFilterChanged;

        _db.OnStatusUpdated += UpdateStatus;
        _db.OnDatabaseUpdated += RefreshQuestionList;
    }

    private void OnLoadPressed()
    {
        _db.LoadLocalDatabase(); 
    }
  
    private async void OnDownloadPressed()
    {
        _btnDownload.Disabled = true; 
        
        int categoryId = _optCategory.GetSelectedId();
        string difficulty = _optDifficulty.GetItemText(_optDifficulty.Selected).ToLower();
        
        await _db.UpdateDatabase(categoryId, difficulty);

        UpdateStatus("Cooling down API (5s)...");
        await System.Threading.Tasks.Task.Delay(5000); 
        UpdateStatus("Download complete.");
        
        _btnDownload.Disabled = false;
    }

    private void UpdateStatus(string message)
    {
        _lblStatus.Text = $"Status: {message}";
    }

    private void OnFilterChanged(long index)
    {
        RefreshQuestionList();
    }

    private void RefreshQuestionList()
    {
        foreach (Node child in _vBoxQuestions.GetChildren())
        {
            child.QueueFree();
        }

        string selectedCat = _optCategory.GetItemText(_optCategory.Selected);
        string selectedDiff = _optDifficulty.GetItemText(_optDifficulty.Selected);

        var filteredQuestions = _db.GetFilteredQuestions(selectedCat, selectedDiff);

        foreach (var q in filteredQuestions)
        {
            Label qLabel = new Label();
            qLabel.Text = $"- {DecodeBase64(q.Question)}"; 
            qLabel.AutowrapMode = TextServer.AutowrapMode.Off; 
            _vBoxQuestions.AddChild(qLabel);
        }

        UpdateStatus($"Showing {filteredQuestions.Count} questions.");
    }

    private string DecodeBase64(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));
}