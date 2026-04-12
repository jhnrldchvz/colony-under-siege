public static class ScoreCalculator
{
    public static int Calculate(int rawKillPoints, float accuracy,
        float elapsedSeconds, int deaths, bool stageCompleted)
    {
        int score = rawKillPoints;
        if (accuracy >= 0.70f)      score += 500;
        else if (accuracy >= 0.50f) score += 250;
        if (elapsedSeconds < 300f)  score += 200;
        if (stageCompleted)         score += 300;
        score -= deaths * 100;
        return score;
    }

    public static string GetGrade(int score)
    {
        if (score >= 3000) return "S";
        if (score >= 2000) return "A";
        if (score >= 1000) return "B";
        if (score >= 500)  return "C";
        return "D";
    }
}
