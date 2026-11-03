using System.Data;
using Microsoft.Data.SqlClient;

namespace FestivalOperations.Data;

public sealed record PersonSummary(
    int PersonID,
    string Name,
    string Role
);

public sealed record PersonRoleSummary(
    int PersonID,
    string Name,
    string Telephone,
    string Email,
    string Role,
    string RoleDetails
);

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

    public async Task<IReadOnlyList<PersonRoleSummary>> GetPeopleByRoleAsync(
        string role)
    {
        const string sql = """
            SELECT
                p.PersonID,
                p.Name,
                p.Telephone,
                p.Email,
                p.Role,
                CASE p.Role
                    WHEN N'Performer' THEN CONCAT(
                        N'Fee: ',
                        CONVERT(nvarchar(30), performer.Fee),
                        N'; Genres: ',
                        COALESCE(
                            (
                                SELECT STRING_AGG(g.GenreName, N', ')
                                FROM dbo.PerformerGenres AS pg
                                INNER JOIN dbo.Genres AS g
                                    ON g.GenreID = pg.GenreID
                                WHERE pg.PersonID = p.PersonID
                            ),
                            N'None'
                        )
                    )
                    WHEN N'Crew' THEN CONCAT(
                        N'Hourly rate: ',
                        CONVERT(nvarchar(30), crew.HourlyRate),
                        N'; Employment: ',
                        crew.EmploymentType,
                        N'; Weekly hours: ',
                        crew.WeeklyHours
                    )
                    WHEN N'Vendor' THEN CONCAT(
                        N'Categories: ',
                        COALESCE(
                            (
                                SELECT STRING_AGG(category.CategoryName, N', ')
                                FROM dbo.VendorCategories AS vc
                                INNER JOIN dbo.ProductCategories AS category
                                    ON category.CategoryID = vc.CategoryID
                                WHERE vc.PersonID = p.PersonID
                            ),
                            N'None'
                        )
                    )
                END AS RoleDetails
            FROM dbo.People AS p
            LEFT JOIN dbo.Performers AS performer
                ON performer.PersonID = p.PersonID
            LEFT JOIN dbo.Crew AS crew
                ON crew.PersonID = p.PersonID
            WHERE p.Role = @Role
            ORDER BY p.PersonID;
            """;

        var people = new List<PersonRoleSummary>();

        await using var command = new SqlCommand(sql, _connection);
        command.Parameters
            .Add("@Role", SqlDbType.NVarChar, 20).Value = role;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            people.Add(new PersonRoleSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)
            ));
        }

        return people;
    }

    public async Task<int> AddCrewAsync(
        string name,
        string telephone,
        string email,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours)
    {
        using SqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            const string insertPersonSql = """
                INSERT INTO dbo.People (Name, Telephone, Email, Role)
                OUTPUT INSERTED.PersonID
                VALUES (@Name, @Telephone, @Email, N'Crew');
                """;
            
            int personID;

            await using (var personCommand = new SqlCommand(insertPersonSql, _connection, transaction))
            {
                personCommand.Parameters
                    .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
                personCommand.Parameters
                    .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
                personCommand.Parameters
                    .Add("@Email", SqlDbType.NVarChar, 254).Value = email;
                
                personID = (int)(await personCommand.ExecuteScalarAsync())!;
            }

            const string insertCrewSql = """
                INSERT INTO dbo.Crew
                    (PersonID, HourlyRate, EmploymentType, WeeklyHours)
                VALUES
                    (@PersonID, HourlyRate, @EmploymentType, @WeeklyHours);
                """;
            
            await using (var crewCommand = 
                new SqlCommand(insertCrewSql, _connection, transaction))
            {
                crewCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                
                var rateParameter = crewCommand.Parameters.Add("@HourlyRate", SqlDbType.Decimal);
                rateParameter.Precision = 8;
                rateParameter.Scale = 2;
                rateParameter.Value = hourlyRate;

                crewCommand.Parameters
                    .Add("@EmploymentType", SqlDbType.NVarChar, 10).Value = employmentType;
                crewCommand.Parameters
                    .Add("@WeeklyHours", SqlDbType.Int).Value = weeklyHours;
                
                await crewCommand.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            return personID;
        }

        catch
        {
            try
            {
                transaction.Rollback();
            }

            catch
            {
                
            }

            throw;
        }
    }
}
