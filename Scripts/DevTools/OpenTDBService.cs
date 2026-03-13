// Contains methods and attributes for integrating with the opentdb api
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public class OpenTDBException : Exception
{
    public int ResponseCode { get; }
    public OpenTDBException(int responseCode, string message) : base(message)
    {
        ResponseCode = responseCode;
    }
}

public class OpenTDBService
{
    private static readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();
    private const string BaseUrl = "https://opentdb.com";
    private string _sessionToken = null;

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_sessionToken)) return _sessionToken;

        string url = $"{BaseUrl}/api_token.php?command=request";
        string json = await _client.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<OpenTDBTokenResponse>(json);

        if (response?.ResponseCode == 0)
        {
            _sessionToken = response.Token;
            return _sessionToken;
        }

        throw new OpenTDBException(response?.ResponseCode ?? -1, "Failed to retrieve session token.");
    }

    public async Task ResetTokenAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken)) return;

        string url = $"{BaseUrl}/api_token.php?command=reset&token={_sessionToken}";
        string json = await _client.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<OpenTDBTokenResponse>(json);

        if (response?.ResponseCode != 0)
        {
            throw new OpenTDBException(response?.ResponseCode ?? -1, "Failed to reset session token.");
        }
    }

    public async Task<List<OpenTDBCategory>> FetchCategoriesAsync()
    {
        string url = $"{BaseUrl}/api_category.php";
        try
        {
            string json = await _client.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<OpenTDBCategoryResponse>(json);
            return response?.TriviaCategories ?? new List<OpenTDBCategory>();
        }
        catch (Exception e)
        {
            GD.PushError($"[OpenTDBService] Failed to fetch categories: {e.Message}");
            return new List<OpenTDBCategory>();
        }
    }

    public async Task<int> GetAvailableCount(int categoryId, string difficulty)
    {
        string url = $"{BaseUrl}/api_count.php?category={categoryId}";
        try
        {
            string json = await _client.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<OpenTDBCountResponse>(json);

            if (response?.CategoryQuestionCount == null) return 0;

            return difficulty.ToLower() switch
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

    public async Task<OpenTDBResponse> FetchQuestionsAsync(int category, string difficulty, int amount = 50)
    {
        int amountAvailable = Math.Min(await GetAvailableCount(category, difficulty), amount);
        string token = await GetTokenAsync();
        string url = $"{BaseUrl}/api.php?amount={amountAvailable}&category={category}&difficulty={difficulty}&type=multiple&encode=base64&token={token}";

        string json = await _client.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<OpenTDBResponse>(json);

        if (response == null)
        {
            throw new Exception("Failed to deserialize OpenTDB response.");
        }

        switch (response.ResponseCode)
        {
            case 0:
                return response;
            case 1:
                throw new OpenTDBException(1, "No Results: Could not return results. The API doesn't have enough questions for your query.");
            case 2:
                throw new OpenTDBException(2, "Invalid Parameter: Contains an invalid parameter. Arguments passed in aren't valid.");
            case 3:
                _sessionToken = null; 
                throw new OpenTDBException(3, "Token Not Found: Session Token does not exist.");
            case 4:
                throw new OpenTDBException(4, "Token Empty: Session Token has returned all possible questions for the specified query. Resetting the Token is necessary.");
            case 5:
                throw new OpenTDBException(5, "Rate Limit: Too many requests have occurred. Each IP can only access the API once every 5 seconds.");
            default:
                throw new OpenTDBException(response.ResponseCode, $"Unknown Response Code: {response.ResponseCode}");
        }
    }
}
