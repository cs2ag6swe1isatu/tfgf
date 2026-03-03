// QuestionModel defines the structure of the question data 
using System.Text.Json.Serialization;
using System.Collections.Generic;

// We use OpenTDB api to generate json response, save it offline and then read later
public class OpenTDBCategoryResponse
{
    [JsonPropertyName("trivia_categories")]
    public List<OpenTDBCategory> TriviaCategories { get; set; }
}
public class OpenTDBResponse
{
    [JsonPropertyName("response_code")]
    public int ResponseCode { get; set; }
    [JsonPropertyName("results")]
    public List<OpenTDBQuestion> Results { get; set; }
}
public class OpenTDBCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
public class OpenTDBQuestion
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("correct_answer")]
    public string CorrectAnswer { get; set; }

    [JsonPropertyName("incorrect_answers")]
    public List<string> IncorrectAnswers { get; set; }
}

public class QuestionModel
{
    public string Category { get; set; }
    public string Difficulty { get; set; }
    public string QuestionText { get; set; }
    public string CorrectAnswer { get; set; }
    public List<string> IncorrectAnswers { get; set; }
    public List<string> AllAnswers { get; set; } = new List<string>();
}