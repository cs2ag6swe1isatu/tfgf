// QuestionDBUI connects a Godot control node to the QuestionDBManager script
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class QuestionDBUI : Control
{
    [Export] private QuestionDBManager _db;

    private OptionButton _optCategory;
    private OptionButton _optDifficulty;
    private Button _btnLoad;
    private Button _btnDownload;
    private Button _btnAdd;
    private Button _btnBrowse;
    private Button _btnAddCategory;
    private LineEdit _txtPath;
    private LineEdit _txtNewCategory;
    private FileDialog _fileDialog;
    private Label _lblStatus;
    private VBoxContainer _vBoxQuestions;
    private QuestionEditor _editor;

    public override async void _Ready()
    {
        _optCategory = GetNode<OptionButton>("MarginContainer/VBoxContainer/CategoryOption");
        _optDifficulty = GetNode<OptionButton>("MarginContainer/VBoxContainer/DifficultyOption");
        _btnLoad = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/LoadButton");
        _btnDownload = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/DownloadButton");
        _btnAdd = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/AddButton");
        _lblStatus = GetNode<Label>("MarginContainer/VBoxContainer/StatusLabel");
        _vBoxQuestions = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/VBoxContainer");

        _btnBrowse = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/PathHBox/BrowseButton");
        _txtPath = GetNodeOrNull<LineEdit>("MarginContainer/VBoxContainer/PathHBox/PathEdit");
        _btnAddCategory = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/CategoryHBox/AddCategoryButton");
        _txtNewCategory = GetNodeOrNull<LineEdit>("MarginContainer/VBoxContainer/CategoryHBox/NewCategoryEdit");

        if (_txtPath != null)
        {
            _txtPath.Text = _db.CurrentDatabasePath;
            _txtPath.Editable = false;
        }

        if (_btnBrowse != null)
        {
            _btnBrowse.Pressed += OnBrowsePressed;
        }

        if (_btnAddCategory != null)
        {
            _btnAddCategory.Pressed += OnAddCategoryPressed;
        }

        if (_txtNewCategory != null)
        {
            _txtNewCategory.PlaceholderText = "Enter new category...";
        }

        _fileDialog = new FileDialog();
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        _fileDialog.AddFilter("*.json", "JSON Files");
        _fileDialog.FileSelected += OnFileSelected;
        AddChild(_fileDialog);

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
        
        _optDifficulty.Clear();
        _optDifficulty.AddItem("Easy");
        _optDifficulty.AddItem("Medium");
        _optDifficulty.AddItem("Hard");

        _btnLoad.Pressed += OnLoadPressed;
        _btnDownload.Pressed += OnDownloadPressed;
        _optCategory.ItemSelected += OnFilterChanged;
        _optDifficulty.ItemSelected += OnFilterChanged;

        _db.OnStatusUpdated += UpdateStatus;
        _db.OnDatabaseUpdated += RefreshQuestionList;

        _editor = new QuestionEditor();
        AddChild(_editor);
        _editor.OnSave += OnEditorSaved;

        _btnAdd.Pressed += OnAddPressed;

        RefreshQuestionList();
    }

    private void OnAddCategoryPressed()
    {
        if (_txtNewCategory != null && !string.IsNullOrWhiteSpace(_txtNewCategory.Text))
        {
            string newCategory = _txtNewCategory.Text.Trim();
            _db.AddCustomCategory(newCategory);
            
            // Refresh the category list
            _optCategory.Clear();
            _optCategory.AddItem("Reloading Categories...", 0);
            _optCategory.Disabled = true;
            
            // Reload categories asynchronously
            Task.Run(async () =>
            {
                await Task.Delay(100); // Small delay to allow UI to update
                Callable.From(() =>
                {
                    RefreshCategoryList();
                }).CallDeferred();
            });
            
            _txtNewCategory.Text = ""; // Clear the input field
        }
    }

    private async void RefreshCategoryList()
    {
        var categories = await _db.FetchCategoriesAsync();
        _optCategory.Clear();
        
        if (categories.Count > 0)
        {
            foreach (var cat in categories)
            {
                _optCategory.AddItem(cat.Name, cat.Id);
            }
            _optCategory.Disabled = false;
            UpdateStatus("Categories refreshed.");
        }
        else
        {
            _optCategory.AddItem("No categories available.", 0);
            UpdateStatus("No categories available.");
        }
    }

    private QuestionModel _editingQuestion = null;

    private void OnAddPressed()
    {
        _editingQuestion = null;
        string category = _optCategory.GetItemText(_optCategory.Selected);
        string difficulty = _optDifficulty.GetItemText(_optDifficulty.Selected);

        var newQ = new QuestionModel
        {
            Category = category,
            Difficulty = difficulty,
            QuestionText = "New Question",
            CorrectAnswer = "",
            IncorrectAnswers = new List<string> { "", "", "" }
        };

        _editor.Title = "Add Question";
        _editor.LoadQuestion(newQ);
        _editor.PopupCentered();
    }

    private void OnEditorSaved(QuestionModel updatedQ)
    {
        if (_editingQuestion == null)
        {
            _db.AddQuestion(updatedQ);
        }
        else
        {
            int index = _db.Database.IndexOf(_editingQuestion);
            if (index != -1)
            {
                _db.UpdateQuestion(index, updatedQ);
            }
        }
        _editingQuestion = null;
    }

    private void OnBrowsePressed()
    {
        _fileDialog.PopupCentered(new Vector2I(700, 500));
    }

    private void OnFileSelected(string path)
    {
        if (_txtPath != null) _txtPath.Text = path;
        _db.SetDatabasePath(path);
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
        await Task.Delay(5000); 
        UpdateStatus("Download complete.");
        
        _btnDownload.Disabled = false;
    }

    private void UpdateStatus(string message)
    {
        if (_lblStatus != null)
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
            _vBoxQuestions.AddChild(CreateQuestionRow(q));
        }

        UpdateStatus($"Showing {filteredQuestions.Count} questions.");
    }

    private Node CreateQuestionRow(QuestionModel q)
    {
        var hBox = new HBoxContainer();
        
        var label = new Label();
        label.Text = q.QuestionText;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(200, 0);
        hBox.AddChild(label);

        var btnEdit = new Button();
        btnEdit.Text = "Edit";
        btnEdit.Pressed += () => OnEditQuestion(q);
        hBox.AddChild(btnEdit);

        var btnDelete = new Button();
        btnDelete.Text = "Delete";
        btnDelete.Pressed += () => OnDeleteQuestion(q);
        hBox.AddChild(btnDelete);

        return hBox;
    }

    private void OnEditQuestion(QuestionModel q)
    {
        _editingQuestion = q;
        _editor.Title = "Edit Question";
        _editor.LoadQuestion(q);
        _editor.PopupCentered();
    }

    private void OnDeleteQuestion(QuestionModel q)
    {
        int index = _db.Database.IndexOf(q);
        if (index != -1)
        {
            _db.DeleteQuestion(index);
        }
    }
}
