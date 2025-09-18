using System.Reflection;
using SharedModule;
using JobsModule;
using DesignModule;
using RenderModule;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mark API", Version = "v1" });
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", p =>
        p.WithOrigins("http://localhost:5009", "https://localhost:58450")
            .AllowAnyMethod()                  // GET/POST/PUT/DELETE/OPTIONS
            .AllowAnyHeader()                  // Content-Type, If-Match, etc.
            .WithExposedHeaders("ETag")        // if you return ETag
            .AllowCredentials());              // only if you use cookies (optional)
});

// Register shared infra FIRST
builder.Services.AddSharedModule();

builder.Services.AddJobsModule();
builder.Services.AddDesignModule();
builder.Services.AddRenderModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Mark API v1"));
}

app.UseRouting();
app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();

// MVC controllers
app.MapControllers();


app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();