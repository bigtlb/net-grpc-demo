using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.Sqlite;

namespace GrpcDemo.Services;

public class ToDoService: ToDoList.ToDoListBase
{
    public ToDoService()
    {
        using (var connection = new SqliteConnection("Data Source=todos.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS Todos (Id UUID PRIMARY KEY, Name TEXT, Description TEXT, Complete BOOLEAN)";
            command.ExecuteNonQuery();
        }
    }
    public override async Task<ToDoItem> InsertTodo(ToDoRequest request, ServerCallContext context)
    {
        using (var connection = new SqliteConnection("Data Source=todos.db"))
        {
            var id = Guid.NewGuid();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Todos (Id, Name, Description, Complete) VALUES (@Id, @Name, @Description, @Complete); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", request.Name);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.Parameters.AddWithValue("@Complete", request.Complete);
            command.ExecuteNonQuery();
            return new ToDoItem
            {
                Id = $"{id}",
                Name = request.Name,
                Description = request.Description,
                Complete = request.Complete
            };
        }
    }

    public override async Task<ToDoAcknowledgement> UpdateTodo(ToDoItem request, ServerCallContext context)
    {
        // Update the todo item in the database
        using (var connection = new SqliteConnection("Data Source=todos.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Todos SET Name = @Name, Description = @Description, Complete = @Complete WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", request.Id);
            command.Parameters.AddWithValue("@Name", request.Name);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.Parameters.AddWithValue("@Complete", request.Complete);
            return new ToDoAcknowledgement { Result = command.ExecuteNonQuery() == 1 };
        }
    }

    public override async Task<ToDoAcknowledgement> DeleteTodo(ToDoId request, ServerCallContext context)
    {
        // Delete the todo item from the database
        using (var connection = new SqliteConnection("Data Source=todos.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Todos WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", request.Id);
            return new ToDoAcknowledgement { Result = command.ExecuteNonQuery() == 1 };
        }
    }

    public override async Task<ToDoListItems> ListToDos(Empty request, ServerCallContext context)
    {
        // List all the todo items from the database
        using (var connection = new SqliteConnection("Data Source=todos.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Description, Complete FROM Todos";
            var reader = command.ExecuteReader();
            var items = new ToDoListItems();
            while (reader.Read())
            {
                items.Todos.Add(new ToDoItem
                {
                    Id = (string) reader["Id"],
                    Name = (string) reader["Name"],
                    Description = (string)reader["Description"],
                    Complete = (long)reader["Complete"] == 1
                });
            }
            return items;
        }
    }
}