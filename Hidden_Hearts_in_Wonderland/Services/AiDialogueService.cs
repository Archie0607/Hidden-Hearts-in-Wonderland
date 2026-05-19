using System.Text;
using System.Text.Json;
using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public class AiDialogueService
{
    public const string GeneratedNextNodeId = "__ai_next__";

    // ใส่ key ผ่าน GROQ_API_KEYS จะปลอดภัยสุด เพราะไม่ต้องเก็บ key จริงไว้ในโค้ด
    // DevApiKeys เอาไว้ลองเครื่องตัวเองชั่วคราวเท่านั้น ถ้าจะอัปขึ้น git ให้ปล่อยว่างไว้แบบนี้
    private static readonly string[] DevApiKeys =
    [
        "",
        "",
        "",
    ];
    private const string Model = "llama-3.3-70b-versatile";
    private static readonly Uri Endpoint = new("https://api.groq.com/openai/v1/chat/completions");

    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ScenePack> GenerateScenePackAsync(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        // เจนทั้งรอบไว้ทีเดียว จะได้คุมโทนและความต่อเนื่องของบทสนทนาได้ดีกว่าเจนทีละช้อยแบบสุ่ม ๆ
        var prompt = BuildScenePackPrompt(character, templateNode, affection, roundId);
        return await RequestCompleteScenePackAsync(prompt, "6-turn scene pack with heroine replies and exactly 3 player choices per turn");
    }

    public async Task<DialogueNode> GenerateOpeningNodeAsync(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        // โหมดเก่าแบบเจน opening เดี่ยว ยังเก็บไว้เผื่อมีหน้าอื่นเรียกใช้
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
        // ใช้สร้างเฉพาะช้อยจากประโยคล่าสุด ถ้า flow ไหนไม่ได้ใช้ scene pack ทั้งรอบ
        var prompt = BuildChoicesPrompt(character, node, affection);
        return await RequestCompleteChoicesAsync(prompt, "exactly 3 player choices");
    }

    public async Task<DialogueNode> GenerateNextNodeAsync(Character character, DialogueNode currentNode, Choice selectedChoice, int affection, IReadOnlyList<string>? roundHistory = null)
    {
        // โหมดเจนเทิร์นถัดไปทันที ใช้ประวัติบทสนทนาช่วยให้ AI ไม่หลุดฉาก
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

        // ให้ AI มีโอกาสแก้งานตัวเองอีกนิด เพราะบางครั้งตอบ JSON ไม่ครบหรือช้อยไม่ครบ 3 อัน
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

        // ช้อยต้องครบ 3 แบบเสมอ คือดี กลาง และพลาดนิด ๆ ตามคะแนน affection
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

        // รอบหลักของเกมต้องครบ 6 เทิร์น ถ้า AI ส่งมาไม่ครบจะขอใหม่ทันที
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

        // ถ้าเนื้อหาดีแต่ JSON พังนิดหน่อย ให้ AI ช่วยจัดกลับเข้ารูปแบบเดิมก่อนทิ้ง error
        var repairedText = await RequestAiTextAsync(BuildJsonRepairPrompt<T>(text), 0.2);
        if (TryParseAiJson(repairedText, out parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Groq returned JSON that could not be parsed after repair. Raw: {ShortenForError(text)}");
    }

    private async Task<string> RequestAiTextAsync(string prompt, double temperature)
    {
        // จุดเดียวที่ยิงไปหา Groq จริง ๆ เมธอดอื่นจะเตรียม prompt กับเช็กผลลัพธ์ก่อน/หลังเรียกตรงนี้
        var apiKeys = GetConfiguredApiKeys();

        if (apiKeys.Count == 0)
            throw new InvalidOperationException("Groq API key is not configured. Set GROQ_API_KEYS or add a temporary dev key in DevApiKeys before starting the app.");

        var systemPrompt = """
        You are a JSON-only writer for an English fantasy dating visual novel.
        Return one valid JSON object only.
        All user-visible dialogue values must be natural English only.
        Do not use Thai, Japanese, Chinese, Korean, mojibake, romanized Thai, emoji, markdown, or garbled characters in dialogue.
        """;
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
        var bodyJson = JsonSerializer.Serialize(body);
        var failedKeyIndexes = new HashSet<int>();
        var lastQuotaError = string.Empty;

        // ถ้ามีหลาย key จะลองไล่ไปทีละอัน เผื่อบาง key quota หมดหรือโดน rate limit
        for (var keyIndex = 0; keyIndex < apiKeys.Count; keyIndex++)
        {
            if (failedKeyIndexes.Contains(keyIndex))
                continue;

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKeys[keyIndex]}");
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                if (IsOrganizationRestrictedError(json))
                {
                    throw new InvalidOperationException("Groq rejected this API key because the organization is restricted (organization_restricted). Use a key from an active Groq organization or fix the organization status in the Groq Console.");
                }

                if (IsQuotaOrRateLimitError(response, json) && keyIndex < apiKeys.Count - 1)
                {
                    failedKeyIndexes.Add(keyIndex);
                    lastQuotaError = $"Key #{keyIndex + 1}: {(int)response.StatusCode} {response.ReasonPhrase}. {ShortenForError(json)}";
                    continue;
                }

                if (IsQuotaOrRateLimitError(response, json))
                    throw new InvalidOperationException($"Groq quota/rate limit failed for all API keys. Last issue: {(int)response.StatusCode} {response.ReasonPhrase}. {ExtractGroqErrorMessage(json)}");

                throw new InvalidOperationException($"Groq request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {ExtractGroqErrorMessage(json)}");
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

        throw new InvalidOperationException($"Groq quota/rate limit failed for all API keys. Last issue: {lastQuotaError}");
    }

    private static List<string> GetConfiguredApiKeys()
    {
        var environmentKeys = Environment.GetEnvironmentVariable("GROQ_API_KEYS") ?? string.Empty;
        // รองรับหลาย key ใน environment เดียว คั่นด้วย ; , หรือขึ้นบรรทัดใหม่ก็ได้
        return environmentKeys
            .Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(DevApiKeys)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct()
            .ToList();
    }

    private static bool IsOrganizationRestrictedError(string responseBody)
    {
        // error แบบนี้เปลี่ยน key อย่างเดียวอาจไม่พอ ต้องแก้สถานะ organization ใน Groq ด้วย
        return responseBody.Contains("organization_restricted", StringComparison.OrdinalIgnoreCase)
            || responseBody.Contains("Organization has been restricted", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractGroqErrorMessage(string responseBody)
    {
        // ดึง error ให้อ่านง่ายขึ้น ไม่งั้น dialog จะโชว์ JSON ยาว ๆ จนผู้เล่นงง
        if (string.IsNullOrWhiteSpace(responseBody))
            return "Groq did not return an error body.";

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.Object)
            {
                var message = TryGetStringProperty(error, "message");
                var code = TryGetStringProperty(error, "code");
                var type = TryGetStringProperty(error, "type");

                var parts = new[] { message, code, type }
                    .Where(part => !string.IsNullOrWhiteSpace(part));

                return string.Join(" | ", parts);
            }
        }
        catch (JsonException)
        {
            // Fall back to a shortened raw body below.
        }

        return ShortenForError(responseBody);
    }

    private static string TryGetStringProperty(JsonElement element, string name)
    {
        // อ่าน property string จาก JSON แบบปลอดภัย ถ้าไม่มีให้คืนค่าว่าง
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool IsQuotaOrRateLimitError(HttpResponseMessage response, string responseBody)
    {
        // ใช้ตัดสินใจว่าจะลอง key ถัดไปไหม โดยไม่สลับ key ตอนเป็น 401 เพราะมักเป็น key ผิดจริง
        var statusCode = (int)response.StatusCode;
        if (statusCode == 429)
            return true;

        if (statusCode == 401)
            return false;

        var body = responseBody.ToLowerInvariant();
        return body.Contains("quota")
            || body.Contains("rate_limit")
            || body.Contains("rate limit")
            || body.Contains("too many requests")
            || body.Contains("resource_exhausted")
            || body.Contains("insufficient_quota")
            || body.Contains("tokens per minute")
            || body.Contains("tokens per day")
            || body.Contains("token limit");
    }

    private static string BuildScenePackPrompt(Character character, DialogueNode templateNode, int affection, string roundId)
    {
        var personalityPrompt = CharacterProfileService.GetPersonalityPrompt(character.Name);

        // Prompt นี้ตั้งใจให้คำตอบของสาว ๆ เกี่ยวกับช้อยที่เลือก แต่ไม่ต้องเห็นด้วยแบบแข็ง ๆ ทุกครั้ง
        return $$"""
        You write a complete 6-turn conversation scene pack for an English fantasy dating visual novel named Hidden Hearts in Wonderland.

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
              "reply": "English spoken dialogue from the heroine, 1-3 sentences",
              "moodTag": "neutral",
              "choices": [
                {
                  "text": "English player spoken dialogue line",
                  "affectionChange": 5,
                  "isMiniGame": false,
                  "expectedMood": "happy",
                  "reply": "English spoken dialogue from the heroine that naturally continues after this choice",
                  "moodTag": "happy"
                }
              ]
            }
          ]
        }

        Scene pack rules:
        - Generate exactly 6 turns in the turns array.
        - Turn 1 reply is the opening line from {{character.Name}}.
        - Turns 2-6 replies are internal bridge lines for planning continuity. They are not shown immediately after a selected choice.
        - Every turn must continue the same small situation like one connected scene, not restart the conversation.
        - Each turn must have exactly 3 choices.
        - Every choice must have a choice-specific heroine reply that feels related to that player line, but it does not need to agree, repeat, or answer too literally.
        - After the player selects a choice, the game shows only that choice-specific heroine reply, then shows the next turn's shared choices.
        - For turns 2-6, choices must make sense after any previous choice-specific heroine reply, because the internal bridge reply is not shown to the player.
        - Keep every next choice set broad enough that it can follow from positive, neutral, or negative previous replies without feeling like it ignored the player.
        - Do not make a 3^6 branching tree. Generate one linear 6-turn scene with 3 reactive replies per turn.
        - Do not repeat or closely paraphrase any player choice across all 6 turns.
        - Do not copy or paraphrase any fixed starter text from the game data.
        - Avoid making {{character.Name}} read a book unless the selected moodTag is reading.

        Dialogue rules:
        - Write like a soft, slightly shy anime visual novel heroine in natural English.
        - English lines should feel casual, warm, and alive, with gentle hesitation when appropriate, such as "um", "well", "I mean", or "thank you".
        - User-visible text in reply, choices.text, and choices.reply must contain English language only.
        - Do not use Thai, romanized Thai, Japanese, Chinese, Korean, emoji, mojibake, or garbled characters in user-visible text.
        - Avoid stiff, literal, questionnaire-like English. Do not make every line sound like an interview question.
        - Avoid bland compliment loops like "Do you like this place?" -> "I like it too." Add a tiny concrete detail from the scene or feeling.
        - Use short English spoken lines, usually 1-2 sentences. Let the character sound like she is reacting in the moment.
        - Prefer emotionally specific spoken lines over generic sentences.
        - All heroine replies must be direct spoken dialogue from {{character.Name}} to the player.
        - All player choice text must be direct spoken dialogue from the player to {{character.Name}}.
        - Do not write narration, actions, stage directions, descriptions of tone, or third-person text.
        - Do not write "{{character.Name}} smiles", "{{character.Name}} looks", or any third-person narration.
        - The heroine must speak like her personality, likes, dislikes, and current emotion.
        - Every choice should stay related to the heroine line for that turn without sounding like a forced exact answer.
        - Choice-specific replies should continue the same topic or mood naturally, like real flirting or casual conversation.
        - The heroine may tease, hesitate, ask back, deflect shyly, add a new small detail, or change the angle slightly instead of agreeing directly.
        - Avoid starting heroine replies with repeated agreement or confirmation phrases such as "yes", "yeah", "I agree", "you're right", or "that's true".
        - Do not quote or repeat the selected choice verbatim inside the heroine reply.
        - Make the dialogue feel like two people talking, flirting, and finding a rhythm, not like summarizing what the player chose.
        - The next turn choices must feel natural after the previous choice-specific heroine reply, even if the internal bridge reply is not displayed.
        - The larger story is that the player and heroine are preparing to confront the Demon Lord. Mention this main quest naturally in some turns through plans, worries, training, clues, courage, or promises.
        - Keep romance alive alongside the main quest: include gentle flirting, emotional closeness, trust, vulnerability, or teasing that fits the heroine.
        - Do not make every line about the Demon Lord. Blend the quest with the current small scene and the relationship.
        - Write natural English. No markdown. No extra explanation.

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

        // Prompt opening เน้นเปิดสถานการณ์เล็ก ๆ ให้คุยต่อได้ ไม่ใช่ประโยคทักทายลอย ๆ
        return $$"""
        You write a fresh opening line for a new conversation round in an English fantasy dating visual novel named Hidden Hearts in Wonderland.

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
          "reply": "English spoken dialogue from the heroine, 1-3 sentences",
          "moodTag": "neutral",
          "choices": [
            {
              "text": "English player spoken dialogue line",
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
        - Make the first situation concrete: include one small visible detail, feeling, object, or question that the choices can naturally answer.
        - Do not copy or paraphrase any fixed starter text from the game data.
        - Avoid making {{character.Name}} read a book unless the selected moodTag is reading.
        - Keep the same setting mood, but vary the place, action, prop, or first emotional beat.
        - Write exactly 3 choices in English.
        - Every choice should be related to {{character.Name}}'s reply, but not every choice needs to answer it literally.
        - The choices must feel like natural things someone might say while talking or flirting, not generic pickup lines.
        - Choices may reassure, ask a small question, tease gently, admit uncertainty, or invite her to do something in the same scene.
        - Each choice should lead the same situation forward, not jump to a different scene.
        - Every choice text must be direct speech from the player to {{character.Name}}.
        - Do not write actions, narration, stage directions, or descriptions of tone.
        - User-visible text in reply and choices.text must contain English language only.
        - Do not use Thai, romanized Thai, Japanese, Chinese, Korean, emoji, mojibake, or garbled characters.
        - Weave in the main quest naturally: the player and heroine are preparing to confront the Demon Lord.
        - The opening may mention a clue, training, a plan, fear, courage, or a promise about the Demon Lord, while still leaving room for romance.
        - Keep the dating tone active: choices can reassure, flirt gently, invite trust, or show affection while discussing the quest.
        - Good choice text examples: "Then let's take this one step at a time together.", "If the Demon Lord scares you, stay beside me. I won't let you face it alone.", "That light over there looks strange. Want to check it with me?"
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

        // Prompt นี้บังคับให้ช้อยเกี่ยวกับประโยคล่าสุด แต่ยังต้องฟังเหมือนคนตอบกันจริง ๆ
        return $$"""
        You write player choices for an English fantasy dating visual novel named Hidden Hearts in Wonderland.

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
              "text": "English player spoken dialogue line",
              "affectionChange": 5,
              "isMiniGame": false,
              "expectedMood": "happy"
            }
          ]
        }

        Rules:
        - Write exactly 3 choices in English.
        - Every choice must stay related to the latest heroine line.
        - If the heroine asks, worries, hints, or shows emotion, at least one choice should address that context directly; the others may respond more casually or playfully.
        - Do not write generic choices that could fit any scene.
        - Choices can mention the place, object, worry, invitation, or emotion she just mentioned, but do not force every choice to mirror the same words.
        - Do not make all choices simple compliments or agreement. Mix reassurance, curiosity, playful honesty, a small invitation, and one believable mistake.
        - Each choice must be a line of dialogue the player says directly to {{character.Name}}.
        - Do not write actions, narration, stage directions, or descriptions of tone.
        - User-visible text in choices.text must contain English language only.
        - Do not use Thai, romanized Thai, Japanese, Chinese, Korean, emoji, mojibake, or garbled characters.
        - Some choices should naturally connect romance with the main quest against the Demon Lord: trust, protection, bravery, planning, or a promise to return safely.
        - Do not make all choices about the Demon Lord; keep them grounded in the heroine's latest line and current emotion.
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

        // ใช้ตอนให้ AI ต่อบทจากช้อยที่เลือก พร้อมส่ง history ไปกันการพูดวนหรือหลุดหัวข้อ
        return $$"""
        You continue an English fantasy dating visual novel scene and choose the heroine emotional mood.

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
          "reply": "English spoken dialogue from the heroine, 1-3 sentences",
          "moodTag": "shy",
          "choices": [
            {
              "text": "English player spoken dialogue line",
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
        - reply must feel related to the player's selected choice, but it should not sound like a literal confirmation of it.
        - The reply may answer, tease, ask back, shyly deflect, or pick up only one small part of the selected choice.
        - The second sentence may gently move the same situation forward with a small new detail for the next choices.
        - Do not quote or repeat the selected choice verbatim.
        - Do not start with formulaic agreement phrases like "yes", "yeah", "I agree", "you're right", "I understand", "that's true", or "thank you for telling me".
        - User-visible text in reply and choices.text must contain English language only.
        - Do not use Thai, romanized Thai, Japanese, Chinese, Korean, emoji, mojibake, or garbled characters.
        - Make it feel like two people talking, not like summarizing what the player chose.
        - Do not change topic suddenly. Continue from the previous heroine line and selected player choice.
        - Do not write narration, actions, stage directions, or descriptions in reply.
        - Do not write "{{character.Name}} smiles", "{{character.Name}} looks", or any third-person narration.
        - The reply must be speakable aloud as the heroine's own words.
        - Write exactly 3 new spoken choices in English for the next player response.
        - The next choices must be natural things the player might say after {{character.Name}}'s new reply.
        - Do not generate choices that ignore what {{character.Name}} just said.
        - The next choices should stay in the same situation, but they can vary between asking, teasing, inviting, reassuring, or hesitating.
        - Do not make all choices praise her or agree with her. At least one choice should move the conversation forward.
        - Do not repeat or closely paraphrase any player choice already used this round.
        - Each new choice should move the current situation forward in a different way.
        - Each choice text must be direct speech from the player to {{character.Name}}.
        - Do not write actions, narration, stage directions, or descriptions of tone.
        - Do not repeat the same choice wording from earlier turns.
        - The next choices should fit the chosen mood and scene.
        - Keep the main story present: the Demon Lord is the larger threat, and this conversation should sometimes reveal preparation, fear, strategy, hope, or resolve about that quest.
        - Keep the romantic route present too: the heroine can grow closer to the player through trust, vulnerability, teasing, courage, and small promises.
        - The quest should support the romance, not replace it.
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
        // ตรงนี้กรองด่านสุดท้ายก่อนส่งเข้า UI กันภาษาแปลก ๆ หรือช้อยเกิน 3 อันหลุดไปโชว์
        var normalized = choices?
            .Where(choice => IsValidEnglishDialogue(choice.Text))
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
        // Scene pack ต้องสะอาดทั้ง reply และช้อย เพราะอันนี้เป็นบทหลักที่ผู้เล่นจะเห็นทั้งรอบ
        var turns = scenePack?.Turns?
            .Where(turn => IsValidEnglishDialogue(turn.Reply) && turn.Choices.Count > 0)
            .Take(6)
            .Select(turn => new SceneTurn
            {
                Reply = turn.Reply.Trim(),
                MoodTag = CleanMoodTag(turn.MoodTag),
                Choices = turn.Choices
                    .Where(choice => IsValidEnglishDialogue(choice.Text) && IsValidEnglishDialogue(choice.Reply))
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

    private static bool IsValidEnglishDialogue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // ยอมให้มีเลข เว้นวรรค และเครื่องหมายวรรคตอน แต่ตัวหนังสือหลักต้องเป็นอังกฤษเท่านั้น
        var hasEnglishLetter = false;
        foreach (var ch in text)
        {
            if (ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                hasEnglishLetter = true;
                continue;
            }

            if (char.IsLetter(ch))
            {
                return false;
            }

            if (char.IsWhiteSpace(ch)
                || char.IsDigit(ch)
                || char.IsPunctuation(ch))
            {
                continue;
            }

            return false;
        }

        return hasEnglishLetter;
    }

    private static string CleanMoodTag(string moodTag)
    {
        // mood ว่างให้กลับไป neutral ไว้ก่อน ภาพตัวละครจะได้ไม่หา key แปลก ๆ
        return string.IsNullOrWhiteSpace(moodTag) ? "neutral" : moodTag.Trim();
    }

    private static string CleanJson(string text)
    {
        // AI บางรอบชอบครอบ ```json มาให้ เลยตัดออกก่อน parse
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
        // parse แบบปกติก่อน ถ้าไม่ได้ค่อยไปทาง flexible parser ด้านล่าง
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

        // บางที AI เปลี่ยนชื่อ field นิดหน่อย เลยมี parser สำรองไว้ช่วยเก็บงานที่ยังพอใช้ได้
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
        // parser สำรองสำหรับคำตอบหนึ่งเทิร์น เผื่อชื่อ field ไม่ตรง schema เป๊ะ ๆ
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
        // parser สำรองสำหรับเคสที่ต้องการแค่ choices
        response = new AiChoiceResponse();

        if (!TryGetJsonRoot(text, out var root))
            return false;

        response.Choices = ReadChoices(root);
        return response.Choices.Count > 0;
    }

    private static bool TryGetJsonRoot(string text, out JsonElement root)
    {
        // รองรับทั้ง JSON ตรง ๆ และ JSON ที่โดนส่งมาเป็น string ซ้อนอีกชั้น
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
        // รับชื่อ field หลายแบบไว้หน่อย เพราะโมเดลบางทีใช้ options หรือ responses แทน choices
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
        // choice อาจมาเป็น string ตรง ๆ หรือมาเป็น object ที่มี text ข้างใน
        return item.ValueKind == JsonValueKind.String
            ? item.GetString() ?? string.Empty
            : GetString(item, "text", "Text", "dialogue", "line", "choice");
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        // อ่านค่าเป็น string จากชื่อ field หลายแบบ เผื่อ AI ตั้งชื่อไม่ตรงเป๊ะ
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
        // อ่านเลขจาก JSON ได้ทั้งแบบ number และ string
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
        // อ่าน boolean จาก JSON ได้ทั้ง bool ตรง ๆ และ string "true"
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
        // หา property แบบตรงชื่อก่อน แล้วค่อยไล่แบบไม่สนตัวพิมพ์เล็กใหญ่
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
        // จำกัด error ไม่ให้ยาวเกิน dialog ในเกม
        var cleaned = string.IsNullOrWhiteSpace(text) ? "(empty)" : text.Trim();
        const int maxLength = 260;
        return cleaned.Length <= maxLength ? cleaned : $"{cleaned[..maxLength]}...";
    }

    private static string BuildJsonRepairPrompt<T>(string brokenText)
    {
        // ใช้ตอน JSON แตกเท่านั้น ให้ AI ซ่อมรูปทรงข้อมูล ไม่ใช่แต่งบทใหม่ทั้งก้อน
        var schema = typeof(T) switch
        {
            { } type when type == typeof(ScenePack) => """
              {
                "turns": [
                  {
                    "reply": "English spoken dialogue from the heroine, 1-3 sentences",
                    "moodTag": "neutral",
                    "choices": [
                      {
                        "text": "English player spoken dialogue line",
                        "affectionChange": 5,
                        "isMiniGame": false,
                        "expectedMood": "happy",
                        "reply": "English spoken dialogue from the heroine reacting to this exact choice",
                        "moodTag": "happy"
                      }
                    ]
                  }
                ]
              }
              """,
            { } type when type == typeof(AiTurnResponse) => """
              {
                "reply": "English spoken dialogue from the heroine, 1-3 sentences",
                "moodTag": "neutral",
                "choices": [
                  {
                    "text": "English player spoken dialogue line",
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
                    "text": "English player spoken dialogue line",
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
        Preserve the English meaning if possible. Do not invent a fallback story. Return JSON only.

        Required schema:
        {{schema}}

        Model output:
        {{brokenText}}
        """;
    }

    private static string BuildRetryPrompt(string originalPrompt, string taskName, string issue)
    {
        // Retry prompt จะบอกปัญหาตรง ๆ แล้วให้เจนใหม่ตาม schema เดิม
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
        // รวมเหตุผลที่ response ใช้ไม่ได้ เพื่อเอาไปบอก AI ตอน retry
        if (result == null)
            return "Response object was null.";

        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(result.Reply))
            issues.Add("Missing reply.");
        else if (!IsValidEnglishDialogue(result.Reply))
            issues.Add("Reply must contain English dialogue only.");

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
        // ชุดช้อยต้องครบ 3 และต้องมีบทบาทครบตามคะแนนที่เราวางไว้
        if (choices.Count != 3)
            return $"Expected 3 choices, got {choices.Count}.";

        return DescribeChoiceRolesIssue(choices);
    }

    private static string DescribeScenePackIssue(ScenePack? scenePack)
    {
        if (scenePack == null)
            return "Response object was null.";

        // เช็กให้ละเอียดหน่อย เพราะถ้าบทชุดแรกพัง เกมจะพังต่อเนื่องทั้งรอบ
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
        // กันไม่ให้ AI ส่งมาแต่ช้อยดีทั้งหมด เพราะเกมต้องมีผลคะแนนให้เลือกจริง
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
        // แปลง history เป็น bullet สั้น ๆ ให้ prompt อ่านง่าย
        if (roundHistory.Count == 0)
            return "- No previous lines yet.";

        return string.Join("\n", roundHistory.Select(line => $"- {line}"));
    }

    private static string FormatUsedChoices(IReadOnlyList<string> roundHistory)
    {
        // ส่งเฉพาะช้อยที่ผู้เล่นเคยกดไปแล้ว เพื่อกัน AI สร้างคำตอบซ้ำ ๆ
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

