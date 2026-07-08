namespace HairyPaws.Application.Common.Ports;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken);

    Task DeleteAsync(string path, CancellationToken cancellationToken);
}
