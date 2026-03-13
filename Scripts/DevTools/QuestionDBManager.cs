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
    private readonly QuestionRepository _repository = new QuestionRepository(Path.Questions);

    public override void _Ready()
    {
        LoadLocalDatabase();
    }

    public void LoadLocalDatabase()
    {
        Database = _repository.Load();
        OnStatusUpdated?.Invoke($"Loaded {Database.Count} questions.");
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

    public async Task<List<OpenTDBCategory>> FetchCategoriesAsync() => await _apiService.FetchCategoriesAsync();

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
