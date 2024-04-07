using Chinook.ClientModels;
using Chinook.Interfaces;
using Chinook.Models;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services
{
    public class PlaylistService : IPlaylistService
    {
        private IDbContextFactory<ChinookContext> DbFactory;

        public PlaylistService(IDbContextFactory<ChinookContext> _DbFactory)
        {
            this.DbFactory = _DbFactory;
        }

        public async Task<Models.Playlist> AddTrackToPlaylist(Models.Playlist playlist, long trackId, string userId, bool isFavorite)
        {
            var dbContext = await DbFactory.CreateDbContextAsync();

            if (isFavorite)
            {
                var myfavlist = dbContext.Playlists.SingleOrDefault(a => a.Name == "My favorite tracks");

                if (myfavlist == null)
                {
                    myfavlist = new Models.Playlist()
                    {
                        PlaylistId = 0,
                        Name = "My favorite tracks"
                    };
                }

                playlist = myfavlist;
            }

            if (playlist.PlaylistId == 0)
            {
                if (dbContext.Playlists.Where(w => w.Name.ToLower() == playlist.Name.ToLower()).Any())
                {
                    throw new ApplicationException("Playlist already exists, please try different name.");
                }

                playlist.PlaylistId = dbContext.Playlists.Max(m => m.PlaylistId) + 1;
                playlist.UserPlaylists.Add(new Models.UserPlaylist() { UserId = userId, PlaylistId = playlist.PlaylistId });
                await dbContext.Playlists.AddAsync(playlist);

                dbContext.SaveChanges();
            }

            var trackData = dbContext.Tracks.SingleOrDefault(a => a.TrackId == trackId);
            var playlistData = dbContext.Playlists.Include(i => i.Tracks).SingleOrDefault(a => a.PlaylistId == playlist.PlaylistId);

            if (!playlistData.Tracks.Where(w => w.TrackId == trackId).Any())
            {
                playlistData.Tracks.Add(trackData);

                dbContext.SaveChanges();
            }

            return playlist;
        }

        public async Task<ClientModels.Playlist> GetPlaylistById(long playlistId, string userId)
        {
            var dbContext = await DbFactory.CreateDbContextAsync();

            return dbContext.Playlists
           .Include(a => a.Tracks).ThenInclude(a => a.Album).ThenInclude(a => a.Artist)
           .Where(p => p.PlaylistId == playlistId)
           .Select(p => new ClientModels.Playlist()
           {
               Name = p.Name,
               Tracks = p.Tracks.Select(t => new ClientModels.PlaylistTrack()
               {
                   AlbumTitle = t.Album.Title,
                   ArtistName = t.Album.Artist.Name,
                   TrackId = t.TrackId,
                   TrackName = t.Name,
                   IsFavorite = t.Playlists.Where(p => p.UserPlaylists.Any(up => up.UserId == userId && up.Playlist.Name == "My favorite tracks")).Any()
               }).ToList()
           })
           .FirstOrDefault();
        }

        public async Task<List<PlaylistTrack>> GetTracksByArtist(long artistId, string userId)
        {
            var dbContext = await DbFactory.CreateDbContextAsync();

            return dbContext.Tracks.Where(a => a.Album.ArtistId == artistId)
                        .Include(a => a.Album).Select(t => new PlaylistTrack()
                        {
                            AlbumTitle = (t.Album == null ? "-" : t.Album.Title),
                            TrackId = t.TrackId,
                            TrackName = t.Name,
                            IsFavorite = t.Playlists.Where(p => p.UserPlaylists.Any(up => up.UserId == userId && up.Playlist.Name == "My favorite tracks")).Any()
                        })
            .ToList();
        }

        public async Task<List<UserPlaylist>> GetUserPlaylists(string userId)
        {
            var dbContext = await DbFactory.CreateDbContextAsync();

            return dbContext.UserPlaylists.Include(i => i.Playlist).Where(w => w.UserId == userId).ToList();
        }

        public async void RemoveTrackFromPlaylist(long playlistId, long trackId, bool isFavorite)
        {
            var dbContext = await DbFactory.CreateDbContextAsync();

            if (isFavorite)
            {
                var myfavlist = dbContext.Playlists.SingleOrDefault(a => a.Name == "My favorite tracks");
                playlistId = myfavlist.PlaylistId;
            }

            var trackData = dbContext.Tracks.SingleOrDefault(a => a.TrackId == trackId);
            var playlistData = dbContext.Playlists.Include(t => t.Tracks).SingleOrDefault(a => a.PlaylistId == playlistId);

            playlistData.Tracks.Remove(trackData);

            dbContext.SaveChanges();
        }
    }
}
