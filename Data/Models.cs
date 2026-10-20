namespace Juleplanlaegning.Data;

// People are shared across all plans; where they are placed is stored per plan in Assignment.
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// One version of the map layout, e.g. one day. Each plan has its own buckets and placements.
public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    public List<Bucket> Buckets { get; set; } = [];
}

public class Bucket
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }

    public string Name { get; set; } = "";
    public int Capacity { get; set; }
    public string Color { get; set; } = "#2f6fdf";

    // Position and size in percent of the map, so buckets scale with the map.
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

// A person placed in a bucket within a plan. At most one per person per plan.
public class Assignment
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public int PersonId { get; set; }
    public int BucketId { get; set; }

    public Person? Person { get; set; }
    public Bucket? Bucket { get; set; }
}

// Single-row table holding the uploaded background image, shared by all plans.
public class MapImage
{
    public int Id { get; set; }
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "";
    public DateTime UpdatedUtc { get; set; }
}

// A named to-do list, e.g. one day's chores. Shown as a tab on the to-do page.
public class TodoList
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    public List<TodoTask> Tasks { get; set; } = [];
}

public class TodoTask
{
    public int Id { get; set; }
    public int TodoListId { get; set; }

    public string Title { get; set; } = "";
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public bool Done { get; set; }

    public List<TaskPerson> People { get; set; } = [];
}

// A person put on a task. A person can be on several tasks.
public class TaskPerson
{
    public int TodoTaskId { get; set; }
    public int PersonId { get; set; }

    public Person? Person { get; set; }
}
