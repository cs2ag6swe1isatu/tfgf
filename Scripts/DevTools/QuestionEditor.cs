// QuestionEditor is a modal screen for showwing&modifying question text and choices
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class QuestionEditor : ConfirmationDialog
{
    private LineEdit _txtQuestion;
    private LineEdit _txtCorrect;
    private List<LineEdit> _txtIncorrect = new List<LineEdit>();
    private Label _lblCategory;
    private Label _lblDifficulty;

    public event Action<QuestionModel> OnSave;

    private QuestionModel _currentModel;

    public QuestionEditor()
    {
        Title = "Edit Question";
        Size = new Vector2I(400, 300);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        AddChild(margin);

        var vBox = new VBoxContainer();
        margin.AddChild(vBox);

        _lblCategory = new Label();
        vBox.AddChild(_lblCategory);

        _lblDifficulty = new Label();
        vBox.AddChild(_lblDifficulty);

        vBox.AddChild(new Label { Text = "Question Text:" });
        _txtQuestion = new LineEdit();
        vBox.AddChild(_txtQuestion);

        vBox.AddChild(new Label { Text = "Correct Answer:" });
        _txtCorrect = new LineEdit();
        vBox.AddChild(_txtCorrect);

        vBox.AddChild(new Label { Text = "Incorrect Answers:" });
        for (int i = 0; i < 3; i++)
        {
            var le = new LineEdit();
            vBox.AddChild(le);
            _txtIncorrect.Add(le);
        }

        Confirmed += HandleConfirmed;
    }

    public void LoadQuestion(QuestionModel q)
    {
        _currentModel = q;
        _lblCategory.Text = $"Category: {q.Category}";
        _lblDifficulty.Text = $"Difficulty: {q.Difficulty}";
        _txtQuestion.Text = q.QuestionText;
        _txtCorrect.Text = q.CorrectAnswer;

        for (int i = 0; i < _txtIncorrect.Count; i++)
        {
            _txtIncorrect[i].Text = i < q.IncorrectAnswers.Count ? q.IncorrectAnswers[i] : "";
        }
    }

    private void HandleConfirmed()
    {
        if (_currentModel == null) return;

        var updated = new QuestionModel
        {
            Category = _currentModel.Category,
            Difficulty = _currentModel.Difficulty,
            QuestionText = _txtQuestion.Text,
            CorrectAnswer = _txtCorrect.Text,
            IncorrectAnswers = _txtIncorrect.Select(le => le.Text).Where(t => !string.IsNullOrEmpty(t)).ToList()
        };
        updated.AllAnswers = new List<string>(updated.IncorrectAnswers) { updated.CorrectAnswer };

        OnSave?.Invoke(updated);
    }
}
