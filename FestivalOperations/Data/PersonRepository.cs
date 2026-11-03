using Microsoft.Data.SqlClient;

namespace FestivalOperations.Data;

public sealed record PersonSummary(int PersonID, string Name, string Role);

public sealed class PersonRepository
{
    private readonly SqlConnection _connection;

    public PersonRepository(SqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<PersonSummary>> GetAllPeopleAsync()
    {
        const string sql = """
            SELECT PersonID, Name, Role
            FROM dbo.People
            ORDER BY PersonID;
            """;
        
        var people = new List<PersonSummary>();

        await using var command = new SqlCommand(sql, _connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            people.Add(new PersonSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return people;
    }
}