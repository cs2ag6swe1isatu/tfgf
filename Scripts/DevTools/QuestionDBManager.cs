// QuestionDBManager is a dev tool script for automating question json management and api calls to OpenTDB
// Todo: separate CRUD operations and api fetch calls
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class QuestionDBManager : Node
{
    private const string DBPath = "res://Data/Questions.json";
    
    // Holds all currently loaded/downloaded questions
    public List<OpenTDBQuestion> Database { get; private set; } = new List<OpenTDBQuestion>();

    public event Action OnDatabaseUpdated;
    public event Action<string> OnStatusUpdated;

    private static readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();

    public void LoadLocalDatabase()
    {
        if (!FileAccess.FileExists(DBPath))
        {
            OnStatusUpdated?.Invoke("No local database found.");
            return;
        }

        using var file = FileAccess.Open(DBPath, FileAccess.ModeFlags.Read);
        var response = JsonSerializer.Deserialize<OpenTDBResponse>(file.GetAsText());
        
        if (response?.Results != null)
        {
            Database = response.Results;
            OnStatusUpdated?.Invoke($"Loaded {Database.Count} questions locally.");
            OnDatabaseUpdated?.Invoke();
        }
    }

    // Download, merge, and sync
    public async Task UpdateDatabase(int category, string difficulty)
    {
        if (Database.Count == 0 && FileAccess.FileExists(DBPath))
        {
            LoadLocalDatabase();
        }

        OnStatusUpdated?.Invoke($"Downloading Cat: {category}, Diff: {difficulty}...");

        int available = await GetAvailableCount(category, difficulty);
        int amountToRequest = Math.Min(50, available);

        if (amountToRequest == 0)
        {
            OnStatusUpdated?.Invoke("No questions available for this category/difficulty.");
            return;
        }
        
        string url = $"https://opentdb.com/api.php?amount=50&category={category}&difficulty={difficulty}&type=multiple&encode=base64";
        
        try
        {
            string json = await _client.GetStringAsync(url);
            GD.Print(json);
            var response = JsonSerializer.Deserialize<OpenTDBResponse>(json);

            if (response != null && response.ResponseCode == 0)
            {
                Database.AddRange(response.Results); // merge
                Database = Database.DistinctBy(q => q.Question).ToList(); // remove dupes
                
                SaveDatabase();
                OnStatusUpdated?.Invoke($"Success! DB now has {Database.Count} questions.");
                OnDatabaseUpdated?.Invoke();
            }
            else
            {
                OnStatusUpdated?.Invoke($"API Error or Rate Limited (Code {response?.ResponseCode}).");
            }
        }
        catch (Exception e)
        {
            OnStatusUpdated?.Invoke($"Download failed: {e.Message}");
        }
    }

    public async Task<List<OpenTDBCategory>> FetchCategoriesAsync()
    {
        string url = "https://opentdb.com/api_category.php";
        try
        {
            string json = await _client.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<OpenTDBCategoryResponse>(json);
            
            if (response?.TriviaCategories != null)
            {
                GD.Print($"[i] Successfully fetched {response.TriviaCategories.Count} categories.");
                return response.TriviaCategories;
            }
        }
        catch (Exception e)
        {
            GD.PushError($"[!] Failed to fetch categories: {e.Message}");
        }
        
        return new List<OpenTDBCategory>(); 
    }

    public async Task<int> GetAvailableCount(int categoryId, string difficulty)
    {
        string url = $"https://opentdb.com/api_count.php?category={categoryId}";
        
        try
        {
            string json = await _client.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<OpenTDBCountResponse>(json);

            if (response?.CategoryQuestionCount == null)
                return 0;

            return difficulty switch
            {
                "easy" => response.CategoryQuestionCount.Easy,
                "medium" => response.CategoryQuestionCount.Medium,
                "hard" => response.CategoryQuestionCount.Hard,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }

    private void SaveDatabase()
    {
        DirAccess.MakeDirAbsolute("res://Data");
        var finalData = new OpenTDBResponse { ResponseCode = 0, Results = Database };
        string json = JsonSerializer.Serialize(finalData, new JsonSerializerOptions { WriteIndented = false });
        using var file = FileAccess.Open(DBPath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public List<OpenTDBQuestion> GetFilteredQuestions(string plainCategory, string plainDifficulty)
    {   
        return Database.Where(q => 
            DecodeBase64(q.Category).ToLower() == plainCategory.ToLower() && 
            DecodeBase64(q.Difficulty).ToLower() == plainDifficulty.ToLower()
        ).ToList();
    }
    private string DecodeBase64(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));
}