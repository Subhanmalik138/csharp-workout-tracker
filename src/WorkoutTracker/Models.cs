namespace WorkoutTracker;

/// <summary>An exercise a user can log sets against, e.g. "Bench Press".</summary>
public sealed class Exercise
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string MuscleGroup { get; set; } = "";
    /// <summary>Unit for the load column: "kg" for strength work, "min" for cardio.</summary>
    public string Unit { get; set; } = "kg";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One performed set: reps at a given load for one exercise.</summary>
public sealed class SetEntry
{
    public string ExerciseId { get; set; } = "";
    public int Reps { get; set; }
    public double WeightKg { get; set; }
}

/// <summary>One training day: a dated collection of sets, optionally from a routine.</summary>
public sealed class WorkoutSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Date { get; set; }
    public string? RoutineName { get; set; }
    public List<SetEntry> Sets { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>One line of a routine: an exercise with a prescribed set/rep scheme.</summary>
public sealed class RoutineItem
{
    public string ExerciseId { get; set; } = "";
    public int Sets { get; set; }
    public int TargetReps { get; set; }
}

/// <summary>A named, reusable workout plan, e.g. "Push Day".</summary>
public sealed class Routine
{
    public string Name { get; set; } = "";
    public List<RoutineItem> Items { get; set; } = new();
}

/// <summary>Everything persisted to disk; one JSON document, written atomically.</summary>
public sealed class WorkoutData
{
    public List<Exercise> Exercises { get; set; } = new();
    public List<WorkoutSession> Sessions { get; set; } = new();
    public List<Routine> Routines { get; set; } = new();
}
