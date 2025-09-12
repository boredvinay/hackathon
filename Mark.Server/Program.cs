using Persistence;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mark API", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, "Mark.Server.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml, includeControllerXmlComments: true);
});

// Modules required right now
builder.Services.AddPersistence(builder.Configuration);
// (we’ll add Jobs module immediately after Persistence is up)
//builder.Services.AddJobsModule();

var app = builder.Build();

// Apply EF migrations automatically (dev only)
if (app.Environment.IsDevelopment())
{
    app.Services.MigrateDb();
    app.UseSwagger();
    app.UseSwaggerUI(s => s.SwaggerEndpoint("/swagger/v1/swagger.json", "Mark API v1"));
}

app.UseRouting();

app.MapControllers();
app.MapJobsEndpoints();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();