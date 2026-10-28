namespace WorkoutTracker;

/// <summary>
/// The workout domain: exercises, routines, session logging, personal
/// records, volume stats and streaks. Pure logic over an <see cref="IWorkoutStore"/>,
/// so it is testable without touching the real data file.
/// </summary>
public sealed class TrackerService
{
    private readonly IWorkoutStore _store;

    public TrackerService(IWorkoutStore store) => _store = store;

    public Exercise AddExercise(string name, string muscleGroup, string unit = "kg")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Exercise name is required.", nameof(name));
        var data = _store.Load();
        if (data.Exercises.Any(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Exercise '{name}' already exists.");
        var exercise = new Exercise { Name = name.Trim(), MuscleGroup = muscleGroup.Trim(), Unit = unit };
        data.Exercises.Add(exercise);
        _store.Save(data);
        return exercise;
    }

    public Exercise RequireExercise(string name)
    {
        var data = _store.Load();
        return data.Exercises.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown exercise '{name}'. Add it first with add-exercise.");
    }

    public Routine CreateRoutine(string name, IEnumerable<RoutineItem> items)
    {
        var list = items.ToList();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Routine name is required.", nameof(name));
        if (list.Count == 0) throw new ArgumentException("A routine needs at least one item.", nameof(items));
        if (list.Any(i => i.Sets <= 0 || i.TargetReps <= 0))
            throw new ArgumentException("Routine items need positive sets and target reps.");
        var data = _store.Load();
        if (data.Routines.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Routine '{name}' already exists.");
        if (list.Any(i => data.Exercises.All(e => e.Id != i.ExerciseId)))
            throw new InvalidOperationException("Routine references an unknown exercise.");
        var routine = new Routine { Name = name.Trim(), Items = list };
        data.Routines.Add(routine);
        _store.Save(data);
        return routine;
    }

    public WorkoutSession LogSession(DateTime date, IEnumerable<SetEntry> sets, string? routineName = null, string? notes = null)
    {
        var list = sets.ToList();
        if (list.Count == 0) throw new ArgumentException("A session needs at least one set.", nameof(sets));
        if (list.Any(s => s.Reps <= 0 || s.WeightKg < 0))
            throw new ArgumentException("Sets need positive reps and non-negative weight.");
        var data = _store.Load();
        if (list.Any(s => data.Exercises.All(e => e.Id != s.ExerciseId)))
            throw new InvalidOperationException("Session references an unknown exercise.");
        var session = new WorkoutSession { Date = date.Date, RoutineName = routineName, Sets = list, Notes = notes };
        data.Sessions.Add(session);
        _store.Save(data);
        return session;
    }

    /// <summary>Heaviest single set per exercise (name → weight in kg).</summary>
    public Dictionary<string, double> PersonalRecords()
    {
        var data = _store.Load();
        var byId = data.Exercises.ToDictionary(e => e.Id, e => e.Name);
        return data.Sessions.SelectMany(s => s.Sets)
            .Where(s => byId.ContainsKey(s.ExerciseId))
            .GroupBy(s => byId[s.ExerciseId])
            .ToDictionary(g => g.Key, g => g.Max(s => s.WeightKg));
    }

    /// <summary>Total volume (reps × weight) per muscle group in a date range, inclusive.</summary>
    public Dictionary<string, double> VolumeByMuscle(DateTime from, DateTime to)
    {
        var data = _store.Load();
        var byId = data.Exercises.ToDictionary(e => e.Id, e => e.MuscleGroup);
        return data.Sessions
            .Where(s => s.Date.Date >= from.Date && s.Date.Date <= to.Date)
            .SelectMany(s => s.Sets)
            .Where(s => byId.ContainsKey(s.ExerciseId))
            .GroupBy(s => byId[s.ExerciseId])
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Reps * s.WeightKg));
    }

    /// <summary>
    /// Consecutive days with at least one session, ending on <paramref name="date"/>
    /// inclusive. A day with no session on that date itself means a streak of 0.
    /// </summary>
    public int StreakOn(DateTime date)
    {
        var days = _store.Load().Sessions.Select(s => s.Date.Date).ToHashSet();
        int streak = 0;
        for (var day = date.Date; days.Contains(day); day = day.AddDays(-1))
            streak++;
        return streak;
    }

    public IReadOnlyList<WorkoutSession> SessionsForExercise(string exerciseName)
    {
        var exercise = RequireExercise(exerciseName);
        return _store.Load().Sessions
            .Where(s => s.Sets.Any(set => set.ExerciseId == exercise.Id))
            .OrderBy(s => s.Date)
            .ToList();
    }
}
