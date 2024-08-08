var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

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

peopleApi.MapGet("/", () =>
    people.Select(p => new { id = p.Key, p.Value.FirstName, p.Value.LastName })
);

peopleApi.MapGet("/{id}", (int id) =>
    people.TryGetValue(id, out var person)
        ? TypedResults.Ok(new { id, person.FirstName, person.LastName })
        : Results.Problem(statusCode: 404)
);

peopleApi.MapPost("/", (Person person) =>
    {
        var id = people.Count == 0 ? 0 : people.Keys.Max() + 1;
        people.Add(id, person);

        return TypedResults.Created($"/people/{id}", new { id, person.FirstName, person.LastName });
    })
    .AddEndpointFilterFactory(ValidationHelper.ValidatePersonFactory);

peopleApi.MapPut("/{id}", (int id, Person person) =>
    {
        if (!people.ContainsKey(id))
        {
            return Results.Problem(statusCode: 404);
        }

        people[id] = person;

        return TypedResults.NoContent();
    })
    .AddEndpointFilterFactory(ValidationHelper.ValidatePersonFactory);

peopleApi.MapDelete("/{id}", (int id) =>
    people.Remove(id)
        ? TypedResults.NoContent()
        : Results.Problem(statusCode: 404)
);

app.Run();

public record Person(string FirstName, string LastName);

class ValidationHelper
{
    internal static EndpointFilterDelegate ValidatePersonFactory(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next
    )
    {
        var parameters = context.MethodInfo.GetParameters();
        int? personPosition = null;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(Person))
            {
                personPosition = i;
                break;
            }
        }

        if (!personPosition.HasValue)
        {
            throw new InvalidOperationException();
        }

        return async (invocationContext) =>
        {
            var person = invocationContext.GetArgument<Person>(personPosition.Value);

            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(person.FirstName))
            {
                errors.Add("firstName", ["Invalid format: Null or empty"]);
            }
            if (string.IsNullOrWhiteSpace(person.LastName))
            {
                errors.Add("lastName", ["Invalid format: Null or empty"]);
            }

            return errors.Count == 0 ? await next(invocationContext) : Results.ValidationProblem(errors);
        };
    }
}