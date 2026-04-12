using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class AlgorithmSimulation : MonoBehaviour
{
    [Serializable]
    public struct AlgorithmProfile
    {
        public string name;
        public float speedMs;
        public float memoryKb;
        public float aggressiveness;
    }

    private struct Accumulator
    {
        public int count;
        public double speedSum;
        public double speedSq;
        public double memorySum;
        public double memorySq;
        public double aggressivenessSum;
        public double aggressivenessSq;

        public void Add(float speed, float memory, float aggressiveness)
        {
            count++;
            speedSum += speed;
            speedSq += speed * speed;
            memorySum += memory;
            memorySq += memory * memory;
            aggressivenessSum += aggressiveness;
            aggressivenessSq += aggressiveness * aggressiveness;
        }
    }

    private struct RunRow
    {
        public int runIndex;
        public string algorithm;
        public float speedMs;
        public float memoryKb;
        public float aggressiveness;
    }

    [Header("Simulation")]
    public int runs = 100;
    public bool runOnStart = true;
    public bool useFixedSeed = true;
    public int fixedSeed = 12345;

    [Header("Noise (percent of mean)")]
    [Range(0f, 0.5f)] public float speedNoisePct = 0.15f;
    [Range(0f, 0.5f)] public float memoryNoisePct = 0.25f;
    [Range(0f, 0.5f)] public float aggressivenessNoisePct = 0.2f;

    [Header("Output")]
    public bool writeCsv = false;
    public string csvFileName = "algorithm_simulation.csv";

    [Header("Baseline Profiles")]
    public AlgorithmProfile[] baselineProfiles = new AlgorithmProfile[]
    {
        new AlgorithmProfile
        {
            name = "Particle Swarm Optimization",
            speedMs = 1.12f,
            memoryKb = 15.36f,
            aggressiveness = 0.3978f
        },
        new AlgorithmProfile
        {
            name = "Gray Wolf Optimization",
            speedMs = 2.67f,
            memoryKb = 64.85f,
            aggressiveness = 0.4411f
        },
        new AlgorithmProfile
        {
            name = "Genetic Algorithm",
            speedMs = 1.84f,
            memoryKb = 28.49f,
            aggressiveness = 0.8f
        }
    };

    private System.Random _rng;

    private void Start()
    {
        if (runOnStart)
            RunSimulation();
    }

    [ContextMenu("Run Simulation")]
    public void RunSimulation()
    {
        if (baselineProfiles == null || baselineProfiles.Length == 0)
        {
            Debug.LogWarning("[AlgorithmSimulation] No baseline profiles configured.");
            return;
        }

        _rng = useFixedSeed ? new System.Random(fixedSeed) : new System.Random();

        var rows = new List<RunRow>(runs * baselineProfiles.Length);
        var stats = new Dictionary<string, Accumulator>();

        foreach (AlgorithmProfile profile in baselineProfiles)
            stats[profile.name] = new Accumulator();

        for (int r = 0; r < runs; r++)
        {
            foreach (AlgorithmProfile profile in baselineProfiles)
            {
                float speed = SamplePositive(profile.speedMs, speedNoisePct);
                float memory = SamplePositive(profile.memoryKb, memoryNoisePct);
                float aggressiveness = SampleClamped01(profile.aggressiveness, aggressivenessNoisePct);

                rows.Add(new RunRow
                {
                    runIndex = r + 1,
                    algorithm = profile.name,
                    speedMs = speed,
                    memoryKb = memory,
                    aggressiveness = aggressiveness
                });

                Accumulator acc = stats[profile.name];
                acc.Add(speed, memory, aggressiveness);
                stats[profile.name] = acc;
            }
        }

        string summary = BuildSummaryTable(stats, runs);
        Debug.Log(summary);

        if (writeCsv)
            WriteCsv(rows);
    }

    private float SamplePositive(float mean, float noisePct)
    {
        double stdDev = Math.Max(0.0001, mean * noisePct);
        double value = mean + NextGaussian() * stdDev;
        return Mathf.Max(0.001f, (float)value);
    }

    private float SampleClamped01(float mean, float noisePct)
    {
        double stdDev = Math.Max(0.0001, mean * noisePct);
        double value = mean + NextGaussian() * stdDev;
        return Mathf.Clamp01((float)value);
    }

    // Box-Muller transform for normal distribution
    private double NextGaussian()
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    private string BuildSummaryTable(Dictionary<string, Accumulator> stats, int runCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Design Constraint Criteria");
        sb.AppendLine("Criteria | Algorithm | Speed (ms) | Memory (kB) | Aggressiveness");
        sb.AppendLine("---|---|---:|---:|---:");

        foreach (AlgorithmProfile profile in baselineProfiles)
        {
            if (!stats.TryGetValue(profile.name, out Accumulator acc) || acc.count == 0)
                continue;

            double speedMean = acc.speedSum / acc.count;
            double memoryMean = acc.memorySum / acc.count;
            double aggroMean = acc.aggressivenessSum / acc.count;

            sb.AppendLine(
                $"Optimization | {profile.name} | {speedMean:F2} | {memoryMean:F2} | {aggroMean:F4}");
        }

        sb.AppendLine($"(n = {runCount} per algorithm)");
        return sb.ToString();
    }

    private void WriteCsv(List<RunRow> rows)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, csvFileName);
            var sb = new StringBuilder();
            sb.AppendLine("run,algorithm,speed_ms,memory_kb,aggressiveness");
            foreach (RunRow row in rows)
                sb.AppendLine($"{row.runIndex},{row.algorithm},{row.speedMs:F4},{row.memoryKb:F4},{row.aggressiveness:F4}");

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[AlgorithmSimulation] CSV written to: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AlgorithmSimulation] CSV write failed: {ex.Message}");
        }
    }
}
