using Microsoft.EntityFrameworkCore;

namespace Juleplanlaegning.Data;

public class TodoService(IDbContextFactory<AppDbContext> dbFactory)
{
    // ---- Lists ----

    public async Task<List<TodoList>> GetListsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.TodoLists.AsNoTracking().OrderBy(l => l.SortOrder).ThenBy(l => l.Id).ToListAsync();
    }

    public async Task<TodoList> CreateListAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var count = await db.TodoLists.CountAsync();
        var maxOrder = await db.TodoLists.MaxAsync(l => (int?)l.SortOrder) ?? 0;
        var list = new TodoList { Name = $"Opgaver {count + 1}", SortOrder = maxOrder + 1 };
        db.TodoLists.Add(list);
        await db.SaveChangesAsync();
        return list;
    }

    public async Task RenameListAsync(int listId, string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.TodoLists.Where(l => l.Id == listId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.Name, name.Trim()));
    }

    public async Task DeleteListAsync(int listId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var taskIds = db.TodoTasks.Where(t => t.TodoListId == listId).Select(t => t.Id);
        await db.TaskPeople.Where(tp => taskIds.Contains(tp.TodoTaskId)).ExecuteDeleteAsync();
        await db.TodoTasks.Where(t => t.TodoListId == listId).ExecuteDeleteAsync();
        await db.TodoLists.Where(l => l.Id == listId).ExecuteDeleteAsync();
        await tx.CommitAsync();
    }

    // ---- Tasks ----

    // Timed tasks first, in time order; tasks without a time keep the order they were added in.
    public async Task<List<TodoTask>> GetTasksAsync(int listId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var tasks = await db.TodoTasks.AsNoTracking()
            .Where(t => t.TodoListId == listId)
            .Include(t => t.People).ThenInclude(tp => tp.Person)
            .ToListAsync();
        return tasks
            .OrderBy(t => t.Start is null)
            .ThenBy(t => t.Start)
            .ThenBy(t => t.End)
            .ThenBy(t => t.Id)
            .ToList();
    }

    public async Task AddTaskAsync(int listId, string title, TimeOnly? start, TimeOnly? end)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.TodoTasks.Add(new TodoTask { TodoListId = listId, Title = title.Trim(), Start = start, End = end });
        await db.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(int taskId, string title, TimeOnly? start, TimeOnly? end)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.TodoTasks.Where(t => t.Id == taskId).ExecuteUpdateAsync(s => s
            .SetProperty(t => t.Title, title.Trim())
            .SetProperty(t => t.Start, start)
            .SetProperty(t => t.End, end));
    }

    public async Task SetTaskDoneAsync(int taskId, bool done)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.TodoTasks.Where(t => t.Id == taskId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Done, done));
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.TaskPeople.Where(tp => tp.TodoTaskId == taskId).ExecuteDeleteAsync();
        await db.TodoTasks.Where(t => t.Id == taskId).ExecuteDeleteAsync();
    }

    // ---- People on tasks ----

    public async Task AddPersonAsync(int taskId, int personId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.TaskPeople.AnyAsync(tp => tp.TodoTaskId == taskId && tp.PersonId == personId)) return;
        db.TaskPeople.Add(new TaskPerson { TodoTaskId = taskId, PersonId = personId });
        await db.SaveChangesAsync();
    }

    public async Task RemovePersonAsync(int taskId, int personId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.TaskPeople.Where(tp => tp.TodoTaskId == taskId && tp.PersonId == personId).ExecuteDeleteAsync();
    }
}
