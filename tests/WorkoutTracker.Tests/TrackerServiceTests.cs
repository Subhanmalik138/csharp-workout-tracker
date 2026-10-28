using WorkoutTracker;
using Xunit;

namespace WorkoutTracker.Tests;

public sealed class TrackerServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
    private TrackerService Service => new(new JsonWorkoutStore(_path));

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    private Exercise Bench(TrackerService s) => s.AddExercise("Bench Press", "Chest");
    private Exercise Squat(TrackerService s) => s.AddExercise("Squat", "Legs");

    [Fact]
    public void AddExercise_RejectsDuplicateName_CaseInsensitive()
    {
        var s = Service;
        Bench(s);
        Assert.Throws<InvalidOperationException>(() => s.AddExercise("bench press", "Chest"));
    }

    [Fact]
    public void LogSession_RejectsUnknownExercise()
    {
        var s = Service;
        Assert.Throws<InvalidOperationException>(() =>
            s.LogSession(DateTime.Today, new[] { new SetEntry { ExerciseId = "nope", Reps = 5, WeightKg = 60 } }));
    }

    [Fact]
    public void LogSession_RejectsEmptyAndInvalidSets()
    {
        var s = Service;
        var bench = Bench(s);
        Assert.Throws<ArgumentException>(() => s.LogSession(DateTime.Today, Array.Empty<SetEntry>()));
        Assert.Throws<ArgumentException>(() => s.LogSession(DateTime.Today,
            new[] { new SetEntry { ExerciseId = bench.Id, Reps = 0, WeightKg = 60 } }));
        Assert.Throws<ArgumentException>(() => s.LogSession(DateTime.Today,
            new[] { new SetEntry { ExerciseId = bench.Id, Reps = 5, WeightKg = -1 } }));
    }

    [Fact]
    public void PersonalRecords_PicksHeaviestSetPerExercise()
    {
        var s = Service;
        var bench = Bench(s);
        var squat = Squat(s);
        s.LogSession(new DateTime(2026, 10, 26), new[]
        {
            new SetEntry { ExerciseId = bench.Id, Reps = 8, WeightKg = 70 },
            new SetEntry { ExerciseId = squat.Id, Reps = 5, WeightKg = 100 },
        });
        s.LogSession(new DateTime(2026, 10, 27), new[]
        {
            new SetEntry { ExerciseId = bench.Id, Reps = 5, WeightKg = 80 },
        });
        var prs = s.PersonalRecords();
        Assert.Equal(80, prs["Bench Press"]);
        Assert.Equal(100, prs["Squat"]);
    }

    [Fact]
    public void VolumeByMuscle_SumsRepsTimesWeight_InRangeOnly()
    {
        var s = Service;
        var bench = Bench(s);
        s.LogSession(new DateTime(2026, 10, 20), new[] { new SetEntry { ExerciseId = bench.Id, Reps = 10, WeightKg = 60 } });
        s.LogSession(new DateTime(2026, 10, 27), new[] { new SetEntry { ExerciseId = bench.Id, Reps = 8, WeightKg = 80 } });
        var volume = s.VolumeByMuscle(new DateTime(2026, 10, 25), new DateTime(2026, 10, 28));
        Assert.Equal(640, volume["Chest"]);
    }

    [Fact]
    public void StreakOn_CountsConsecutiveDays_AndBreaksOnGap()
    {
        var s = Service;
        var bench = Bench(s);
        var day = new DateTime(2026, 10, 28);
        foreach (var d in new[] { day.AddDays(-2), day.AddDays(-1), day })
            s.LogSession(d, new[] { new SetEntry { ExerciseId = bench.Id, Reps = 5, WeightKg = 60 } });
        Assert.Equal(3, s.StreakOn(day));
        Assert.Equal(0, s.StreakOn(day.AddDays(1)));
    }

    [Fact]
    public void CreateRoutine_RejectsDuplicateAndUnknownExercise()
    {
        var s = Service;
        var bench = Bench(s);
        s.CreateRoutine("Push", new[] { new RoutineItem { ExerciseId = bench.Id, Sets = 3, TargetReps = 8 } });
        Assert.Throws<InvalidOperationException>(() =>
            s.CreateRoutine("push", new[] { new RoutineItem { ExerciseId = bench.Id, Sets = 3, TargetReps = 8 } }));
        Assert.Throws<InvalidOperationException>(() =>
            s.CreateRoutine("Legs", new[] { new RoutineItem { ExerciseId = "nope", Sets = 3, TargetReps = 8 } }));
    }

    [Fact]
    public void Store_RoundTripsDataThroughJsonFile()
    {
        var s = Service;
        var bench = Bench(s);
        s.LogSession(new DateTime(2026, 10, 28), new[] { new SetEntry { ExerciseId = bench.Id, Reps = 5, WeightKg = 60 } });
        var reloaded = new JsonWorkoutStore(_path).Load();
        Assert.Single(reloaded.Exercises);
        Assert.Single(reloaded.Sessions);
        Assert.Equal(5, reloaded.Sessions[0].Sets[0].Reps);
    }

    [Fact]
    public void SessionsForExercise_ReturnsOnlyMatchingSessions_InDateOrder()
    {
        var s = Service;
        var bench = Bench(s);
        var squat = Squat(s);
        s.LogSession(new DateTime(2026, 10, 27), new[] { new SetEntry { ExerciseId = squat.Id, Reps = 5, WeightKg = 100 } });
        s.LogSession(new DateTime(2026, 10, 26), new[] { new SetEntry { ExerciseId = bench.Id, Reps = 8, WeightKg = 70 } });
        var sessions = s.SessionsForExercise("Bench Press");
        Assert.Single(sessions);
        Assert.Equal(new DateTime(2026, 10, 26), sessions[0].Date);
    }
}
