// QuestionDBManager is a dev tool script for automating json management and api calls to OpenTDB
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFGF.Core;
public partial class QuestionDBManager : Node
{
    
    public List<QuestionModel> Database { get; private set; } = new List<QuestionModel>();

    public event Action OnDatabaseUpdated;
    public event Action<string> OnStatusUpdated;

    private readonly OpenTDBService _apiService = new OpenTDBService();
    private QuestionRepository _repository = new QuestionRepository(Path.Questions);

    public string CurrentDatabasePath => _repository.DbPath;

    public override void _Ready()
    {
        LoadLocalDatabase();
    }

    public void SetDatabasePath(string path)
    {
        _repository.DbPath = path;
        LoadLocalDatabase();
    }

    public void LoadLocalDatabase()
    {
        if (string.IsNullOrEmpty(_repository.DbPath))
        {
            Database = new List<QuestionModel>();
            OnStatusUpdated?.Invoke("No database file selected.");
            OnDatabaseUpdated?.Invoke();
            return;
        }

        Database = _repository.Load();
        string fileName = Godot.FileAccess.FileExists(_repository.DbPath) ? _repository.DbPath.GetFile() : "new file";
        OnStatusUpdated?.Invoke($"Loaded {Database.Count} questions from {fileName}.");
        OnDatabaseUpdated?.Invoke();
    }

    public async Task UpdateDatabase(int category, string difficulty)
    {
        OnStatusUpdated?.Invoke($"Downloading Cat: {category}, Diff: {difficulty}...");

        try
        {
            var response = await _apiService.FetchQuestionsAsync(category, difficulty);

            var newQuestions = response.Results.Select(MapToModel).ToList();

            foreach (var nq in newQuestions)
            {
                if (!Database.Any(dbq => dbq.QuestionText == nq.QuestionText))
                {
                    Database.Add(nq);
                }
            }

            _repository.Save(Database);
            OnStatusUpdated?.Invoke($"Success! DB now has {Database.Count} questions.");
            OnDatabaseUpdated?.Invoke();
        }
        catch (OpenTDBException e)
        {
            if (e.ResponseCode == 4)
            {
                OnStatusUpdated?.Invoke("Token Empty. Resetting token...");
                try
                {
                    await _apiService.ResetTokenAsync();
                    OnStatusUpdated?.Invoke("Token reset. Retrying...");
                    await UpdateDatabase(category, difficulty);
                    return;
                }
                catch (Exception resetEx)
                {
                    OnStatusUpdated?.Invoke($"Reset Failed: {resetEx.Message}");
                }
            }
            
            OnStatusUpdated?.Invoke($"API Error ({e.ResponseCode}): {e.Message}");
            GD.PushError($"[QuestionDBManager] OpenTDB Error: {e.Message}");
        }
        catch (Exception e)
        {
            OnStatusUpdated?.Invoke($"Error: {e.Message}");
            GD.PushError($"[QuestionDBManager] Failed to update database: {e.Message}");
        }
    }

    private HashSet<string> _customCategories = new HashSet<string>();

    public List<OpenTDBCategory> GetCustomCategories()
    {
        var result = new List<OpenTDBCategory>();
        int id = 2000; // Start from 2000 for custom categories to avoid conflicts
        foreach (var categoryName in _customCategories.OrderBy(c => c))
        {
            result.Add(new OpenTDBCategory { Id = id++, Name = categoryName });
        }
        return result;
    }

    public void AddCustomCategory(string categoryName)
    {
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            _customCategories.Add(categoryName.Trim());
            SaveCustomCategories();
        }
    }

    public void RemoveCustomCategory(string categoryName)
    {
        _customCategories.Remove(categoryName);
        SaveCustomCategories();
    }

    private void SaveCustomCategories()
    {
        // Save custom categories to a file
        string customCategoriesPath = "user://custom_categories.json";
        using var file = FileAccess.Open(customCategoriesPath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(_customCategories.ToArray(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            file.StoreString(json);
        }
    }

    private void LoadCustomCategories()
    {
        string customCategoriesPath = "user://custom_categories.json";
        if (FileAccess.FileExists(customCategoriesPath))
        {
            using var file = FileAccess.Open(customCategoriesPath, FileAccess.ModeFlags.Read);
            if (file != null)
            {
                string json = file.GetAsText();
                try
                {
                    var loadedCategories = System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
                    if (loadedCategories != null)
                    {
                        foreach (string category in loadedCategories)
                        {
                            _customCategories.Add(category);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    GD.PushError($"[QuestionDBManager] Failed to load custom categories: {ex.Message}");
                }
            }
        }
    }

    public async Task<List<OpenTDBCategory>> FetchCategoriesAsync()
    {
        // Load custom categories first
        LoadCustomCategories();

        // Try to fetch from API first with a 5-second timeout
        List<OpenTDBCategory> apiCategories = null;
        try
        {
            // Temporarily modify the OpenTDBService to use a 5-second timeout for this specific call
            // We'll do this by creating a race condition with a delay task
            var apiTask = _apiService.FetchCategoriesAsync();
            var delayTask = Task.Delay(TimeSpan.FromSeconds(5));
            
            if (await Task.WhenAny(apiTask, delayTask) == delayTask)
            {
                // Timeout occurred
                GD.PushWarning("[QuestionDBManager] API fetch timed out after 5 seconds. Falling back to local categories.");
                return GetFallbackCategories();
            }
            
            apiCategories = await apiTask;
            if (apiCategories != null && apiCategories.Count > 0)
            {
                return apiCategories;
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[QuestionDBManager] API fetch failed: {ex.Message}. Falling back to local categories.");
        }

        // Fallback to extracting categories from the current database
        return GetFallbackCategories();
    }

    private List<OpenTDBCategory> GetFallbackCategories()
    {
        var localCategories = new HashSet<string>();
        foreach (var question in Database)
        {
            if (!string.IsNullOrEmpty(question.Category))
            {
                localCategories.Add(question.Category);
            }
        }

        // Combine local categories with custom categories
        foreach (var customCategory in _customCategories)
        {
            localCategories.Add(customCategory);
        }

        // Convert to OpenTDBCategory format (with dummy IDs)
        var result = new List<OpenTDBCategory>();
        int id = 1000; // Start from 1000 to avoid conflicts with API IDs
        foreach (var categoryName in localCategories.OrderBy(c => c))
        {
            result.Add(new OpenTDBCategory { Id = id++, Name = categoryName });
        }

        return result;
    }

    public void SaveDatabase()
    {
        _repository.Save(Database);
        OnStatusUpdated?.Invoke("Database saved.");
    }

    public void AddQuestion(QuestionModel question)
    {
        Database.Add(question);
        SaveDatabase();
        OnDatabaseUpdated?.Invoke();
    }

    public void UpdateQuestion(int index, QuestionModel updatedQuestion)
    {
        if (index >= 0 && index < Database.Count)
        {
            Database[index] = updatedQuestion;
            SaveDatabase();
            OnDatabaseUpdated?.Invoke();
        }
    }

    public void DeleteQuestion(int index)
    {
        if (index >= 0 && index < Database.Count)
        {
            Database.RemoveAt(index);
            SaveDatabase();
            OnDatabaseUpdated?.Invoke();
        }
    }

    public List<QuestionModel> GetFilteredQuestions(string plainCategory, string plainDifficulty)
    {   
        return Database.Where(q => 
            q.Category.Equals(plainCategory, StringComparison.OrdinalIgnoreCase) && 
            q.Difficulty.Equals(plainDifficulty, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    private QuestionModel MapToModel(OpenTDBQuestion q)
    {
        var model = new QuestionModel
        {
            Category = DecodeBase64(q.Category),
            Difficulty = DecodeBase64(q.Difficulty),
            QuestionText = DecodeBase64(q.Question),
            CorrectAnswer = DecodeBase64(q.CorrectAnswer),
            IncorrectAnswers = q.IncorrectAnswers.Select(DecodeBase64).ToList()
        };
        model.AllAnswers = new List<string>(model.IncorrectAnswers) { model.CorrectAnswer };
        return model;
    }

    private string DecodeBase64(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));
}
