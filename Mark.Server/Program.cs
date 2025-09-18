using System.Reflection;
using SharedModule;
using JobsModule;
using DesignModule;

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

// Register shared infra FIRST
builder.Services.AddSharedModule();

builder.Services.AddJobsModule();
builder.Services.AddDesignModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Mark API v1"));
}

app.UseRouting();

// MVC controllers
app.MapControllers();


app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();