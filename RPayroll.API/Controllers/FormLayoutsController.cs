using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.FormLayout;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/form-layouts")]
[Authorize]
public class FormLayoutsController : ControllerBase
{
    private readonly IFormLayoutService _formLayoutService;

    public FormLayoutsController(IFormLayoutService formLayoutService)
    {
        _formLayoutService = formLayoutService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] FormLayoutDto dto)
    {
        var tabs = dto.Tabs?.Count > 0
            ? dto.Tabs
            : new List<FormLayoutTabDto>
            {
                new()
                {
                    Id = "tab-1",
                    Name = "Tab 1",
                    GridSize = dto.GridSize,
                    Layout = dto.Layout
                }
            };

        var formLayout = new FormLayoutDefinition
        {
            Key = dto.Key ?? string.Empty,
            ModelKey = dto.Entity,
            ModelLabel = string.IsNullOrWhiteSpace(dto.EntityLabel) ? SplitSlug(dto.Entity) : dto.EntityLabel,
            Name = string.IsNullOrWhiteSpace(dto.Name)
                ? $"{(string.IsNullOrWhiteSpace(dto.EntityLabel) ? SplitSlug(dto.Entity) : dto.EntityLabel)} Form"
                : dto.Name,
            GridSize = dto.GridSize,
            SubmitUrl = dto.SubmitUrl ?? string.Empty,
            SubmitMethod = dto.SubmitMethod ?? "POST",
            LayoutJson = JsonSerializer.Serialize(tabs),
            MenuKey = dto.MenuKey ?? string.Empty,
            OwnerUserId = dto.OwnerUserId,
            VisibilityScope = Enum.IsDefined(typeof(VisibilityScope), dto.VisibilityScope)
                ? (VisibilityScope)dto.VisibilityScope
                : VisibilityScope.AllUsers,
            MaxHierarchyLevelAllowed = dto.MaxHierarchyLevelAllowed
        };

        var saved = await _formLayoutService.SaveAsync(formLayout, dto.SaveMode, dto.TargetLayoutKey);
        return Ok(Map(saved));
    }

    [HttpGet("{layoutKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByKey(string layoutKey)
    {
        var layout = await _formLayoutService.GetByKeyAsync(layoutKey);
        return layout == null ? NotFound() : Ok(Map(layout));
    }

    [HttpGet("model/{modelKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByModelKey(string modelKey)
    {
        var layout = await _formLayoutService.GetByModelKeyAsync(modelKey);
        return layout == null ? NotFound() : Ok(Map(layout));
    }

    [HttpGet("menu/{menuKey}")]
    public async Task<IActionResult> GetByMenuKey(string menuKey)
    {
        var layout = await _formLayoutService.GetByMenuKeyAsync(menuKey);
        return layout == null ? NotFound() : Ok(Map(layout));
    }

    private static FormLayoutDto Map(FormLayoutDefinition layout)
    {
        var tabs = DeserializeTabs(layout.LayoutJson, layout.GridSize);
        var items = tabs.FirstOrDefault()?.Layout ?? new List<FormLayoutItemDto>();
        return new FormLayoutDto
        {
            Key = layout.Key,
            MenuKey = layout.MenuKey,
            Name = layout.Name,
            OwnerUserId = layout.OwnerUserId,
            VisibilityScope = (int)layout.VisibilityScope,
            MaxHierarchyLevelAllowed = layout.MaxHierarchyLevelAllowed,
            Entity = layout.ModelKey,
            EntityLabel = layout.ModelLabel,
            GridSize = layout.GridSize,
            SubmitUrl = layout.SubmitUrl,
            SubmitMethod = layout.SubmitMethod,
            Layout = items,
            Tabs = tabs
        };
    }

    private static List<FormLayoutTabDto> DeserializeTabs(string layoutJson, string gridSize)
    {
        try
        {
            var tabs = JsonSerializer.Deserialize<List<FormLayoutTabDto>>(layoutJson);
            if (tabs is { Count: > 0 })
            {
                return tabs;
            }
        }
        catch
        {
            // fall back to legacy layout payloads
        }

        var items = JsonSerializer.Deserialize<List<FormLayoutItemDto>>(layoutJson) ?? new List<FormLayoutItemDto>();
        return new List<FormLayoutTabDto>
        {
            new()
            {
                Id = "tab-1",
                Name = "Tab 1",
                GridSize = gridSize,
                Layout = items
            }
        };
    }

    private static string SplitSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $" {character}"
                : character.ToString()));
    }
}
