using MudColor = MudBlazor.Color;

namespace Journal.Components.Shared
{
    public static class MoodIcons
    {
        public static readonly (int Value, string Icon, MudColor Color, string Label)[] All =
        [
            (1, MudBlazor.Icons.Material.Filled.SentimentVerySatisfied, MudColor.Success, "Great"),
            (2, MudBlazor.Icons.Material.Filled.SentimentSatisfied, MudColor.Info, "Good"),
            (3, MudBlazor.Icons.Material.Filled.SentimentNeutral, MudColor.Default, "Okay"),
            (4, MudBlazor.Icons.Material.Filled.SentimentDissatisfied, MudColor.Warning, "Bad"),
            (5, MudBlazor.Icons.Material.Filled.SentimentVeryDissatisfied, MudColor.Error, "Very bad")
        ];

        public static string GetIcon(int value) => All.First(m => m.Value == value).Icon;

        public static MudColor GetColor(int value) => All.First(m => m.Value == value).Color;
    }
}
