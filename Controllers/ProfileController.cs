using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAuthApi.Data;
using UserAuthApi.DTOs;

namespace UserAuthApi.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        JsonElement customData;

        try
        {
            customData = JsonSerializer.Deserialize<JsonElement>(
                string.IsNullOrWhiteSpace(user.CustomData)
                    ? "{}"
                    : user.CustomData
            );
        }
        catch (JsonException)
        {
            customData = JsonSerializer.Deserialize<JsonElement>("{}");
        }

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.CreatedAt,
                customData
            }
        });
    }

    [HttpPut("{userId:int}/custom-data")]
    public async Task<IActionResult> UpdateCustomData(
        int userId,
        UpdateCustomDataDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (dto.CustomData.ValueKind is JsonValueKind.Undefined)
        {
            return BadRequest(new
            {
                message = "CustomData is required."
            });
        }

        user.CustomData = dto.CustomData.GetRawText();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Custom data updated successfully.",
            customData = dto.CustomData
        });
    }
}