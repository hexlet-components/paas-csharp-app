namespace PaasCsharpApp;

/// <summary>Строка счётчика посещений: путь страницы и число посещений.</summary>
internal sealed class PageVisit
{
    public required string Path { get; set; }

    public long Total { get; set; }
}
