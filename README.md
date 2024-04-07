# Chinook

This application is unfinished. Please complete below tasks. Spend max 2 hours.
We would like to have a short written explanation of the changes you made.

1. Move data retrieval methods to separate class / classes (use dependency injection)
2. Favorite / unfavorite tracks. An automatic playlist should be created named "My favorite tracks"
3. Search for artist name

Optional:
4. The user's playlists should be listed in the left navbar. If a playlist is added (or modified), this should reflect in the left navbar (NavMenu.razor). Preferrably, this list should be refreshed without a full page reload. (suggestion: you can use Event, Reactive.NET, SectionOutlet, or any other method you prefer)
5. Add tracks to a playlist (existing or new one). The dialog is already created but not yet finished.

When creating a user account, you will see this:
"This app does not currently have a real email sender registered, see these docs for how to configure a real email sender. Normally this would be emailed: Click here to confirm your account."
After you click 'Click here to confirm your account' you should be able to login.

Please send us a zip file with the modified solution when you are done. You can also upload it to your own GitHub account and send us the link.

# Explnation of Changes
##### Moved the data retrieval to services and accessing it through interface 
```cs
    //IArtistService.cs

    using Chinook.Models;

    namespace Chinook.Interfaces
    {
        public interface IArtistService
        {
            Task<List<Artist>> GetArtists();
            Task<Artist> GetArtistById(long id);
        }
    }
```

```cs
    //IPlaylistService.cs

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
```

```cs
    //ArtistService.cs

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
```

```cs
    //PlaylistService.cs

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

```

```cs
    //Program.cs

    builder.Services.AddTransient<IArtistService, ArtistService>();
    builder.Services.AddTransient<IPlaylistService, PlaylistService>();
```

##### UserPlaylists initialized in Playlist constructor   
```
    //Playlist.cs

    public Playlist()
    {
        Tracks = new HashSet<Track>();
        UserPlaylists = new HashSet<UserPlaylist>();
    }
```

##### Injecting interface to client pages
```
    @inject IArtistService ArtistService
    @inject IPlaylistService PlaylistService
```

##### Class name corrected to bi-star-fill
```html
    <a href="#" class="m-1" title="Unmark as favorite" @onclick="@(() => UnfavoriteTrack(track.TrackId))" @onclick:preventDefault><i class="bi bi-star-fill"></i></a>
```

##### Dynamically loading the user playlist to dropdown
```
    <select class="form-control" id="ExistingPlaylist" @bind="SelectedPlaylistId">
        @if (UserPlaylists != null)
        {
            foreach (var item in UserPlaylists.Where(w => w.Playlist?.Name != "My favorite tracks"))
            {
                <option value="@item.PlaylistId">@item.Playlist?.Name</option>
            }

        }
    </select>
```