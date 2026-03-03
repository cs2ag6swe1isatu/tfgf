// Question manager is responsible for reading and validating the data from the question database, and owns and mutates the availableQuestions list.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

public partial class QuestionManager : Node
{
    public List<QuestionModel> AvailableQuestions { get; private set; } = new List<QuestionModel>();

    public override void _Ready()
    {
        LoadQuestions("res://Data/Questions.json");
    }

    public void LoadQuestions(string filePath)
    {
        if (!FileAccess.FileExists(filePath))
        {
            GD.PushError($"[!] Question file not found at: {filePath}");
            return;
        }

        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        string jsonContent = file.GetAsText();

        var response = JsonSerializer.Deserialize<OpenTDBResponse>(jsonContent);

        if (response != null && response.ResponseCode == 0 && response.Results != null)
        {
            foreach (var tdbQuestion in response.Results)
            {
                var cleanQuestion = new QuestionModel
                {
                    Category = DecodeBase64(tdbQuestion.Category),
                    Difficulty = DecodeBase64(tdbQuestion.Difficulty),
                    QuestionText = DecodeBase64(tdbQuestion.Question),
                    CorrectAnswer = DecodeBase64(tdbQuestion.CorrectAnswer),
                    IncorrectAnswers = tdbQuestion.IncorrectAnswers.Select(DecodeBase64).ToList()
                };

                cleanQuestion.AllAnswers.Add(cleanQuestion.CorrectAnswer);
                cleanQuestion.AllAnswers.AddRange(cleanQuestion.IncorrectAnswers);

                AvailableQuestions.Add(cleanQuestion);
            }

            GD.Print($"[i] Successfully loaded and decoded {AvailableQuestions.Count} questions.");
        }
        else
        {
            GD.PushError("[!] Failed to load questions: Response code was not 0 or results were empty.");
        }

    }

    // Helper function to decode OpenTDB Base64 strings
    private string DecodeBase64(string base64EncodedData)
    {
        if (string.IsNullOrEmpty(base64EncodedData)) return string.Empty;
        
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}