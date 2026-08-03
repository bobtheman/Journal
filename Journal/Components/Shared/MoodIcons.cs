using MudColor = MudBlazor.Color;

namespace Journal.Components.Shared
{
    public static class MoodIcons
    {
        // Explicit hex values instead of the MudBlazor theme colors - those varied in
        // brightness/saturation across light/dark mode and left "Okay" a dull, low-contrast
        // olive-gray. This is a deliberate bright, high-contrast traffic-light gradient
        // instead, readable on both light and dark surfaces.
        public static readonly (int Value, string Icon, MudColor Color, string Hex, string Label)[] All =
        [
            (1, MudBlazor.Icons.Material.Filled.SentimentVerySatisfied, MudColor.Success, "#16A34A", "Great"),
            (2, MudBlazor.Icons.Material.Filled.SentimentSatisfied, MudColor.Info, "#0D9488", "Good"),
            (3, MudBlazor.Icons.Material.Filled.SentimentNeutral, MudColor.Warning, "#D97706", "Okay"),
            (4, MudBlazor.Icons.Material.Filled.SentimentDissatisfied, MudColor.Warning, "#EA580C", "Bad"),
            (5, MudBlazor.Icons.Material.Filled.SentimentVeryDissatisfied, MudColor.Error, "#DC2626", "Very bad")
        ];

        public static string GetIcon(int value) => All.First(m => m.Value == value).Icon;

        public static MudColor GetColor(int value) => All.First(m => m.Value == value).Color;

        public static string GetHex(int value) => All.First(m => m.Value == value).Hex;

        // Rolls a day's mood scores up to the nearest of the 5 bands so the day header can
        // show a single representative colour (1 = best, 5 = worst - same scale as All).
        public static int RoundToNearestBand(double average) =>
            Math.Clamp((int)Math.Round(average, MidpointRounding.AwayFromZero), All.Min(m => m.Value), All.Max(m => m.Value));
    }
}
