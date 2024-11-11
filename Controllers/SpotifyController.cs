using SPOTIFY_APP.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace SPOTIFY_APP.Controllers
{
    public class SpotifyController : Controller
    {

        private readonly SpotifyService _spotifyService;
        private readonly IConfiguration _configuration;

        public SpotifyController(SpotifyService spotifyService, IConfiguration configuration)
        {
            _spotifyService = spotifyService;
            _configuration = configuration;
        }

        // Step 1: Redirect user to Spotify's authorization endpoint
        [HttpGet]
        public IActionResult Login(string username, string password)
        {
            //HttpContext.Session.Clear();
            var authorizationUrl = _spotifyService.GetAuthorizationUrl();
            Debug.WriteLine("authorizationUrl : " + authorizationUrl);
            return Redirect(authorizationUrl);
        }

        // Step 2: Spotify redirects back with an authorization code
        [HttpGet]
        public async Task<IActionResult> Callback(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code missing.");
            }

            // Step 3: Exchange the authorization code for an access token
            var accessToken = await _spotifyService.GetAccessTokenAsync(code);

            Debug.WriteLine("accessToken : " + accessToken);

            // Store the access token for later use (in a session, for example)
            HttpContext.Session.SetString("SpotifyAccessToken", accessToken);

            return RedirectToAction("Index"); // Redirect to the main page or wherever needed
        }

        // Example method to list playlists (using the stored access token)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accessToken = HttpContext.Session.GetString("SpotifyAccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToAction("Login");
            }

            var playlists = await _spotifyService.GetUserPlaylistsAsync(accessToken);

            var accountHolder = HttpContext.Session.GetString("AccountHolder");
            if (!string.IsNullOrEmpty(accountHolder))
            {
                ViewData["AccountHolder"] = accountHolder;
            }
            else
            {
                accountHolder = await _spotifyService.GetDisplayNameAsync(accessToken);
                HttpContext.Session.SetString("AccountHolder", accountHolder);
                ViewData["AccountHolder"] = accountHolder;
            }
            return View(playlists);
        }


        [HttpGet]
        public async Task<IActionResult> PlaylistTracks(string id)
        {
            var accessToken = HttpContext.Session.GetString("SpotifyAccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToAction("Login");
            }

            var tracks = await _spotifyService.GetPlaylistTracksAsync(accessToken, id);
            ViewData["PlaylistId"] = id;
            return View(tracks);
        }

public IActionResult Charts()
        {
            ViewData["HideFooter"] = true;
            return View();
        }

        public async Task<IActionResult> ArtistTopTracks(string artistId)
        {
            var accessToken = HttpContext.Session.GetString("SpotifyAccessToken");
            if (string.IsNullOrEmpty(accessToken))
                return RedirectToAction("Login");

            var topTracks = await _spotifyService.GetArtistTopTracksAsync(accessToken, artistId);
            var artistDetails = await _spotifyService.GetArtistDetailsAsync(accessToken, artistId);
            var albumCounts = await _spotifyService.GetAlbumsByYearAsync(artistId);

            // Extract track names and popularity data
            var topTracksData = topTracks.Select(t => t.popularity).ToList();
            var topTracksLabels = topTracks.Select(t => t.name).ToList();

            // Pass the data to the view using ViewData or a view model
            ViewData["TopTracksData"] = topTracksData;
            ViewData["TopTracksLabels"] = topTracksLabels;
            ViewData["ArtistName"] = artistDetails.name;
            ViewData["Popularity"] = artistDetails.popularity;
            ViewData["Followers"] = artistDetails.followers.total;
            ViewData["HideFooter"] = true;

            ViewData["Years"] = albumCounts.Keys.ToArray();
            ViewData["AlbumCounts"] = albumCounts.Values.ToArray();


            return View("ArtistTopTracks", topTracks);
        }



    }
}