using Microsoft.AspNetCore.Http;

namespace API.Interfaces
{
    public interface IPhotoService
    {
        Task<string> AddPhotoAsync(IFormFile file);
        Task<bool> DeletePhotoAsync(string fileName);
    }
}