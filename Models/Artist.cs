namespace SPOTIFY_APP.Models
{

    public class SpotifyTokenResponse
    {
        public string access_token { get; set; }
    }


    public class Item
    {
        public string id { get; set; }
        public string name { get; set; }
        public Owner owner { get; set; }
        public Tracks tracks { get; set; }
        public Track track { get; set; }
        public string description { get; set; }
        public string release_date { get; set; }
        public string release_date_precision { get; set; }
    }


    public class Owner
    {
        public string display_name { get; set; }
    }

    public class Root
    {
        public List<Item> items { get; set; }
    }

    public class Tracks
    {
        public int total { get; set; }
    }


    public class Playlist
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tracks { get; set; }
    }

    public class ArtistTopTracks
    {
        public List<Track> tracks { get; set; }
    }


    public class Artist
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Album
    {
        public string name { get; set; }

    }

    public class Track
    {

        public Album album { get; set; }
        public List<Artist> artists { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public int popularity { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string AlbumName { get; set; }
        public string ArtistId { get; set; }
    }

    public class Followers
    {
        public int total { get; set; }
    }


    public class ArtistData
    {
        public Followers followers { get; set; }
        public int popularity { get; set; }
        public string name { get; set; }

    }

    public class ArtistAlbums
    {

        public List<Item> items { get; set; }
    }


}


