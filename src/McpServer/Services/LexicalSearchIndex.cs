using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace McpServer.Services;

/// <summary>
/// In-process BM25 index over chunk texts. No external dependencies:
/// identifiers like map_chunk / SlotComponent are split into subtokens,
/// digits are kept whole (protocol versions are meaningful query terms).
/// </summary>
public sealed class LexicalSearchIndex
{
    private const double K1 = 1.2;
    private const double B = 0.75;

    private readonly Dictionary<string, List<Posting>> _postings;
    private readonly int[] _docLengths;
    private readonly double _avgDocLength;
    private readonly int _docCount;

    public LexicalSearchIndex(IReadOnlyList<string> documents)
    {
        _docCount = documents.Count;
        _docLengths = new int[documents.Count];
        _postings = new Dictionary<string, List<Posting>>(StringComparer.Ordinal);

        for (var doc = 0; doc < documents.Count; doc++)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var length = 0;
            foreach (var token in Tokenize(documents[doc]))
            {
                length++;
                counts[token] = counts.TryGetValue(token, out var c) ? c + 1 : 1;
            }

            _docLengths[doc] = length;
            foreach (var (token, count) in counts)
            {
                if (!_postings.TryGetValue(token, out var list))
                {
                    list = new List<Posting>();
                    _postings[token] = list;
                }
                list.Add(new Posting(doc, count));
            }
        }

        _avgDocLength = _docCount == 0 ? 1 : _docLengths.Average();
    }

    public IReadOnlyList<LexicalHit> Search(string query, int top)
    {
        var terms = Tokenize(query).Distinct(StringComparer.Ordinal).ToArray();
        if (terms.Length == 0 || _docCount == 0) return Array.Empty<LexicalHit>();

        var scores = new Dictionary<int, double>();
        foreach (var term in terms)
        {
            if (!_postings.TryGetValue(term, out var postings)) continue;

            var df = postings.Count;
            var idf = Math.Log(1 + (_docCount - df + 0.5) / (df + 0.5));
            foreach (var posting in postings)
            {
                var norm = K1 * (1 - B + B * _docLengths[posting.Doc] / _avgDocLength);
                var contribution = idf * posting.TermFrequency * (K1 + 1) / (posting.TermFrequency + norm);
                scores[posting.Doc] = scores.TryGetValue(posting.Doc, out var s) ? s + contribution : contribution;
            }
        }

        return scores
            .OrderByDescending(pair => pair.Value)
            .Take(top)
            .Select(pair => new LexicalHit(pair.Key, pair.Value))
            .ToArray();
    }

    public static IEnumerable<string> Tokenize(string text)
    {
        var sb = new StringBuilder();
        var prev = '\0';
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (sb.Length > 0 && char.IsUpper(ch) && (char.IsLower(prev) || char.IsDigit(prev)))
                {
                    if (sb.Length >= 2) yield return sb.ToString();
                    sb.Clear();
                }
                sb.Append(char.ToLowerInvariant(ch));
            }
            else if (sb.Length > 0)
            {
                if (sb.Length >= 2) yield return sb.ToString();
                sb.Clear();
            }
            prev = ch;
        }

        if (sb.Length >= 2) yield return sb.ToString();
    }

    private readonly record struct Posting(int Doc, int TermFrequency);
}

public readonly record struct LexicalHit(int Doc, double Score);
