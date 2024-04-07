using Chinook.Models;

namespace Chinook.Interfaces
{
    public interface IArtistService
    {
        Task<List<Artist>> GetArtists();
        Task<Artist> GetArtistById(long id);
    }
}
