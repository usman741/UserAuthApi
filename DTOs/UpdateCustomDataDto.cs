using System.Text.Json;

namespace UserAuthApi.DTOs;

public class UpdateCustomDataDto
{
    public JsonElement CustomData { get; set; }
}