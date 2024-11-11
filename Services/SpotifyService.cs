using SPOTIFY_APP.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Newtonsoft.Json;

namespace SPOTIFY_APP.Services
{

    public class SpotifyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SpotifyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public string GetAuthorizationUrl()
        {
            var clientId = _configuration["Spotify:ClientId"];

            bool localDevelopment = _configuration.GetValue<bool>("Spotify:LocalDevelopment");

            string redirectUri = GetRedirectURI(localDevelopment);

            var scopes = _configuration["Spotify:Scopes"];
            return $"{_configuration["Spotify:AuthorizationEndpoint"]}?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopes)}";
        }

        public async Task<string> GetAccessTokenAsync(string code)
        {
            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];
            bool localDevelopment = _configuration.GetValue<bool>("Spotify:LocalDevelopment");

            string redirectUri = GetRedirectURI(localDevelopment);

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.PostAsync(_configuration["Spotify:TokenEndpoint"], requestBody);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get access token from Spotify");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<SpotifyTokenResponse>(json);

            return tokenResponse.access_token;
        }

        private string GetRedirectURI(bool localDevelopment)
        {
            if (localDevelopment)
                return _configuration["Spotify:RedirectUriLocal"];
            else
                return _configuration["Spotify:RedirectUri"];
        }

        public async Task<List<Playlist>> GetUserPlaylistsAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var playlistURl = _configuration["Spotify:PlaylistURL"];
            var response = await _httpClient.GetAsync(playlistURl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch playlists");
            }

            var json = await response.Content.ReadAsStringAsync();
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(json);

            var playlists = new List<Playlist>();
            foreach (var item in myDeserializedClass.items)
            {
                playlists.Add(new Playlist
                {
                    Id = item.id,
                    Name = item.name,
                    Description = item.description,
                    Tracks = item.tracks.total
                });
            }
            return playlists;
        }



        public async Task<string> GetUserProfileAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            // Get the current user's Spotify ID
            var userResponse = await _httpClient.GetAsync("https://api.spotify.com/v1/me");
            if (!userResponse.IsSuccessStatusCode)
                throw new Exception("Failed to retrieve user information.");

            var json = await userResponse.Content.ReadAsStringAsync();
            var res = System.Text.Json.JsonSerializer.Deserialize<Item>(json);
            return res.id;


        }

        public async Task<string> GetDisplayNameAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get the current user's Spotify ID
            var userResponse = await _httpClient.GetAsync("https://api.spotify.com/v1/me");
            if (!userResponse.IsSuccessStatusCode)
                throw new Exception("Failed to retrieve user information.");

            var userJson = await userResponse.Content.ReadAsStringAsync();
            return JsonDocument.Parse(userJson).RootElement.GetProperty("display_name").GetString();


        }



        public async Task<List<Track>> GetPlaylistTracksAsync(string accessToken, string playlistId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to fetch playlist tracks");

            var json = await response.Content.ReadAsStringAsync();
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(json);

            var tracks = new List<Track>();
            foreach (var item in myDeserializedClass.items)
            {
                var track = item.track;
                tracks.Add(new Track
                {
                    Id = track.id,
                    Name = track.name,
                    Artist = track.artists[0].name,
                    AlbumName = track.album.name,
                    ArtistId = track.artists[0].id
                });
            }
            return tracks;
        }

        public async Task<List<Track>> GetArtistTopTracksAsync(string accessToken, string artistId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/artists/{artistId}/top-tracks?market=US");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var topTracksData = System.Text.Json.JsonSerializer.Deserialize<ArtistTopTracks>(json);
            return topTracksData.tracks;
        }


        public async Task<ArtistData> GetArtistDetailsAsync(string accessToken, string artistId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var artistResponse = await _httpClient.GetAsync($"https://api.spotify.com/v1/artists/{artistId}");
            artistResponse.EnsureSuccessStatusCode();
            var json = await artistResponse.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<ArtistData>(json);
        }

        public async Task<Dictionary<int, int>> GetAlbumsByYearAsync(string artistId)
        {
            var url = $"https://api.spotify.com/v1/artists/{artistId}/albums?limit=50";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to fetch artist albums");

            var jsonResponse = await response.Content.ReadAsStringAsync();


            ArtistAlbums myDeserializedClass = JsonConvert.DeserializeObject<ArtistAlbums>(jsonResponse);


            // Count albums by year
            var albumReleaseCounts = new Dictionary<int, int>();

            foreach (var album in myDeserializedClass.items)
            {
                if (album.release_date == null) continue;

                int releaseYear;
                if (album.release_date_precision == "year")
                {
                    releaseYear = int.Parse(album.release_date);
                }
                else
                {
                    var releaseDate = DateTime.Parse(album.release_date);
                    releaseYear = releaseDate.Year;
                }

                if (albumReleaseCounts.ContainsKey(releaseYear))
                    albumReleaseCounts[releaseYear]++;
                else
                    albumReleaseCounts[releaseYear] = 1;
            }

            return albumReleaseCounts.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value); // Sort by year
        }

        public async Task UpdatePlaylistAsync(string accessToken, string playlistId, string name, string description)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var requestContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
            {
                name = name,
                description = description
            }), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"https://api.spotify.com/v1/playlists/{playlistId}", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to update playlist");
            }
        }
        public async Task CreatePlaylistAsync(string accessToken, string name, string description, bool isPublic)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userId = GetUserProfileAsync(accessToken);

            // Create the playlist for the user
            var requestBody = new
            {
                name = name,
                description = description,
                @public = isPublic
            };

            var requestContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var createResponse = await _httpClient.PostAsync($"https://api.spotify.com/v1/users/{userId.Result}/playlists", requestContent);
            if (!createResponse.IsSuccessStatusCode)
            {
                throw new Exception("Failed to create playlist.");
            }
        }

    }
}
