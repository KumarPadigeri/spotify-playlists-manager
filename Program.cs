namespace SPOTIFY_APP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession(); // Add session services

            // Add HttpClient for SpotifyService
            builder.Services.AddHttpClient<Services.SpotifyService>();


            // Configure settings from appsettings.json
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();





            app.UseSession(); // Enable session middleware


            // Add route for SpotifyController actions


            app.MapControllerRoute(
name: "spotify",
pattern: "{controller=Spotify}/{action=Index}/{id?}");


            app.Run();

        }
    }
}
