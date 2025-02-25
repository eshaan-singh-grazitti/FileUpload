using Dapper;
using FileUpload.Models;
using Microsoft.Data.SqlClient;
using System.Data;

public class DataRepository
{
    private readonly string? _connectionString;

    public DataRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    public async Task<(List<UploadedFileInfo> Files, int TotalCount)> GetPaginatedDataAsync(string userId, bool isUser, int pageNumber, int pageSize)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId); // Pass userId for filtering
                parameters.Add("@IsUser", isUser);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);
                parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var result = await connection.QueryAsync<UploadedFileInfo>(
                    "GetPaginatedData",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                int totalCount = parameters.Get<int>("@TotalCount");

                return (result.AsList(), totalCount);
            }
        }
        catch (SqlException ex)
        {
            // Log SQL exceptions
            Console.WriteLine($"SQL Error: {ex.Message}");
            return (new List<UploadedFileInfo>(), 0); // Return empty list in case of error
        }
        catch (Exception ex)
        {
            // Log any other exceptions
            Console.WriteLine($"Error: {ex.Message}");
            return (new List<UploadedFileInfo>(), 0); // Return empty list in case of error
        }
    }

}
