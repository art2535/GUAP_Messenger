using Messenger.API.Extensions;

namespace Messenger.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /*if (builder.Environment.IsDevelopment())
            {
                JwtExtension.SetTheEnvironmentVariable(forMachine: false);
            }*/

            builder.Services.AddControllers();

            builder.Services.AddSwagger();
            builder.Services.AddPostgreSQL(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddServices();
            builder.Services.AddJwtService(builder.Configuration);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerInterface();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
