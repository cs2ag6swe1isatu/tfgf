// Contains methods and attributes for CRUD operations to the questions json database
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public class QuestionRepository
{
    private readonly string _dbPath;

    public QuestionRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    public List<QuestionModel> Load()
    {
        if (!FileAccess.FileExists(_dbPath))
        {
            return new List<QuestionModel>();
        }

        using var file = FileAccess.Open(_dbPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        
        try 
        {
            return JsonSerializer.Deserialize<List<QuestionModel>>(json) ?? new List<QuestionModel>();
        }
        catch
        {
            GD.Print("[QuestionRepository] Could not parse as List<QuestionModel>, check format.");
            return new List<QuestionModel>();
        }
    }

    public void Save(List<QuestionModel> questions)
    {
        string directory = _dbPath.GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(directory))
        {
            DirAccess.MakeDirRecursiveAbsolute(directory);
        }

        string json = JsonSerializer.Serialize(questions, new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(_dbPath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }
}
