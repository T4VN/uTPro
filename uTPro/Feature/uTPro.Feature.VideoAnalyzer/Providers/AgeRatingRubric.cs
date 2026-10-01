namespace uTPro.Feature.VideoAnalyzer.Providers;

/// <summary>
/// Default age-rating rubric embedded into the analysis prompt, following the Vietnamese
/// film rating scale from Decree 81/2023/ND-CP (P, K, C13, C16, C18). Kept as a single
/// constant so it stays in sync with the report model; swap here to adopt another scale.
/// </summary>
public static class AgeRatingRubric
{
    public const string Text = """
        Age-suitability rubric (Vietnamese film rating scale, Decree 81/2023/ND-CP):
        - "P": suitable for all ages; no content that could disturb children.
        - "K": broadly suitable, but contains some content children under 13 should be guided about.
        - "C13": restricted to 13 and older (mild violence, mild horror, romantic themes).
        - "C16": restricted to 16 and older (violence, horror, alcohol/tobacco, intense themes).
        - "C18": restricted to 18 and older (graphic violence, explicit sexual content, heavy drug use, gambling promotion, self-harm).
        Choose the STRICTEST label any part of the video (speech, visuals, on-screen text) justifies.
        flags must use these English tokens when applicable: violence, gore, horror, sexual-content,
        language, alcohol, drugs, gambling, discrimination, self-harm, weapons, crime, distressing.
        reasons: 1-3 short sentences citing what was seen/heard and where (with timestamps).
        confidence: 0.0-1.0. When the evidence is unclear, lower the confidence.
        """;
}
