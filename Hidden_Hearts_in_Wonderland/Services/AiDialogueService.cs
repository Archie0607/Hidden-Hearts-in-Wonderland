using System.Text;
using System.Text.Json;
using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class AiDialogueService
{
    public const string GeneratedNextNodeId = "__ai_next__";

    private const string ApiKey = "";
    private const string Model = "llama-3.3-70b-versatile";
    private static readonly Uri Endpoint = new("https://api.groq.com/openai/v1/chat/completions");

    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ScenePack> GenerateScenePackAsync(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        var prompt = BuildScenePackPrompt(character, templateNode, affection, roundId);
        return await RequestCompleteScenePackAsync(prompt, "6-turn scene pack with heroine replies and exactly 3 player choices per turn");
    }

    public async Task<DialogueNode> GenerateOpeningNodeAsync(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        var prompt = BuildOpeningPrompt(character, templateNode, affection, roundId);
        var (result, choices) = await RequestCompleteTurnAsync(prompt, "opening dialogue with reply, moodTag, and exactly 3 player choices");

        return new DialogueNode
        {
            Id = $"opening_{roundId}",
            CharacterName = character.Name,
            Image = CharacterProfileService.GetImageForMood(character.Name, result.MoodTag, 0),
            Text = result.Reply.Trim(),
            Choices = choices
        };
    }

    public async Task<List<Choice>> GenerateChoicesAsync(Character character, DialogueNode node, int affection)
    {
        var prompt = BuildChoicesPrompt(character, node, affection);
        return await RequestCompleteChoicesAsync(prompt, "exactly 3 player choices");
    }

    public async Task<DialogueNode> GenerateNextNodeAsync(Character character, DialogueNode currentNode, Choice selectedChoice, int affection, IReadOnlyList<string>? roundHistory = null)
    {
        var prompt = BuildNextTurnPrompt(character, currentNode, selectedChoice, affection, roundHistory ?? []);
        var (result, choices) = await RequestCompleteTurnAsync(prompt, "next dialogue with reply, moodTag, and exactly 3 player choices");

        return new DialogueNode
        {
            Id = $"ai_{Guid.NewGuid():N}",
            CharacterName = character.Name,
            Image = CharacterProfileService.GetImageForMood(character.Name, result.MoodTag, selectedChoice.AffectionChange),
            Text = result.Reply.Trim(),
            Choices = choices
        };
    }

    private async Task<(AiTurnResponse Result, List<Choice> Choices)> RequestCompleteTurnAsync(string prompt, string taskName)
    {
        var lastIssue = string.Empty;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var attemptPrompt = attempt == 1
                ? prompt
                : BuildRetryPrompt(prompt, taskName, lastIssue);

            var result = await RequestAiAsync<AiTurnResponse>(attemptPrompt);
            var choices = NormalizeChoices(result?.Choices);
            lastIssue = DescribeTurnIssue(result, choices);

            if (string.IsNullOrWhiteSpace(lastIssue))
            {
                return (result!, choices);
            }
        }

        throw new InvalidOperationException($"Groq did not return a complete {taskName}. Last issue: {lastIssue}");
    }

    private async Task<List<Choice>> RequestCompleteChoicesAsync(string prompt, string taskName)
    {
        var lastIssue = string.Empty;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var attemptPrompt = attempt == 1
                ? prompt
                : BuildRetryPrompt(prompt, taskName, lastIssue);

            var result = await RequestAiAsync<AiChoiceResponse>(attemptPrompt);
            var choices = NormalizeChoices(result?.Choices);
            lastIssue = DescribeChoicesIssue(choices);

            if (string.IsNullOrWhiteSpace(lastIssue))
            {
                return choices;
            }
        }

        throw new InvalidOperationException($"Groq did not return {taskName}. Last issue: {lastIssue}");
    }

    private async Task<ScenePack> RequestCompleteScenePackAsync(string prompt, string taskName)
    {
        var lastIssue = string.Empty;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var attemptPrompt = attempt == 1
                ? prompt
                : BuildRetryPrompt(prompt, taskName, lastIssue);

            var result = await RequestAiAsync<ScenePack>(attemptPrompt);
            var scenePack = NormalizeScenePack(result);
            lastIssue = DescribeScenePackIssue(scenePack);

            if (string.IsNullOrWhiteSpace(lastIssue))
            {
                return scenePack;
            }
        }

        throw new InvalidOperationException($"Groq did not return a complete {taskName}. Last issue: {lastIssue}");
    }

    private async Task<T?> RequestAiAsync<T>(string prompt)
    {
        var text = await RequestAiTextAsync(prompt, 0.75);
        if (TryParseAiJson(text, out T? parsed))
        {
            return parsed;
        }

        var repairedText = await RequestAiTextAsync(BuildJsonRepairPrompt<T>(text), 0.2);
        if (TryParseAiJson(repairedText, out parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Groq returned JSON that could not be parsed after repair. Raw: {ShortenForError(text)}");
    }

    private async Task<string> RequestAiTextAsync(string prompt, double temperature)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("Missing Groq API key. Fill ApiKey in AiDialogueService before starting the app.");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        var systemPrompt = "You are a JSON-only writer for a Thai dating visual novel. Return one valid JSON object only.";
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {ApiKey}");

        var body = new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = systemPrompt
                },
                new
                {
                    role = "user",
                    content = prompt
                },
            },
            temperature,
            max_tokens = 6000,
            response_format = new
            {
                type = "json_object"
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Groq request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {json}");
        }

        var groq = JsonSerializer.Deserialize<GroqChatResponse>(json, _jsonOptions);
        var text = groq?.Choices?
            .FirstOrDefault()?
            .Message?
            .Content;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Groq returned an empty message.");
        }

        return text;
    }

    private static string BuildScenePackPrompt(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        var personalityPrompt = CharacterProfileService.GetPersonalityPrompt(character.Name);

        return $$"""
        You write a complete 6-turn conversation scene pack for a Thai dating visual novel named Hidden Hearts in Wonderland.

        Heroine personality:
        {{personalityPrompt}}

        Heroine: {{character.Name}}
        Current affection before the round: {{affection}}
        Round id for variety: {{roundId}}
        Old template seed, do not copy directly: "{{templateNode.Text}}"
        Setting seed: Wonderland fantasy town/garden/castle paths/tea corners. Use the round id as a variety seed.

        Allowed moodTag and expectedMood values:
        neutral, happy, shy, sad, angry, romantic, thinking, surprised, annoyed, confused, sleepy, reading

        Return JSON only:
        {
          "turns": [
            {
              "reply": "Thai spoken dialogue from the heroine, 1-3 sentences",
              "moodTag": "neutral",
              "choices": [
                {
                  "text": "Thai player spoken dialogue line",
                  "affectionChange": 5,
                  "isMiniGame": false,
                  "expectedMood": "happy",
                  "reply": "Thai spoken dialogue from the heroine reacting to this exact choice",
                  "moodTag": "happy"
                }
              ]
            }
          ]
        }

        Scene pack rules:
        - Generate exactly 6 turns in the turns array.
        - Turn 1 reply is the opening line from {{character.Name}}.
        - Turns 2-6 replies are shared continuation lines that should still make sense after any previous choice.
        - Every turn must continue the same small situation like one connected scene, not restart the conversation.
        - Each turn must have exactly 3 choices.
        - Every choice must have a choice-specific heroine reply that directly reacts to that exact player line.
        - After the player selects any choice, the game will show that choice's reply, then advance to the next shared turn's choices.
        - Keep continuity broad enough that the next shared turn can follow from positive, neutral, or negative previous replies.
        - Do not make a 3^6 branching tree. Generate one linear 6-turn scene with 3 reactive replies per turn.
        - Do not repeat or closely paraphrase any player choice across all 6 turns.
        - Do not copy or paraphrase any fixed starter text from the game data.
        - Avoid making {{character.Name}} read a book unless the selected moodTag is reading.

        Dialogue rules:
        - Write like a soft, slightly shy Thai anime visual novel heroine. Use natural Thai spoken rhythm, not formal translated Thai.
        - Thai lines should feel casual, warm, and alive, with gentle particles or hesitation when appropriate, such as "อื้ม", "เอ่อ", "นะ", "ล่ะ", "เหรอ", "ถ้าไม่รบกวน".
        - Avoid stiff, literal, questionnaire-like Thai. Do not make every line sound like an interview question.
        - Prefer emotionally specific spoken lines over generic sentences.
        - All heroine replies must be direct spoken dialogue from {{character.Name}} to the player.
        - All player choice text must be direct spoken dialogue from the player to {{character.Name}}.
        - Do not write narration, actions, stage directions, descriptions of tone, or third-person text.
        - Do not write "{{character.Name}} smiles", "{{character.Name}} looks", or any third-person narration.
        - The heroine must speak like her personality, likes, dislikes, and current emotion.
        - Every choice must directly answer or react to the heroine line for that turn.
        - Choice-specific replies must answer the meaning or emotion of the selected choice.
        - Do not quote or repeat the selected choice verbatim inside the heroine reply.
        - Make the dialogue feel like two people talking, not like summarizing what the player chose.
        - Write natural Thai. No markdown. No extra explanation.

        Choice role rules for every turn:
        - Choice 1 should be positive: respects her personality, likes, current emotion, or boundaries. affectionChange must be 5 to 12.
        - Choice 2 should be neutral: polite and natural but not especially intimate. affectionChange must be 0 to 3.
        - Choice 3 should be negative: touches one of her dislikes, misunderstands her, ignores her boundary, teases badly, is too pushy, or says something emotionally careless. affectionChange must be -8 to -2.
        - The negative choice must still sound like natural player dialogue, not cartoonishly evil.
        - The negative choice must relate to her listed dislikes or current emotional context.
        - affectionChange must be between -8 and 12.
        - isMiniGame must always be false.

        Mood rules:
        - If the player is caring, respectful, funny in a kind way, or raises affection, use happy, shy, thinking, or romantic.
        - If the player is cold, rude, careless, touches a listed dislike, violates a boundary, or lowers affection, use sad, angry, annoyed, or confused.
        - If the player surprises her, use surprised or thinking.
        - If affectionChange is 9 or higher, strongly consider romantic or shy.
        - If affectionChange is below 0, avoid happy and romantic unless the reply is bittersweet.
        """;
    }

    private static string BuildOpeningPrompt(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        var personalityPrompt = CharacterProfileService.GetPersonalityPrompt(character.Name);

        return $$"""
        You write a fresh opening line for a new conversation round in a Thai dating visual novel named Hidden Hearts in Wonderland.

        Heroine personality:
        {{personalityPrompt}}

        Heroine: {{character.Name}}
        Current affection: {{affection}}
        Round id for variety: {{roundId}}
        Setting seed: Wonderland fantasy town/garden/castle paths/tea corners. Use the round id as a variety seed.

        Allowed moodTag values:
        neutral, happy, shy, sad, angry, romantic, thinking, surprised, annoyed, confused, sleepy, reading

        Return JSON only:
        {
          "reply": "Thai spoken dialogue from the heroine, 1-3 sentences",
          "moodTag": "neutral",
          "choices": [
            {
              "text": "Thai player spoken dialogue line",
              "affectionChange": 5,
              "isMiniGame": false,
              "expectedMood": "happy"
            }
          ]
        }

        Rules:
        - reply must be direct spoken dialogue from {{character.Name}} to the player.
        - Do not write narration, actions, stage directions, or descriptions in reply.
        - Do not write "{{character.Name}} smiles", "{{character.Name}} looks", or any third-person narration.
        - The reply must be speakable aloud as the heroine's own words.
        - This is a new conversation round. Create a different first situation from previous rounds.
        - This opening starts a connected 6-turn mini-scene. Set up one clear situation that can naturally continue for 6 turns.
        - Give the heroine a small immediate concern, curiosity, invitation, or emotional hook that the player can respond to.
        - Do not copy or paraphrase any fixed starter text from the game data.
        - Avoid making {{character.Name}} read a book unless the selected moodTag is reading.
        - Keep the same setting mood, but vary the place, action, prop, or first emotional beat.
        - Write exactly 3 choices in Thai.
        - Every choice must directly respond to or follow from {{character.Name}}'s reply.
        - The choices must feel like natural answers to what {{character.Name}} just said, not generic pickup lines.
        - Each choice should lead the same situation forward, not jump to a different scene.
        - Every choice text must be direct speech from the player to {{character.Name}}.
        - Do not write actions, narration, stage directions, or descriptions of tone.
        - Do not write text like "ยิ้มให้", "เดินเข้าไป", "ถามว่า", "พูดกับ", or "ชวนคุย".
        - Good choice text examples: "วันนี้เธอดูสบายใจกว่าปกตินะ", "ถ้าไม่รังเกียจ ฉันขออยู่ตรงนี้ด้วยได้ไหม"
        - Generate exactly 3 player choices with these roles:
          1. Positive: respects her personality, likes, current emotion, or boundaries. affectionChange must be 5 to 12.
          2. Neutral: polite and natural but not especially intimate. affectionChange must be 0 to 3.
          3. Negative: touches one of her dislikes, misunderstands her, ignores her boundary, teases badly, is too pushy, or says something emotionally careless. affectionChange must be -8 to -2.
        - The negative choice must still sound like natural player dialogue, not cartoonishly evil.
        - The negative choice must relate to her listed dislikes or current emotional context.
        - affectionChange must be between -8 and 12.
        - isMiniGame must always be false.
        - No markdown. No extra explanation.
        """;
    }

    private static string BuildChoicesPrompt(Character character, DialogueNode node, int affection)
    {
        var personalityPrompt = CharacterProfileService.GetPersonalityPrompt(character.Name);

        return $$"""
        You write player choices for a Thai dating visual novel named Hidden Hearts in Wonderland.

        Heroine personality:
        {{personalityPrompt}}

        Heroine: {{character.Name}}
        Current affection: {{affection}}
        Current heroine mood image: {{node.Image}}
        Latest heroine line: "{{node.Text}}"
        Generation seed for variety: {{Guid.NewGuid():N}}

        Allowed mood tags for expectedMood:
        neutral, happy, shy, sad, angry, romantic, thinking, surprised, annoyed, confused, sleepy, reading

        Return JSON only:
        {
          "choices": [
            {
              "text": "Thai player spoken dialogue line",
              "affectionChange": 5,
              "isMiniGame": false,
              "expectedMood": "happy"
            }
          ]
        }

        Rules:
        - Write exactly 3 choices in Thai.
        - Every choice must directly answer or react to the latest heroine line.
        - If the heroine asks, worries, hints, or shows emotion, each choice should address that exact context.
        - Do not write generic choices that could fit any scene.
        - Each choice must be a line of dialogue the player says directly to {{character.Name}}.
        - Do not write actions, narration, stage directions, or descriptions of tone.
        - Do not write text like "ยิ้มให้", "เดินเข้าไป", "ถามว่า", "พูดกับ", or "ชวนคุย".
        - The player can imply emotion through words, but the text itself must be speakable aloud.
        - Do not repeat the same choice wording from earlier turns.
        - Generate exactly 3 player choices with these roles:
          1. Positive: respects her personality, likes, current emotion, or boundaries. affectionChange must be 5 to 12.
          2. Neutral: polite and natural but not especially intimate. affectionChange must be 0 to 3.
          3. Negative: touches one of her dislikes, misunderstands her, ignores her boundary, teases badly, is too pushy, or says something emotionally careless. affectionChange must be -8 to -2.
        - The negative choice must still sound like natural player dialogue, not cartoonishly evil.
        - The negative choice must relate to her listed dislikes or current emotional context.
        - expectedMood must describe the heroine reaction this choice is likely to cause.
        - affectionChange must be between -8 and 12.
        - isMiniGame must always be false.
        - No markdown. No extra explanation.
        """;
    }

    private static string BuildNextTurnPrompt(Character character, DialogueNode node, Choice selectedChoice, int affection, IReadOnlyList<string> roundHistory)
    {
        var personalityPrompt = CharacterProfileService.GetPersonalityPrompt(character.Name);
        var historyText = FormatRoundHistory(roundHistory);
        var usedChoicesText = FormatUsedChoices(roundHistory);

        return $$"""
        You continue a Thai dating visual novel scene and choose the heroine emotional mood.

        Heroine personality:
        {{personalityPrompt}}

        Heroine: {{character.Name}}
        Current affection after this choice: {{affection}}
        Previous heroine line: "{{node.Text}}"
        Player selected choice: "{{selectedChoice.Text}}"
        Choice affection change: {{selectedChoice.AffectionChange}}
        Generation seed for variety: {{Guid.NewGuid():N}}

        Conversation so far in this 6-turn round:
        {{historyText}}

        Player choices already used this round:
        {{usedChoicesText}}

        Allowed moodTag values:
        neutral, happy, shy, sad, angry, romantic, thinking, surprised, annoyed, confused, sleepy, reading

        Return JSON only:
        {
          "reply": "Thai spoken dialogue from the heroine, 1-3 sentences",
          "moodTag": "shy",
          "choices": [
            {
              "text": "Thai player spoken dialogue line",
              "affectionChange": 5,
              "isMiniGame": false,
              "expectedMood": "happy"
            }
          ]
        }

        Mood rules:
        - If the player is caring, respectful, funny in a kind way, or raises affection, use happy, shy, thinking, or romantic.
        - If the player is cold, rude, careless, touches a listed dislike, violates a boundary, or lowers affection, use sad, angry, annoyed, or confused.
        - If the player surprises her, use surprised or thinking.
        - If affectionChange is 9 or higher, strongly consider romantic or shy.
        - If affectionChange is below 0, avoid happy and romantic unless the reply is bittersweet.

        Dialogue rules:
        - This is turn-by-turn continuity inside one 6-turn round. Treat the conversation so far as canon.
        - Continue the same situation like one connected scene. Do not restart the setting or introduce an unrelated new situation.
        - reply must sound like {{character.Name}}.
        - reply must be direct spoken dialogue from {{character.Name}} to the player.
        - reply must directly react to the player's selected choice.
        - The first sentence of reply must naturally answer the meaning or emotion of the selected choice.
        - Do not quote or repeat the selected choice verbatim.
        - Do not start with formulaic phrases like "ที่คุณบอกว่า", "ที่คุณพูดว่า", "คำว่า", or "เรื่องที่คุณพูด".
        - Make it feel like two people talking, not like summarizing what the player chose.
        - Do not change topic suddenly. Continue from the previous heroine line and selected player choice.
        - Do not write narration, actions, stage directions, or descriptions in reply.
        - Do not write "{{character.Name}} smiles", "{{character.Name}} looks", or any third-person narration.
        - The reply must be speakable aloud as the heroine's own words.
        - Write exactly 3 new spoken choices in Thai for the next player response.
        - The next choices must be natural responses to {{character.Name}}'s new reply.
        - Do not generate choices that ignore what {{character.Name}} just said.
        - Do not repeat or closely paraphrase any player choice already used this round.
        - Each new choice should move the current situation forward in a different way.
        - Each choice text must be direct speech from the player to {{character.Name}}.
        - Do not write actions, narration, stage directions, or descriptions of tone.
        - Do not write text like "ยิ้มให้", "เดินเข้าไป", "ถามว่า", "พูดกับ", or "ชวนคุย".
        - Do not repeat the same choice wording from earlier turns.
        - The next choices should fit the chosen mood and scene.
        - Generate exactly 3 player choices with these roles:
          1. Positive: respects her personality, likes, current emotion, or boundaries. affectionChange must be 5 to 12.
          2. Neutral: polite and natural but not especially intimate. affectionChange must be 0 to 3.
          3. Negative: touches one of her dislikes, misunderstands her, ignores her boundary, teases badly, is too pushy, or says something emotionally careless. affectionChange must be -8 to -2.
        - The negative choice must still sound like natural player dialogue, not cartoonishly evil.
        - The negative choice must relate to her listed dislikes or current emotional context.
        - affectionChange must be between -8 and 12.
        - isMiniGame must always be false.
        - No markdown. No extra explanation.
        """;
    }

    private static List<Choice> NormalizeChoices(IReadOnlyList<AiChoice>? choices)
    {
        var normalized = choices?
            .Where(choice => !string.IsNullOrWhiteSpace(choice.Text))
            .Take(3)
            .Select(choice => new Choice
            {
                Text = choice.Text.Trim(),
                AffectionChange = Math.Clamp(choice.AffectionChange, -8, 12),
                IsMiniGame = false,
                ExpectedMood = string.IsNullOrWhiteSpace(choice.ExpectedMood) ? "neutral" : choice.ExpectedMood.Trim(),
                NextNodeId = GeneratedNextNodeId
            })
            .ToList();

        return normalized is { Count: > 0 } ? normalized : [];
    }

    private static ScenePack NormalizeScenePack(ScenePack? scenePack)
    {
        var turns = scenePack?.Turns?
            .Where(turn => !string.IsNullOrWhiteSpace(turn.Reply) && turn.Choices.Count > 0)
            .Take(6)
            .Select(turn => new SceneTurn
            {
                Reply = turn.Reply.Trim(),
                MoodTag = CleanMoodTag(turn.MoodTag),
                Choices = turn.Choices
                    .Where(choice => !string.IsNullOrWhiteSpace(choice.Text) && !string.IsNullOrWhiteSpace(choice.Reply))
                    .Take(3)
                    .Select(choice => new SceneChoice
                    {
                        Text = choice.Text.Trim(),
                        AffectionChange = Math.Clamp(choice.AffectionChange, -8, 12),
                        IsMiniGame = false,
                        ExpectedMood = CleanMoodTag(choice.ExpectedMood),
                        Reply = choice.Reply.Trim(),
                        MoodTag = CleanMoodTag(choice.MoodTag)
                    })
                    .ToList()
            })
            .ToList() ?? [];

        return new ScenePack { Turns = turns };
    }

    private static string CleanMoodTag(string moodTag)
    {
        return string.IsNullOrWhiteSpace(moodTag) ? "neutral" : moodTag.Trim();
    }

    private static string CleanJson(string text)
    {
        var cleaned = text.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty)
            .Trim();

        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            cleaned = cleaned[firstBrace..(lastBrace + 1)];
        }

        return cleaned;
    }

    private bool TryDeserializeJson<T>(string text, out T? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(CleanJson(text), _jsonOptions);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    private bool TryParseAiJson<T>(string text, out T? result)
    {
        if (TryDeserializeJson(text, out result))
            return true;

        if (typeof(T) == typeof(AiTurnResponse) && TryParseFlexibleTurn(text, out var turn))
        {
            result = (T)(object)turn;
            return true;
        }

        if (typeof(T) == typeof(AiChoiceResponse) && TryParseFlexibleChoices(text, out var choiceResponse))
        {
            result = (T)(object)choiceResponse;
            return true;
        }

        result = default;
        return false;
    }

    private static bool TryParseFlexibleTurn(string text, out AiTurnResponse response)
    {
        response = new AiTurnResponse();

        if (!TryGetJsonRoot(text, out var root))
            return false;

        response.Reply = GetString(root, "reply", "Reply", "dialogue", "line", "text");
        response.MoodTag = GetString(root, "moodTag", "mood", "emotion");
        response.Choices = ReadChoices(root);

        return !string.IsNullOrWhiteSpace(response.Reply)
            || !string.IsNullOrWhiteSpace(response.MoodTag)
            || response.Choices.Count > 0;
    }

    private static bool TryParseFlexibleChoices(string text, out AiChoiceResponse response)
    {
        response = new AiChoiceResponse();

        if (!TryGetJsonRoot(text, out var root))
            return false;

        response.Choices = ReadChoices(root);
        return response.Choices.Count > 0;
    }

    private static bool TryGetJsonRoot(string text, out JsonElement root)
    {
        root = default;
        var cleaned = CleanJson(text);

        try
        {
            using var document = JsonDocument.Parse(cleaned);
            root = document.RootElement.Clone();
            return true;
        }
        catch
        {
            try
            {
                var unquoted = JsonSerializer.Deserialize<string>(text);
                if (string.IsNullOrWhiteSpace(unquoted))
                    return false;

                using var document = JsonDocument.Parse(CleanJson(unquoted));
                root = document.RootElement.Clone();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private static List<AiChoice> ReadChoices(JsonElement root)
    {
        if (!TryGetProperty(root, out var choicesElement, "choices", "Choices", "playerChoices", "options", "responses"))
            return [];

        if (choicesElement.ValueKind != JsonValueKind.Array)
            return [];

        var choices = new List<AiChoice>();
        foreach (var item in choicesElement.EnumerateArray())
        {
            var choice = new AiChoice
            {
                Text = ReadChoiceText(item),
                AffectionChange = GetInt(item, "affectionChange", "affection", "score", "points"),
                IsMiniGame = GetBool(item, "isMiniGame", "miniGame"),
                ExpectedMood = GetString(item, "expectedMood", "mood", "reactionMood")
            };

            if (!string.IsNullOrWhiteSpace(choice.Text))
            {
                choices.Add(choice);
            }
        }

        return choices;
    }

    private static string ReadChoiceText(JsonElement item)
    {
        return item.ValueKind == JsonValueKind.String
            ? item.GetString() ?? string.Empty
            : GetString(item, "text", "Text", "dialogue", "line", "choice");
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names))
            return string.Empty;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static int GetInt(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names))
            return 0;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            return value;

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
            return value;

        return 0;
    }

    private static bool GetBool(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names))
            return false;

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(property.GetString(), out var value) && value,
            _ => false
        };
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names)
    {
        property = default;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out property))
                return true;
        }

        foreach (var item in element.EnumerateObject())
        {
            if (names.Any(name => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                property = item.Value;
                return true;
            }
        }

        return false;
    }

    private static string ShortenForError(string text)
    {
        var cleaned = string.IsNullOrWhiteSpace(text) ? "(empty)" : text.Trim();
        const int maxLength = 260;
        return cleaned.Length <= maxLength ? cleaned : $"{cleaned[..maxLength]}...";
    }

    private static string BuildJsonRepairPrompt<T>(string brokenText)
    {
        var schema = typeof(T) switch
        {
            { } type when type == typeof(ScenePack) => """
              {
                "turns": [
                  {
                    "reply": "Thai spoken dialogue from the heroine, 1-3 sentences",
                    "moodTag": "neutral",
                    "choices": [
                      {
                        "text": "Thai player spoken dialogue line",
                        "affectionChange": 5,
                        "isMiniGame": false,
                        "expectedMood": "happy",
                        "reply": "Thai spoken dialogue from the heroine reacting to this exact choice",
                        "moodTag": "happy"
                      }
                    ]
                  }
                ]
              }
              """,
            { } type when type == typeof(AiTurnResponse) => """
              {
                "reply": "Thai spoken dialogue from the heroine, 1-3 sentences",
                "moodTag": "neutral",
                "choices": [
                  {
                    "text": "Thai player spoken dialogue line",
                    "affectionChange": 5,
                    "isMiniGame": false,
                    "expectedMood": "happy"
                  }
                ]
              }
              """,
            _ => """
              {
                "choices": [
                  {
                    "text": "Thai player spoken dialogue line",
                    "affectionChange": 5,
                    "isMiniGame": false,
                    "expectedMood": "happy"
                  }
                ]
              }
              """
        };

        return $$"""
        Convert the following model output into exactly one valid JSON object for this schema.
        Preserve the Thai meaning if possible. Do not invent a fallback story. Return JSON only.

        Required schema:
        {{schema}}

        Model output:
        {{brokenText}}
        """;
    }

    private static string BuildRetryPrompt(string originalPrompt, string taskName, string issue)
    {
        return $$"""
        Your previous response for this task was incomplete or invalid: {{issue}}

        Generate the task again from scratch. Do not use fallback text. Do not apologize.
        Task: {{taskName}}

        Return exactly one valid JSON object that follows the original schema.
        Original prompt:
        {{originalPrompt}}
        """;
    }

    private static string DescribeTurnIssue(AiTurnResponse? result, IReadOnlyList<Choice> choices)
    {
        if (result == null)
            return "Response object was null.";

        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(result.Reply))
            issues.Add("Missing reply.");

        if (string.IsNullOrWhiteSpace(result.MoodTag))
            issues.Add("Missing moodTag.");

        if (choices.Count != 3)
            issues.Add($"Expected 3 choices, got {choices.Count}.");
        else
        {
            var roleIssue = DescribeChoiceRolesIssue(choices);
            if (!string.IsNullOrWhiteSpace(roleIssue))
            {
                issues.Add(roleIssue);
            }
        }

        return string.Join(" ", issues);
    }

    private static string DescribeChoicesIssue(IReadOnlyList<Choice> choices)
    {
        if (choices.Count != 3)
            return $"Expected 3 choices, got {choices.Count}.";

        return DescribeChoiceRolesIssue(choices);
    }

    private static string DescribeScenePackIssue(ScenePack? scenePack)
    {
        if (scenePack == null)
            return "Response object was null.";

        var issues = new List<string>();

        if (scenePack.Turns.Count != 6)
        {
            issues.Add($"Expected 6 turns, got {scenePack.Turns.Count}.");
        }

        for (var turnIndex = 0; turnIndex < scenePack.Turns.Count; turnIndex++)
        {
            var turn = scenePack.Turns[turnIndex];
            if (string.IsNullOrWhiteSpace(turn.Reply))
                issues.Add($"Turn {turnIndex + 1} is missing reply.");

            if (string.IsNullOrWhiteSpace(turn.MoodTag))
                issues.Add($"Turn {turnIndex + 1} is missing moodTag.");

            if (turn.Choices.Count != 3)
            {
                issues.Add($"Turn {turnIndex + 1} expected 3 choices, got {turn.Choices.Count}.");
                continue;
            }

            var roleChoices = turn.Choices
                .Select(choice => new Choice { Text = choice.Text, AffectionChange = choice.AffectionChange })
                .ToList();

            var roleIssue = DescribeChoiceRolesIssue(roleChoices);
            if (!string.IsNullOrWhiteSpace(roleIssue))
            {
                issues.Add($"Turn {turnIndex + 1}: {roleIssue}");
            }

            for (var choiceIndex = 0; choiceIndex < turn.Choices.Count; choiceIndex++)
            {
                var choice = turn.Choices[choiceIndex];
                if (string.IsNullOrWhiteSpace(choice.Text))
                    issues.Add($"Turn {turnIndex + 1} choice {choiceIndex + 1} is missing text.");

                if (string.IsNullOrWhiteSpace(choice.Reply))
                    issues.Add($"Turn {turnIndex + 1} choice {choiceIndex + 1} is missing reply.");

                if (string.IsNullOrWhiteSpace(choice.MoodTag))
                    issues.Add($"Turn {turnIndex + 1} choice {choiceIndex + 1} is missing moodTag.");
            }
        }

        return string.Join(" ", issues);
    }

    private static string DescribeChoiceRolesIssue(IReadOnlyList<Choice> choices)
    {
        var hasPositiveChoice = choices.Any(choice => choice.AffectionChange >= 5);
        var hasNeutralChoice = choices.Any(choice => choice.AffectionChange is >= 0 and <= 3);
        var hasNegativeChoice = choices.Any(choice => choice.AffectionChange <= -2);

        var issues = new List<string>();
        if (!hasPositiveChoice)
            issues.Add("Missing positive choice with affectionChange 5 to 12.");

        if (!hasNeutralChoice)
            issues.Add("Missing neutral choice with affectionChange 0 to 3.");

        if (!hasNegativeChoice)
            issues.Add("Missing negative choice with affectionChange -8 to -2.");

        return string.Join(" ", issues);
    }

    private static string FormatRoundHistory(IReadOnlyList<string> roundHistory)
    {
        if (roundHistory.Count == 0)
            return "- No previous lines yet.";

        return string.Join("\n", roundHistory.Select(line => $"- {line}"));
    }

    private static string FormatUsedChoices(IReadOnlyList<string> roundHistory)
    {
        var usedChoices = roundHistory
            .Where(line => line.StartsWith("Player:", StringComparison.OrdinalIgnoreCase))
            .Select(line => line["Player:".Length..].Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return usedChoices.Count == 0
            ? "- No previous player choices yet."
            : string.Join("\n", usedChoices.Select(choice => $"- {choice}"));
    }

    public sealed class ScenePack
    {
        public List<SceneTurn> Turns { get; set; } = [];
    }

    public sealed class SceneTurn
    {
        public string Reply { get; set; } = string.Empty;
        public string MoodTag { get; set; } = "neutral";
        public List<SceneChoice> Choices { get; set; } = [];
    }

    public sealed class SceneChoice
    {
        public string Text { get; set; } = string.Empty;
        public int AffectionChange { get; set; }
        public bool IsMiniGame { get; set; }
        public string ExpectedMood { get; set; } = "neutral";
        public string Reply { get; set; } = string.Empty;
        public string MoodTag { get; set; } = "neutral";
    }

    private class AiChoiceResponse
    {
        public List<AiChoice> Choices { get; set; } = [];
    }

    private sealed class AiTurnResponse : AiChoiceResponse
    {
        public string Reply { get; set; } = string.Empty;
        public string MoodTag { get; set; } = string.Empty;
    }

    private sealed class AiChoice
    {
        public string Text { get; set; } = string.Empty;
        public int AffectionChange { get; set; }
        public bool IsMiniGame { get; set; }
        public string ExpectedMood { get; set; } = string.Empty;
    }

    private sealed class GroqChatResponse
    {
        public List<GroqChoice> Choices { get; set; } = [];
    }

    private sealed class GroqChoice
    {
        public GroqMessage Message { get; set; } = new();
    }

    private sealed class GroqMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
