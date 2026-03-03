// OpenTDBModel defines the structure of the openTDB api response data 
using System.Text.Json.Serialization;
using System.Collections.Generic;

// Question count per category
public class OpenTDBCountResponse
{
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("category_question_count")]
    public CategoryCount CategoryQuestionCount { get; set; }
}

public class CategoryCount
{
    [JsonPropertyName("total_question_count")]
    public int Total { get; set; }

    [JsonPropertyName("total_easy_question_count")]
    public int Easy { get; set; }

    [JsonPropertyName("total_medium_question_count")]
    public int Medium { get; set; }

    [JsonPropertyName("total_hard_question_count")]
    public int Hard { get; set; }
}


// Available categories
public class OpenTDBCategoryResponse
{
    [JsonPropertyName("trivia_categories")]
    public List<OpenTDBCategory> TriviaCategories { get; set; }
}
public class OpenTDBCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

// Question List
public class OpenTDBResponse
{
    [JsonPropertyName("response_code")]
    public int ResponseCode { get; set; }
    [JsonPropertyName("results")]
    public List<OpenTDBQuestion> Results { get; set; }
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
