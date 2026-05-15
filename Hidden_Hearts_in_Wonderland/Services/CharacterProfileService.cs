using Hidden_Hearts_in_Wonderland.Models;

namespace Hidden_Hearts_in_Wonderland.Services;

public static class CharacterProfileService
{
    private static readonly List<Character> CharacterProfiles =
    [
        new Character
        {
            Name = "Luna",
            Image = "luna.png",
            StartNodeId = "start_luna",
            PersonalityPrompt = "Luna เป็นสาวอ่อนโยน ขี้อาย ชอบอ่านหนังสือและชงชา พูดสุภาพ อบอุ่น แต่จะเปิดใจช้า ชอบคนที่ใจเย็นและใส่ใจรายละเอียดเล็ก ๆ"
        },
        new Character
        {
            Name = "Alice",
            Image = "alice.png",
            StartNodeId = "start_alice",
            PersonalityPrompt = "Alice เป็นสาวมั่นใจ ฉลาด พูดตรง มีความเป็นผู้นำและชอบท้าทายผู้เล่น เธอชอบคำตอบที่กล้าหาญ ซื่อสัตย์ และมีไหวพริบ"
        },
        new Character
        {
            Name = "Ryne",
            Image = "ryne_smile.png",
            StartNodeId = "start_ryne",
            PersonalityPrompt = "Ryne เป็นสาวสดใส แข็งแรง รักการผจญภัย ใจดีแบบลุย ๆ พูดเป็นกันเอง ชอบคนที่จริงใจ สนุกกับสถานการณ์ และพร้อมช่วยเหลือคนอื่น"
        },
        new Character
        {
            Name = "Raven",
            Image = "raven_smile.png",
            StartNodeId = "start_raven",
            PersonalityPrompt = "Raven เป็นสาวเงียบ ขรึม ลึกลับ ชอบสังเกตมากกว่าพูด แต่ข้างในอ่อนโยน เธอชอบคำตอบที่เคารพพื้นที่ส่วนตัว ไม่เร่งรัด และจริงใจ"
        }
    ];

    // ส่งรายชื่อตัวละครทั้งหมดให้หน้าเลือกตัวละคร
    public static IReadOnlyList<Character> GetCharacters() => CharacterProfiles;

    public static Character GetByName(string name)
    {
        // หาโปรไฟล์จากชื่อ ถ้าไม่เจอกลับไปใช้ Luna เป็นค่า default
        return CharacterProfiles.FirstOrDefault(character =>
            string.Equals(character.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? CharacterProfiles[0];
    }

    public static Character GetByStartNodeId(string startNodeId)
    {
        // ใช้ย้อนจาก start node ไปหาว่าฉากนี้เป็นของตัวละครไหน
        return CharacterProfiles.FirstOrDefault(character =>
            string.Equals(character.StartNodeId, startNodeId, StringComparison.OrdinalIgnoreCase))
            ?? CharacterProfiles[0];
    }

    public static string GetPersonalityPrompt(string characterName)
    {
        // prompt บุคลิกหลักของแต่ละคน ส่งให้ AI เพื่อให้บทพูดไม่ออกมาเหมือนกันหมด
        return characterName.ToLowerInvariant() switch
        {
            "alice" => """
                Alice personality:
                - Confident, playful, sharp-tongued, curious, and likes witty conversation.
                - She enjoys being challenged if the player is respectful and clever.
                - Likes: confidence, honesty, playful teasing, courage, curiosity, and people who keep up with her pace.
                - Dislikes: boring answers, passive behavior, being treated like she is fragile, dishonesty, and pushy flirting without tact.
                - Negative choices for Alice should sound like the player is dull, dismissive, dishonest, or too arrogant in a way that irritates her.
                """,
            "ryne" => """
                Ryne personality:
                - Energetic, brave, friendly, straightforward, and protective.
                - She likes adventure and honest action more than overthinking.
                - Likes: encouragement, courage, playful energy, teamwork, direct kindness, and sharing small adventures.
                - Dislikes: cowardice, laziness, mocking her enthusiasm, ignoring danger, and treating her like she is reckless or childish.
                - Negative choices for Ryne should sound like the player kills the mood, doubts her too harshly, or refuses to take her feelings seriously.
                """,
            "raven" => """
                Raven personality:
                - Quiet, mysterious, guarded, observant, and slow to trust.
                - She softens when the player respects her boundaries and notices small emotions.
                - Likes: patience, calm honesty, privacy, subtle compliments, emotional intelligence, and being trusted without pressure.
                - Dislikes: prying questions, loud pressure, shallow flirting, jokes about her secrecy, and forcing her to explain herself.
                - Negative choices for Raven should sound like the player pushes too hard, invades her privacy, or treats her silence as a problem.
                """,
            _ => """
                Luna personality:
                - Gentle, shy, warm, observant, and quietly affectionate.
                - She wants to be understood without being rushed.
                - Likes: soft encouragement, patience, honest compliments, being asked about her feelings, gentle humor, and quiet companionship.
                - Dislikes: being rushed, being teased about her ears, loud pressure, being treated like a mascot, careless jokes, and people ignoring her quiet signals.
                - Negative choices for Luna should sound like the player is careless, too pushy, dismisses her worry, or teases a sensitive point.
                """
        };
    }

    public static IReadOnlyList<string> GetImageNames(string characterName)
    {
        // เอาไว้ดึงชื่อไฟล์รูปทั้งหมดของตัวละครนั้น
        return GetImageMoodOptions(characterName).Keys.ToList();
    }

    public static IReadOnlyDictionary<string, string> GetImageMoodOptions(string characterName)
    {
        // map รูปกับอารมณ์ เพื่อให้ AI/ระบบเลือกภาพที่ใกล้กับประโยคที่สุด
        return characterName.ToLowerInvariant() switch
        {
            "alice" => new Dictionary<string, string>
            {
                ["alice.png"] = "neutral, confident, curious",
                ["alice_smile.png"] = "friendly, amused, pleased",
                ["alice_shy.png"] = "shy, flustered, secretly happy",
                ["alice_sad.png"] = "hurt, disappointed, lonely",
                ["alice_angry.png"] = "angry, challenged, defensive",
                ["alice_inlove.png"] = "romantic, deeply touched, affectionate",
                ["alice_serious.png"] = "serious, focused, testing the player",
                ["alice_bored.png"] = "bored, unimpressed, waiting for something interesting"
            },
            "ryne" => new Dictionary<string, string>
            {
                ["ryne.png"] = "neutral, energetic, ready for adventure",
                ["ryne_smile.png"] = "happy, friendly, excited",
                ["ryne_shy.png"] = "shy, bashful, unexpectedly touched",
                ["ryne_sad.png"] = "sad, worried, emotionally softened",
                ["ryne_inlove.png"] = "romantic, charmed, openly affectionate",
                ["ryne_sleepy.png"] = "sleepy, relaxed, low energy",
                ["ryne_think.png"] = "thinking, curious, considering the player's words",
                ["ryne_surprise.png"] = "surprised, startled, impressed",
                ["ryne_strong_gaze.png"] = "determined, brave, protective"
            },
            "raven" => new Dictionary<string, string>
            {
                ["raven.png"] = "neutral, quiet, mysterious",
                ["raven_smile.png"] = "soft smile, quietly pleased, gentle trust",
                ["raven_shy.png"] = "shy, guarded but touched, vulnerable",
                ["raven_sad.png"] = "sad, distant, emotionally hurt",
                ["raven_inlove.png"] = "romantic, deeply moved, tender",
                ["raven_sleepy.png"] = "sleepy, calm, softened guard",
                ["raven_annoyed.png"] = "annoyed, suspicious, boundary pushed",
                ["raven_bored.png"] = "bored, unimpressed, emotionally distant"
            },
            _ => new Dictionary<string, string>
            {
                ["luna.png"] = "neutral, gentle, calm",
                ["luna_smile.png"] = "warm smile, happy, comfortable",
                ["luna_shy.png"] = "shy, flustered, sweetly embarrassed",
                ["luna_sad.png"] = "sad, worried, emotionally hurt",
                ["luna_inlove.png"] = "romantic, deeply touched, affectionate",
                ["luna_reading.png"] = "quiet, thoughtful, bookish, focused",
                ["luna_confused.png"] = "confused, unsure, trying to understand",
                ["luna_thinking.png"] = "thinking, curious, uncertain",
                ["luna_sleepy.png"] = "sleepy, relaxed, soft and low energy",
                ["luna_surprised.png"] = "surprised, startled, caught off guard",
                ["luna_angry.png"] = "angry, upset, defensive"
            }
        };
    }

    public static string GetImageMoodPrompt(string characterName)
    {
        // แปลง mood options เป็นข้อความอ่านง่ายสำหรับใส่ใน prompt
        return string.Join("\n", GetImageMoodOptions(characterName)
            .Select(image => $"- {image.Key}: {image.Value}"));
    }

    public static string GetFallbackImage(string characterName, int affectionChange)
    {
        // ถ้า moodTag ที่ได้มาไม่รู้จัก ให้เดาภาพจากคะแนน affection แทน
        return characterName.ToLowerInvariant() switch
        {
            "alice" when affectionChange >= 9 => "alice_inlove.png",
            "alice" when affectionChange >= 4 => "alice_smile.png",
            "alice" when affectionChange < 0 => "alice_angry.png",
            "alice" => "alice_serious.png",

            "ryne" when affectionChange >= 9 => "ryne_inlove.png",
            "ryne" when affectionChange >= 4 => "ryne_smile.png",
            "ryne" when affectionChange < 0 => "ryne_sad.png",
            "ryne" => "ryne_think.png",

            "raven" when affectionChange >= 9 => "raven_inlove.png",
            "raven" when affectionChange >= 4 => "raven_smile.png",
            "raven" when affectionChange < 0 => "raven_annoyed.png",
            "raven" => "raven.png",

            _ when affectionChange >= 9 => "luna_inlove.png",
            _ when affectionChange >= 4 => "luna_smile.png",
            _ when affectionChange < 0 => "luna_sad.png",
            _ => "luna_thinking.png"
        };
    }

    public static string GetImageForMood(string characterName, string moodTag, int affectionChange)
    {
        // เลือกรูปตาม moodTag ก่อน ถ้า tag แปลกค่อย fallback ด้วยคะแนน affection
        var mood = moodTag.Trim().ToLowerInvariant();

        return characterName.ToLowerInvariant() switch
        {
            "alice" => mood switch
            {
                "neutral" => "alice.png",
                "happy" => "alice_smile.png",
                "shy" => "alice_shy.png",
                "sad" => "alice_sad.png",
                "angry" => "alice_angry.png",
                "romantic" => "alice_inlove.png",
                "thinking" => "alice_serious.png",
                "surprised" => "alice_serious.png",
                "annoyed" => "alice_bored.png",
                _ => GetFallbackImage(characterName, affectionChange)
            },
            "ryne" => mood switch
            {
                "neutral" => "ryne.png",
                "happy" => "ryne_smile.png",
                "shy" => "ryne_shy.png",
                "sad" => "ryne_sad.png",
                "angry" => "ryne_strong_gaze.png",
                "romantic" => "ryne_inlove.png",
                "thinking" => "ryne_think.png",
                "surprised" => "ryne_surprise.png",
                "annoyed" => "ryne_sad.png",
                "sleepy" => "ryne_sleepy.png",
                _ => GetFallbackImage(characterName, affectionChange)
            },
            "raven" => mood switch
            {
                "neutral" => "raven.png",
                "happy" => "raven_smile.png",
                "shy" => "raven_shy.png",
                "sad" => "raven_sad.png",
                "angry" => "raven_annoyed.png",
                "romantic" => "raven_inlove.png",
                "thinking" => "raven.png",
                "surprised" => "raven_shy.png",
                "annoyed" => "raven_annoyed.png",
                "sleepy" => "raven_sleepy.png",
                _ => GetFallbackImage(characterName, affectionChange)
            },
            _ => mood switch
            {
                "neutral" => "luna.png",
                "happy" => "luna_smile.png",
                "shy" => "luna_shy.png",
                "sad" => "luna_sad.png",
                "angry" => "luna_angry.png",
                "romantic" => "luna_inlove.png",
                "thinking" => "luna_thinking.png",
                "surprised" => "luna_surprised.png",
                "confused" => "luna_confused.png",
                "sleepy" => "luna_sleepy.png",
                "reading" => "luna_reading.png",
                _ => GetFallbackImage(characterName, affectionChange)
            }
        };
    }
}
