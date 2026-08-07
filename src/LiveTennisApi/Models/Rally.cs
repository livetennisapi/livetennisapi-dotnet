using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>One player of a charted rally match.</summary>
    public sealed record RallyPlayer : LiveTennisModel
    {
        /// <summary>Name, or <c>null</c>.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        /// <summary>Playing hand (<c>R</c>, <c>L</c>, <c>U</c>, <c>A</c>), or <c>null</c>.</summary>
        [JsonPropertyName("hand")]
        public string? Hand { get; init; }
    }

    /// <summary>
    /// A charted match with shot-by-shot data. <b>ULTRA only.</b> Rally
    /// construction is the layer below the tape: the tape says what the score
    /// became after each point, this says how the point was played.
    /// </summary>
    /// <remarks>
    /// This product has its <b>own id space</b> (<see cref="RallyMatchId"/>). The
    /// charted corpus and our own match table are different populations — the
    /// corpus reaches back decades and concentrates on the biggest events.
    /// <see cref="MatchId"/> links to our match id only when the charted match is
    /// also one we hold; it is <c>null</c> for most charted matches. Coverage is
    /// curated (human charting), deep but not universal — ask the list endpoint
    /// for the authoritative coverage rather than assuming a match is charted.
    /// </remarks>
    public record RallyMatch : LiveTennisModel
    {
        /// <summary>The id this product is keyed on.</summary>
        [JsonPropertyName("rally_match_id")]
        public int? RallyMatchId { get; init; }

        /// <summary>The stable source key.</summary>
        [JsonPropertyName("source_id")]
        public string? SourceId { get; init; }

        /// <summary>
        /// <b>Our</b> match id, when the charted match is also one we hold.
        /// <c>null</c> otherwise — most charted matches predate our collection.
        /// </summary>
        [JsonPropertyName("match_id")]
        public int? MatchId { get; init; }

        /// <summary>Match date (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        /// <summary>Tournament name, or <c>null</c>.</summary>
        [JsonPropertyName("tournament")]
        public string? Tournament { get; init; }

        /// <summary>Round, or <c>null</c>.</summary>
        [JsonPropertyName("round")]
        public string? Round { get; init; }

        /// <summary>Surface, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Gender: <c>M</c>, <c>W</c>, or <c>null</c>.</summary>
        [JsonPropertyName("gender")]
        public string? Gender { get; init; }

        /// <summary>Best of 3 or 5, or <c>null</c>.</summary>
        [JsonPropertyName("best_of")]
        public int? BestOf { get; init; }

        /// <summary>The two players, index 0 = player 1.</summary>
        [JsonPropertyName("players")]
        public IReadOnlyList<RallyPlayer>? Players { get; init; }

        /// <summary>Charted points in this match.</summary>
        [JsonPropertyName("points")]
        public int? Points { get; init; }

        /// <summary>
        /// How many of the charted points our parser read cleanly — the
        /// per-match quality number.
        /// </summary>
        [JsonPropertyName("points_parsed")]
        public int? PointsParsed { get; init; }
    }

    /// <summary>
    /// One stroke of a charted point. Shots are numbered from the serve:
    /// serve 1, return 2, the server's next ball 3.
    /// </summary>
    public sealed record RallyShot : LiveTennisModel
    {
        /// <summary>Shot number within the point (serve = 1).</summary>
        [JsonPropertyName("number")]
        public int? Number { get; init; }

        /// <summary>The charter's raw code, e.g. <c>f</c>.</summary>
        [JsonPropertyName("code")]
        public string? Code { get; init; }

        /// <summary>
        /// Stroke type: <c>serve</c>, <c>groundstroke</c>, <c>slice</c>,
        /// <c>volley</c>, <c>half_volley</c>, <c>swinging_volley</c>,
        /// <c>overhead</c>, <c>drop_shot</c>, <c>lob</c>, <c>trick</c>,
        /// <c>unknown</c>, or <c>null</c>.
        /// </summary>
        [JsonPropertyName("stroke")]
        public string? Stroke { get; init; }

        /// <summary>The side it was struck <b>from</b>: <c>forehand</c>, <c>backhand</c>, or <c>null</c>.</summary>
        [JsonPropertyName("wing")]
        public string? Wing { get; init; }

        /// <summary>Where the ball was sent: <c>forehand_side</c>, <c>middle</c>, <c>backhand_side</c>, or <c>null</c>.</summary>
        [JsonPropertyName("direction")]
        public string? Direction { get; init; }

        /// <summary>Depth: <c>shallow</c>, <c>mid</c>, <c>deep</c>, or <c>null</c>.</summary>
        [JsonPropertyName("depth")]
        public string? Depth { get; init; }

        /// <summary>Court position: <c>approaching</c>, <c>at_net</c>, <c>baseline</c>, or <c>null</c>.</summary>
        [JsonPropertyName("position")]
        public string? Position { get; init; }
    }

    /// <summary>
    /// One charted point. <see cref="Raw"/> is the charter's own string,
    /// verbatim, and is always present; the parsed fields are our reading of it.
    /// <see cref="Parsed"/> is <c>false</c> when the notation contained something
    /// we could not read cleanly — the recognised part is still returned. A
    /// consumer who wants only unambiguous rows filters on <see cref="Parsed"/>.
    /// </summary>
    public sealed record RallyPoint : LiveTennisModel
    {
        /// <summary>Point number within the match.</summary>
        [JsonPropertyName("point")]
        public int? Point { get; init; }

        /// <summary>Set score entering the point: <c>[p1, p2]</c> (entries may be null).</summary>
        [JsonPropertyName("set")]
        public IReadOnlyList<int?>? Set { get; init; }

        /// <summary>Game score entering the point: <c>[p1, p2]</c> (entries may be null).</summary>
        [JsonPropertyName("games")]
        public IReadOnlyList<int?>? Games { get; init; }

        /// <summary>Point score, e.g. <c>30-40</c>, or <c>null</c>.</summary>
        [JsonPropertyName("score")]
        public string? Score { get; init; }

        /// <summary>Game number, or <c>null</c>.</summary>
        [JsonPropertyName("game")]
        public int? Game { get; init; }

        /// <summary>Whether the point was played in a tiebreak.</summary>
        [JsonPropertyName("is_tiebreak")]
        public bool? IsTiebreak { get; init; }

        /// <summary>The server (<c>1</c> or <c>2</c>), or <c>null</c>.</summary>
        [JsonPropertyName("server")]
        public int? Server { get; init; }

        /// <summary>Who won the point (<c>1</c> or <c>2</c>), or <c>null</c>.</summary>
        [JsonPropertyName("point_winner")]
        public int? PointWinner { get; init; }

        /// <summary>
        /// The charter's shot string, verbatim; both serves joined by <c>;</c>
        /// when the first was a fault.
        /// </summary>
        [JsonPropertyName("raw")]
        public string? Raw { get; init; }

        /// <summary>Whether the notation was read cleanly.</summary>
        [JsonPropertyName("parsed")]
        public bool? Parsed { get; init; }

        /// <summary>Which serve was in play (<c>1</c> or <c>2</c>), or <c>null</c>.</summary>
        [JsonPropertyName("serve_number")]
        public int? ServeNumber { get; init; }

        /// <summary>Serve direction: <c>wide</c>, <c>body</c>, <c>down_the_t</c>, or <c>null</c>.</summary>
        [JsonPropertyName("serve_direction")]
        public string? ServeDirection { get; init; }

        /// <summary>Strokes including the serve. An ace is 1, a double fault 0.</summary>
        [JsonPropertyName("rally_length")]
        public int? RallyLength { get; init; }

        /// <summary>
        /// How the point ended: <c>winner</c>, <c>forced_error</c>,
        /// <c>unforced_error</c>, <c>error</c> (a miss the charter did not
        /// classify — never guessed), <c>other</c>, or <c>null</c>.
        /// </summary>
        [JsonPropertyName("outcome")]
        public string? Outcome { get; init; }

        /// <summary>Where the error landed: <c>net</c>, <c>wide</c>, <c>deep</c>, <c>wide_and_deep</c>, or <c>null</c>.</summary>
        [JsonPropertyName("error_location")]
        public string? ErrorLocation { get; init; }

        /// <summary>The ending stroke type, or <c>null</c>.</summary>
        [JsonPropertyName("ending_stroke")]
        public string? EndingStroke { get; init; }

        /// <summary>The ending wing, or <c>null</c>.</summary>
        [JsonPropertyName("ending_wing")]
        public string? EndingWing { get; init; }

        /// <summary>Whether the point was an ace.</summary>
        [JsonPropertyName("is_ace")]
        public bool? IsAce { get; init; }

        /// <summary>Whether the point was a double fault.</summary>
        [JsonPropertyName("is_double_fault")]
        public bool? IsDoubleFault { get; init; }

        /// <summary>Whether the server serve-and-volleyed.</summary>
        [JsonPropertyName("is_serve_and_volley")]
        public bool? IsServeAndVolley { get; init; }

        /// <summary>The strokes of the point, in order.</summary>
        [JsonPropertyName("shots")]
        public IReadOnlyList<RallyShot>? Shots { get; init; }
    }

    /// <summary>
    /// One charted match with its points, in play order. <b>ULTRA only.</b>
    /// Paged with <c>limit</c>/<c>offset</c>; <c>Meta.Total</c> is the match's
    /// full point count.
    /// </summary>
    public sealed record RallyMatchDetail : RallyMatch
    {
        /// <summary>The charted points on this page, in play order.</summary>
        [JsonPropertyName("rally")]
        public IReadOnlyList<RallyPoint>? Rally { get; init; }

        /// <summary>The pagination envelope; <see cref="ListMeta.Total"/> is the full point count.</summary>
        [JsonPropertyName("meta")]
        public ListMeta? Meta { get; init; }
    }
}
