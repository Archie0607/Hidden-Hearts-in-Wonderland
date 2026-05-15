using Hidden_Hearts_in_Wonderland.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Hidden_Hearts_in_Wonderland.Services
{
    public class DialogueService
    {
        public async Task<List<DialogueNode>> LoadDialogueAsync()
        {
            // โหลด dialogue.json จาก resources แล้วแปลงเป็น node สำหรับระบบบทสนทนา
            using var stream = await FileSystem.OpenAppPackageFileAsync("dialogue.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            return JsonSerializer.Deserialize<List<DialogueNode>>(json);
        }
    }
}
