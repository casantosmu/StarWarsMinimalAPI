using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteOptions>(o =>
{
    o.LowercaseUrls = true;
    o.AppendTrailingSlash = false;
    o.LowercaseQueryStrings = false;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

var people = new Dictionary<int, Person>
{
    { 0, new("Anakin", "Skywalker") },
    { 1, new("Obi-Wan", "Kenobi") },
    { 2, new("Padmé", "Amidala") }
};

var peopleApi = app.MapGroup("/people");

peopleApi
    .MapGet("/", () => people.Select(p => new { id = p.Key, p.Value.FirstName, p.Value.LastName }))
    .WithName("getPeople");

peopleApi
    .MapGet("/{id}", (int id) =>
        people.TryGetValue(id, out var person)
            ? TypedResults.Ok(new { id, person.FirstName, person.LastName })
            : Results.Problem(statusCode: 404)
    )
    .WithName("getPerson");

peopleApi
    .MapPost("/", (Person person, LinkGenerator links) =>
    {
        var id = people.Count == 0 ? 0 : people.Keys.Max() + 1;
        people.Add(id, person);

        var link = links.GetPathByName("getPerson", new { id });

        return TypedResults.Created(link, new { id, person.FirstName, person.LastName });
    })
    .WithParameterValidation()
    .WithName("addPerson");

peopleApi
    .MapPut("/{id}", (int id, Person person) =>
    {
        if (!people.ContainsKey(id))
        {
            return Results.Problem(statusCode: 404);
        }

        people[id] = person;

        return TypedResults.NoContent();
    })
    .WithParameterValidation()
    .WithName("updatePerson");

peopleApi
    .MapDelete("/{id}", (int id) =>
        people.Remove(id)
            ? TypedResults.NoContent()
            : Results.Problem(statusCode: 404)
    )
    .WithName("removePerson");

app.Run();

public record Person(
    [Required]
    [Display(Name = "First name")]
    string FirstName,

    [Required]
    [Display(Name = "Last name")]
    string LastName
);