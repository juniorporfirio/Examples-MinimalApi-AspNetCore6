using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("StarDb") ?? "Data Source=stardb";

builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<StarDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/error");
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/projects", async (StarDbContext context) =>
{
	return await context.Projects.ToListAsync();
});

app.MapGet("/projects/stars", async (StarDbContext context) =>
{
	return await context.Projects.Where(s => s.Star).ToListAsync();
});

app.MapGet("/projects/{id:int}", async (StarDbContext context, int id) =>
{
	return await context.Projects.FindAsync(id) is Project project ?
	Results.Ok(project) : Results.NotFound();
});

app.MapPost("/projects", async (StarDbContext context, Project project) =>
{
	context.Add(project);
	await context.SaveChangesAsync();
	return Results.Created($"projects/{project.Id}", project);
});

app.MapPut("/projects/{id:int}", async (StarDbContext context, int Id, Project inputProject) =>
{
	var project = await context.Projects.FindAsync(Id);

	if (project is null) return Results.NotFound();

	project.Name = inputProject.Name;
	project.Url = inputProject.Url;
	project.Star = inputProject.Star;

	await context.SaveChangesAsync();

	return Results.NoContent();
});

app.MapPut("/projects/{id:int}/give-star", async (StarDbContext context, int Id, Project inputProject) =>
{
	var project = await context.Projects.FindAsync(Id);

	if (project is null) return Results.NotFound();

	project.Star = true;

	await context.SaveChangesAsync();

	return Results.NoContent();
});

app.MapPut("/projects/{id:int}/remove-star", async (StarDbContext context, int Id, Project inputProject) =>
{
	var project = await context.Projects.FindAsync(Id);

	if (project is null) return Results.NotFound();

	project.Star = false;

	await context.SaveChangesAsync();

	return Results.NoContent();
});

app.Run();
class Project
{

	public int Id { get; set; }
	[Required]
	public string? Name { get; set; }
	public string? Url { get; set; }
	public bool Star { get; set; }
}
class StarDbContext : DbContext
{
	public StarDbContext(DbContextOptions<StarDbContext> options) : base(options)
	{ }
	public DbSet<Project> Projects => Set<Project>();
}



