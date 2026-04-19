namespace HairyPaws.Application.Common.Files;

public sealed record UploadedFile(
    string FileName,
    string ContentType,
    byte[] Content)
{
    public long Length => Content.LongLength;

    public Stream OpenReadStream() => new MemoryStream(Content, writable: false);
}
