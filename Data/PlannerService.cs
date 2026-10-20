using Microsoft.EntityFrameworkCore;

namespace Juleplanlaegning.Data;

public class PlannerService(IDbContextFactory<AppDbContext> dbFactory)
{
    private static readonly string[] Palette =
        ["#2f6fdf", "#1f9d6b", "#c2410c", "#7c3aed", "#0e7490", "#b45309", "#be185d", "#4d7c0f"];

    // ---- Plans (versions) ----

    public async Task<List<Plan>> GetPlansAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var plans = await db.Plans.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        if (plans.Count > 0) return plans;

        // Always have at least one plan to work in.
        var first = new Plan { Name = "Day 1" };
        db.Plans.Add(first);
        await db.SaveChangesAsync();
        return [first];
    }

    // Creates a new plan as a copy of an existing one: same buckets and same placements.
    public async Task<Plan> CreatePlanFromAsync(int sourcePlanId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var planCount = await db.Plans.CountAsync();
        var maxOrder = await db.Plans.MaxAsync(p => (int?)p.SortOrder) ?? 0;
        var plan = new Plan { Name = $"Day {planCount + 1}", SortOrder = maxOrder + 1 };
        db.Plans.Add(plan);
        await db.SaveChangesAsync();

        var sourceBuckets = await db.Buckets.AsNoTracking().Where(b => b.PlanId == sourcePlanId).ToListAsync();
        var bucketCopies = sourceBuckets.ToDictionary(b => b.Id, b => new Bucket
        {
            PlanId = plan.Id,
            Name = b.Name,
            Capacity = b.Capacity,
            Color = b.Color,
            X = b.X, Y = b.Y, Width = b.Width, Height = b.Height,
        });
        db.Buckets.AddRange(bucketCopies.Values);
        await db.SaveChangesAsync();

        var sourceAssignments = await db.Assignments.AsNoTracking().Where(a => a.PlanId == sourcePlanId).ToListAsync();
        db.Assignments.AddRange(sourceAssignments.Select(a => new Assignment
        {
            PlanId = plan.Id,
            PersonId = a.PersonId,
            BucketId = bucketCopies[a.BucketId].Id,
        }));
        await db.SaveChangesAsync();

        await tx.CommitAsync();
        return plan;
    }

    public async Task RenamePlanAsync(int planId, string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Plans.Where(p => p.Id == planId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Name, name.Trim()));
    }

    public async Task DeletePlanAsync(int planId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Assignments.Where(a => a.PlanId == planId).ExecuteDeleteAsync();
        await db.Buckets.Where(b => b.PlanId == planId).ExecuteDeleteAsync();
        await db.Plans.Where(p => p.Id == planId).ExecuteDeleteAsync();
        await tx.CommitAsync();
    }

    // ---- People ----

    public async Task<List<Person>> GetPeopleAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.People.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }

    public async Task AddPersonAsync(string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.People.Add(new Person { Name = name.Trim() });
        await db.SaveChangesAsync();
    }

    public async Task DeletePersonAsync(int personId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Assignments.Where(a => a.PersonId == personId).ExecuteDeleteAsync();
        await db.TaskPeople.Where(tp => tp.PersonId == personId).ExecuteDeleteAsync();
        await db.People.Where(p => p.Id == personId).ExecuteDeleteAsync();
    }

    // ---- Placements ----

    // Maps person id to bucket id for everyone placed in the plan.
    public async Task<Dictionary<int, int>> GetAssignmentsAsync(int planId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Assignments.AsNoTracking()
            .Where(a => a.PlanId == planId)
            .ToDictionaryAsync(a => a.PersonId, a => a.BucketId);
    }

    // Capacity is intentionally not enforced here: overfilling is allowed and only flagged in the UI.
    public async Task AssignAsync(int planId, int personId, int? bucketId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Assignments.Where(a => a.PlanId == planId && a.PersonId == personId).ExecuteDeleteAsync();
        if (bucketId is { } id)
        {
            db.Assignments.Add(new Assignment { PlanId = planId, PersonId = personId, BucketId = id });
            await db.SaveChangesAsync();
        }
    }

    // ---- Buckets ----

    public async Task<List<Bucket>> GetBucketsAsync(int planId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Buckets.AsNoTracking().Where(b => b.PlanId == planId).OrderBy(b => b.Id).ToListAsync();
    }

    public async Task<Bucket> CreateBucketAsync(int planId, double x, double y, double width, double height)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var count = await db.Buckets.CountAsync(b => b.PlanId == planId);
        var bucket = new Bucket
        {
            PlanId = planId,
            Name = $"Bucket {count + 1}",
            Capacity = 4,
            Color = Palette[count % Palette.Length],
            X = x, Y = y, Width = width, Height = height,
        };
        db.Buckets.Add(bucket);
        await db.SaveChangesAsync();
        return bucket;
    }

    public async Task UpdateBucketRectAsync(int bucketId, double x, double y, double width, double height)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Buckets.Where(b => b.Id == bucketId).ExecuteUpdateAsync(s => s
            .SetProperty(b => b.X, x)
            .SetProperty(b => b.Y, y)
            .SetProperty(b => b.Width, width)
            .SetProperty(b => b.Height, height));
    }

    public async Task UpdateBucketDetailsAsync(int bucketId, string name, int capacity, string color)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Buckets.Where(b => b.Id == bucketId).ExecuteUpdateAsync(s => s
            .SetProperty(b => b.Name, name.Trim())
            .SetProperty(b => b.Capacity, Math.Max(0, capacity))
            .SetProperty(b => b.Color, color));
    }

    public async Task DeleteBucketAsync(int bucketId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // People in the bucket go back to the unplaced list.
        await db.Assignments.Where(a => a.BucketId == bucketId).ExecuteDeleteAsync();
        await db.Buckets.Where(b => b.Id == bucketId).ExecuteDeleteAsync();
    }

    // ---- Map image ----

    public async Task<DateTime?> GetMapImageVersionAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.MapImages.Select(m => (DateTime?)m.UpdatedUtc).FirstOrDefaultAsync();
    }

    public async Task<MapImage?> GetMapImageAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.MapImages.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task SetMapImageAsync(byte[] data, string contentType)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.MapImages.ExecuteDeleteAsync();
        db.MapImages.Add(new MapImage { Data = data, ContentType = contentType, UpdatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public async Task ClearMapImageAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.MapImages.ExecuteDeleteAsync();
    }
}
