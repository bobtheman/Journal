using MudColor = MudBlazor.Color;

namespace Journal.Components.Shared
{
    public static class MoodIcons
    {
        public static readonly (int Value, string Icon, MudColor Color, string? Hex, string Label)[] All =
        [
            (1, MudBlazor.Icons.Material.Filled.SentimentVerySatisfied, MudColor.Success, null, "Great"),
            (2, MudBlazor.Icons.Material.Filled.SentimentSatisfied, MudColor.Info, null, "Good"),
            (3, MudBlazor.Icons.Material.Filled.SentimentNeutral, MudColor.Default, "#93A381", "Okay"),
            (4, MudBlazor.Icons.Material.Filled.SentimentDissatisfied, MudColor.Warning, null, "Bad"),
            (5, MudBlazor.Icons.Material.Filled.SentimentVeryDissatisfied, MudColor.Error, null, "Very bad")
        ];

        public static string GetIcon(int value) => All.First(m => m.Value == value).Icon;

        public static MudColor GetColor(int value) => All.First(m => m.Value == value).Color;

        public static string? GetHex(int value) => All.First(m => m.Value == value).Hex;
    }
}
