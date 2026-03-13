// QuestionModel defines the structure of the question data 
using System.Text.Json.Serialization;
using System.Collections.Generic;

public class QuestionModel
{
    public string Category { get; set; }
    public string Difficulty { get; set; }
    public string QuestionText { get; set; }
    public string CorrectAnswer { get; set; }
    public List<string> IncorrectAnswers { get; set; }
    public List<string> AllAnswers { get; set; } = new List<string>();
}
