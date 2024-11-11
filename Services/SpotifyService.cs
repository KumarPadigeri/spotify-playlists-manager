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


    }
}
