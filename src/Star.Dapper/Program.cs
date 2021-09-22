using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("StarDb") ?? "Data Source=stardb";
builder.Services.AddScoped(_ => new SqliteConnection(connectionString));

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();

await EnsureDatabase(app.Services, app.Logger);

if (app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/error");
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/projects", async (SqliteConnection context) =>
{
	return await context.QueryAsync<Project>("SELECT * FROM Projects");
});

app.MapGet("/projects/stars", async (SqliteConnection context) =>
{
	return await context.QueryAsync<Project>("SELECT * FROM Projects WHERE Star = true");
});

app.MapGet("/projects/{id:int}", async (SqliteConnection context, int id) =>
{
	return await context.QuerySingleAsync<Project>("SELECT * FROM Project WHERE Id=@id", new { id }) is Project project ?
	Results.Ok(project) : Results.NotFound();
});

app.MapPost("/projects", async (SqliteConnection context, Project project) =>
{
	var newProject = await context.QuerySingleAsync<Project>("INSERT INTO Project(Id,Name, Url, Star) VALUES (@Id, @Name, @Url, @Star) RETURNING * ", project);
	return Results.Created($"projects/{newProject.Id}", newProject);
});

app.MapPut("/projects/{id:int}", async (SqliteConnection context, int Id, Project inputProject) =>
{
	return await context.ExecuteAsync("UPDATE Project SET Name=@Name, Url=@Url, Star=@Star WHERE Id = @Id", inputProject) == 1 ? Results.NoContent() : Results.NotFound();
});

app.MapPut("/projects/{id:int}/give-star", async (SqliteConnection context, int Id, Project inputProject) =>
{
	return await context.ExecuteAsync("UPDATE Project SET Star=true WHERE Id = @Id", inputProject) == 1 ? Results.NoContent() : Results.NotFound();

});

app.MapPut("/projects/{id:int}/remove-star", async (SqliteConnection context, int Id, Project inputProject) =>
{
	return await context.ExecuteAsync("UPDATE Project SET Star=false WHERE Id = @Id", inputProject) == 1 ? Results.NoContent() : Results.NotFound();
});

app.Run();

async Task EnsureDatabase(IServiceProvider services, ILogger logger)
{
	logger.LogInformation("Ensuring database exists at connection string '{connectionString}'", connectionString);

	using var db = services.CreateScope().ServiceProvider.GetRequiredService<SqliteConnection>();

	var sql = $@"CREATE TABLE IF NOT EXISTS Projects (
                  {nameof(Project.Id)} INTEGER PRIMARY KEY NOT NULL,
                  {nameof(Project.Name)} TEXT NOT NULL,
                  {nameof(Project.Url)} TEXT NOT NULL,
                  {nameof(Project.Star)} INTEGER DEFAULT 0 NOT NULL CHECK({nameof(Project.Star)} IN (0, 1))
                 );";

	await db.ExecuteAsync(sql);
}
class Project
{

	public int Id { get; set; }
	[Required]
	public string? Name { get; set; }
	public string? Url { get; set; }
	public bool Star { get; set; }
}
