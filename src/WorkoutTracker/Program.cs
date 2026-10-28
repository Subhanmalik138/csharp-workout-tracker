using WorkoutTracker;

var dataPath = Environment.GetEnvironmentVariable("WORKOUT_TRACKER_DATA")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".workout-tracker", "data.json");
var service = new TrackerService(new JsonWorkoutStore(dataPath));

if (args.Length == 0)
{
    PrintUsage();
    return 0;
}

try
{
    switch (args[0])
    {
        case "add-exercise":
        {
            Require(args, 3, "add-exercise <name> <muscle-group> [unit]");
            var e = service.AddExercise(args[1], args[2], args.Length > 3 ? args[3] : "kg");
            Console.WriteLine($"Added exercise: {e.Name} ({e.MuscleGroup}, {e.Unit})");
            break;
        }
        case "list-exercises":
        {
            var store = new JsonWorkoutStore(dataPath).Load();
            if (store.Exercises.Count == 0) { Console.WriteLine("No exercises yet."); break; }
            foreach (var e in store.Exercises.OrderBy(e => e.Name))
                Console.WriteLine($"{e.Name} — {e.MuscleGroup} ({e.Unit})");
            break;
        }
        case "create-routine":
        {
            // create-routine <name> <exercise:sets:targetReps> [<exercise:sets:targetReps> ...]
            Require(args, 3, "create-routine <name> <exercise:sets:targetReps> [...]");
            var items = args.Skip(2).Select(spec =>
            {
                var parts = spec.Split(':');
                if (parts.Length != 3) throw new ArgumentException($"Bad routine item '{spec}'; use exercise:sets:targetReps.");
                return new RoutineItem
                {
                    ExerciseId = service.RequireExercise(parts[0]).Id,
                    Sets = int.Parse(parts[1]),
                    TargetReps = int.Parse(parts[2]),
                };
            });
            var r = service.CreateRoutine(args[1], items);
            Console.WriteLine($"Created routine '{r.Name}' with {r.Items.Count} exercise(s).");
            break;
        }
        case "log":
        {
            // log <yyyy-MM-dd> <exercise> <reps> <weight> [<exercise> <reps> <weight> ...]
            Require(args, 5, "log <yyyy-MM-dd> <exercise> <reps> <weight> [...]");
            var date = DateTime.Parse(args[1]);
            var rest = args.Skip(2).ToArray();
            if (rest.Length % 3 != 0) throw new ArgumentException("Sets come in triples: <exercise> <reps> <weight>.");
            var sets = new List<SetEntry>();
            for (int i = 0; i < rest.Length; i += 3)
                sets.Add(new SetEntry
                {
                    ExerciseId = service.RequireExercise(rest[i]).Id,
                    Reps = int.Parse(rest[i + 1]),
                    WeightKg = double.Parse(rest[i + 2]),
                });
            var session = service.LogSession(date, sets);
            Console.WriteLine($"Logged {session.Sets.Count} set(s) on {session.Date:yyyy-MM-dd}.");
            break;
        }
        case "history":
        {
            var store = new JsonWorkoutStore(dataPath).Load();
            var sessions = args.Length > 1
                ? service.SessionsForExercise(args[1])
                : store.Sessions.OrderBy(s => s.Date).ToList();
            if (sessions.Count == 0) { Console.WriteLine("No sessions yet."); break; }
            var byId = store.Exercises.ToDictionary(e => e.Id, e => e.Name);
            foreach (var s in sessions)
            {
                var detail = string.Join(", ", s.Sets.Select(set =>
                    $"{(byId.TryGetValue(set.ExerciseId, out var n) ? n : "?")} {set.Reps}x{set.WeightKg}kg"));
                Console.WriteLine($"{s.Date:yyyy-MM-dd} {detail}");
            }
            break;
        }
        case "prs":
        {
            var prs = service.PersonalRecords();
            if (prs.Count == 0) { Console.WriteLine("No personal records yet."); break; }
            foreach (var (name, weight) in prs.OrderBy(p => p.Key))
                Console.WriteLine($"{name}: {weight} kg");
            break;
        }
        case "stats":
        {
            var from = args.Length > 1 ? DateTime.Parse(args[1]) : DateTime.Today.AddDays(-30);
            var to = args.Length > 2 ? DateTime.Parse(args[2]) : DateTime.Today;
            var volume = service.VolumeByMuscle(from, to);
            if (volume.Count == 0) { Console.WriteLine("No volume in that range."); break; }
            foreach (var (muscle, total) in volume.OrderByDescending(v => v.Value))
                Console.WriteLine($"{muscle}: {total:F0} kg total volume");
            break;
        }
        case "streak":
        {
            var date = args.Length > 1 ? DateTime.Parse(args[1]) : DateTime.Today;
            Console.WriteLine($"Current streak: {service.StreakOn(date)} day(s)");
            break;
        }
        default:
            Console.WriteLine($"Unknown command '{args[0]}'.");
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
return 0;

static void Require(string[] args, int min, string usage)
{
    if (args.Length < min) throw new ArgumentException($"Usage: {usage}");
}

static void PrintUsage()
{
    Console.WriteLine("""
        workout-tracker — CLI workout tracker

          add-exercise <name> <muscle-group> [unit]
          list-exercises
          create-routine <name> <exercise:sets:targetReps> [...]
          log <yyyy-MM-dd> <exercise> <reps> <weight> [...]
          history [exercise]
          prs
          stats [from-yyyy-MM-dd] [to-yyyy-MM-dd]
          streak [yyyy-MM-dd]

        Data file: $WORKOUT_TRACKER_DATA or ~/.workout-tracker/data.json
        """);
}
