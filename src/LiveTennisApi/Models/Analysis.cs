using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>Free-text notes attached to an analysis thesis.</summary>
    public sealed record AnalysisNotes : LiveTennisModel
    {
        /// <summary>Matchup notes, or <c>null</c>.</summary>
        [JsonPropertyName("matchup")]
        public string? Matchup { get; init; }

        /// <summary>Environment notes, or <c>null</c>.</summary>
        [JsonPropertyName("environment")]
        public string? Environment { get; init; }

        /// <summary>Fatigue notes, or <c>null</c>.</summary>
        [JsonPropertyName("fatigue")]
        public string? Fatigue { get; init; }
    }

    /// <summary>The model's directional call on a match.</summary>
    public sealed record AnalysisThesis : LiveTennisModel
    {
        /// <summary>The picked side: <c>1</c> or <c>2</c>.</summary>
        [JsonPropertyName("pick_side")]
        public int? PickSide { get; init; }

        /// <summary>Confidence in the pick, or <c>null</c>.</summary>
        [JsonPropertyName("confidence")]
        public double? Confidence { get; init; }

        /// <summary>Win probability for the picked side, or <c>null</c>.</summary>
        [JsonPropertyName("win_probability_pick")]
        public double? WinProbabilityPick { get; init; }

        /// <summary>Thesis state: <c>valid</c>, <c>confirmed</c>, <c>weakened</c>, <c>broken</c>, or <c>null</c>.</summary>
        [JsonPropertyName("state")]
        public string? State { get; init; }

        /// <summary>Human-readable reasoning, or <c>null</c>.</summary>
        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; init; }

        /// <summary>Structured notes, or <c>null</c>.</summary>
        [JsonPropertyName("notes")]
        public AnalysisNotes? Notes { get; init; }

        /// <summary>Scenario playbook entries, or <c>null</c>.</summary>
        [JsonPropertyName("scenario_playbook")]
        public IReadOnlyList<string>? ScenarioPlaybook { get; init; }

        /// <summary>When the thesis was created, ISO 8601 UTC string, or <c>null</c>.</summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }
    }

    /// <summary>A pre-match statistical profile of a match.</summary>
    public sealed record AnalysisProfile : LiveTennisModel
    {
        /// <summary>Win probability for player 1, or <c>null</c>.</summary>
        [JsonPropertyName("win_probability_p1")]
        public double? WinProbabilityP1 { get; init; }

        /// <summary>Expected closeness of the match, or <c>null</c>.</summary>
        [JsonPropertyName("expected_closeness")]
        public double? ExpectedCloseness { get; init; }

        /// <summary>Volatility rating: <c>low</c>, <c>med</c>, <c>high</c>, or <c>null</c>.</summary>
        [JsonPropertyName("volatility_rating")]
        public string? VolatilityRating { get; init; }

        /// <summary>Key factors driving the profile, or <c>null</c>.</summary>
        [JsonPropertyName("key_factors")]
        public IReadOnlyList<string>? KeyFactors { get; init; }

        /// <summary>When the profile was created, ISO 8601 UTC string, or <c>null</c>.</summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }
    }

    /// <summary>Model analysis for a match. <b>ULTRA tier only</b>; either half may be <c>null</c>.</summary>
    public sealed record Analysis : LiveTennisModel
    {
        /// <summary>The directional thesis, or <c>null</c>.</summary>
        [JsonPropertyName("thesis")]
        public AnalysisThesis? Thesis { get; init; }

        /// <summary>The pre-match profile, or <c>null</c>.</summary>
        [JsonPropertyName("profile")]
        public AnalysisProfile? Profile { get; init; }
    }
}
