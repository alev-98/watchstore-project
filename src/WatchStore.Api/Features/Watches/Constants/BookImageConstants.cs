namespace WatchStore.Api.Features.Watches.Constants;

internal static class WatchImageConstants
{
    public const string ImagesFolderName = "watchstore-images";

    public const int ImageMaxSizeMb = 5;

    public static string[] ImageAllowedExtensions => [".jpg", ".png"];

    public const string ImageDefaultUri = "https://placehold.co/100";
}
