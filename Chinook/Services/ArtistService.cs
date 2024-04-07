using Chinook.Interfaces;
using Chinook.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services
{
    public class ArtistService : IArtistService
    {
        private IDbContextFactory<ChinookContext> DbFactory;

        public ArtistService(IDbContextFactory<ChinookContext> _DbFactory)
        {
            this.DbFactory = _DbFactory;
        }

        public async Task<Artist> GetArtistById(long id)
        {
            var dbContext = await DbFactory.CreateDbContextAsync();
            return dbContext.Artists.SingleOrDefault(a => a.ArtistId == id);
        }

        public async Task<List<Artist>> GetArtists()
        {
            var dbContext = await DbFactory.CreateDbContextAsync();
            return dbContext.Artists.Include(a => a.Albums).ToList();
        }
    }
}
