namespace PtcgpTracker.Api.Images;

public class ImageMirrorOptions
{
    public const string SectionName = "Images";

    /// <summary>Where downloaded card and pack art is stored. In Docker this is a named volume.</summary>
    public string Directory { get; set; } = "/data/images";

    /// <summary>How many images are downloaded at the same time.</summary>
    public int Concurrency { get; set; } = 16;
}
