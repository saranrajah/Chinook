using Chinook.ClientModels;

namespace Chinook.Interfaces
{
    public interface IPlaylistService
    {
        Task<List<PlaylistTrack>> GetTracksByArtist(long artistId, string userId);
        Task<Models.Playlist> AddTrackToPlaylist(Models.Playlist playlist, long trackId, string userId, bool isFavorite);
        void RemoveTrackFromPlaylist(long playlistId, long trackId, bool isFavorite);
        Task<List<Models.UserPlaylist>> GetUserPlaylists(string userId);
        Task<ClientModels.Playlist> GetPlaylistById(long playlistId, string userId);
    }
}
