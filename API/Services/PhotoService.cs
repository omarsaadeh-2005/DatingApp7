using API.Interfaces;

namespace API.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly IWebHostEnvironment _env;

        public PhotoService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> AddPhotoAsync(IFormFile file)
        {
            if (file.Length == 0) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "images");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // THIS is what gets stored in DB
            var url = "images/" + fileName;

            return url;
        }

        public Task<bool> DeletePhotoAsync(string fileName)
        {
            var filePath = Path.Combine(_env.WebRootPath, "images", fileName);

            if (!File.Exists(filePath)) return Task.FromResult(false);

            File.Delete(filePath);

            return Task.FromResult(true);
        }
    }
}