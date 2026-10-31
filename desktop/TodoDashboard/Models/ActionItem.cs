using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TodoDashboard.Models;

public sealed class ActionItem : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string NextAction { get; set; } = "";
    public string Why { get; set; } = "";
    public string Quote { get; set; } = "";
    public int Impact { get; set; } = 3;
    public int Confidence { get; set; } = 3;
    public int EffortMinutes { get; set; } = 30;
    public List<string> Tags { get; set; } = new();

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    [JsonIgnore]
    public int PriorityScore
    {
        get
        {
            var ease = EffortMinutes switch
            {
                <= 5 => 5,
                <= 15 => 4,
                <= 30 => 3,
                <= 60 => 2,
                _ => 1,
            };
            return (int)Math.Round(Impact * Confidence * ease / 125.0 * 100);
        }
    }

    [JsonIgnore]
    public string EffortText => $"{EffortMinutes} 分钟";

    [JsonIgnore]
    public string BadgeText => $"影响 {Impact} · 信心 {Confidence} · {EffortText} · 推荐 {PriorityScore}";

    public event PropertyChangedEventHandler? PropertyChanged;
}
