using System.Text.Json;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class FormLayoutService : IFormLayoutService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public FormLayoutService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FormLayoutDefinition> SaveAsync(FormLayoutDefinition formLayout, string saveMode = "update", string? targetLayoutKey = null)
    {
        EnsureAdmin();
        Validate(formLayout);

        var normalizedSaveMode = string.IsNullOrWhiteSpace(saveMode) ? "update" : saveMode.Trim().ToLowerInvariant();
        var target = !string.IsNullOrWhiteSpace(targetLayoutKey)
            ? await _unitOfWork.FormLayouts.GetByKeyAsync(targetLayoutKey, includeInactive: true)
            : await _unitOfWork.FormLayouts.GetByModelKeyAsync(formLayout.ModelKey, includeInactive: true);

        if (target != null)
        {
            EnsureLockedContract(target, formLayout);
        }

        if (normalizedSaveMode == "draft")
        {
            var draft = await CreateDraftAsync(formLayout, target);
            await _unitOfWork.FormLayouts.AddAsync(draft);
            await EnsureMenuItemAsync(draft, isNew: true);
            await _unitOfWork.SaveChangesAsync();
            return draft;
        }

        if (target == null)
        {
            formLayout.Key = string.IsNullOrWhiteSpace(formLayout.Key)
                ? await BuildUniqueLayoutKeyAsync($"form-{formLayout.ModelKey.ToLowerInvariant()}")
                : formLayout.Key;
            formLayout.MenuKey = string.IsNullOrWhiteSpace(formLayout.MenuKey)
                ? formLayout.Key
                : formLayout.MenuKey;
            formLayout.Name = string.IsNullOrWhiteSpace(formLayout.Name)
                ? $"{formLayout.ModelLabel} Form"
                : formLayout.Name;
            formLayout.OwnerUserId ??= _currentUser.UserId;
            formLayout.CreatedDate = DateTime.UtcNow;
            formLayout.CreatedBy = _currentUser.UserId.ToString();
            formLayout.Status = StatusCode.Accepted;
            await _unitOfWork.FormLayouts.AddAsync(formLayout);
            await EnsureMenuItemAsync(formLayout, isNew: true);
            await _unitOfWork.SaveChangesAsync();
            return formLayout;
        }

        target.Name = string.IsNullOrWhiteSpace(formLayout.Name) ? target.Name : formLayout.Name;
        target.ModelLabel = formLayout.ModelLabel;
        target.GridSize = formLayout.GridSize;
        target.LayoutJson = formLayout.LayoutJson;
        target.VisibilityScope = formLayout.VisibilityScope;
        target.MaxHierarchyLevelAllowed = formLayout.MaxHierarchyLevelAllowed;
        target.UpdatedDate = DateTime.UtcNow;
        target.UpdatedBy = _currentUser.UserId.ToString();
        await _unitOfWork.FormLayouts.UpdateAsync(target);
        await EnsureMenuItemAsync(target, isNew: false);
        await _unitOfWork.SaveChangesAsync();
        return target;
    }

    public Task<FormLayoutDefinition?> GetByKeyAsync(string key)
    {
        return _unitOfWork.FormLayouts.GetByKeyAsync(key, includeInactive: true);
    }

    public async Task<FormLayoutDefinition?> GetByMenuKeyAsync(string menuKey)
    {
        var layout = await _unitOfWork.FormLayouts.GetByMenuKeyAsync(menuKey, includeInactive: true);
        if (layout == null)
        {
            return null;
        }

        EnsureCanView(layout);
        return layout;
    }

    public Task<FormLayoutDefinition?> GetByModelKeyAsync(string modelKey)
    {
        return _unitOfWork.FormLayouts.GetByModelKeyAsync(modelKey, includeInactive: true);
    }

    public async Task<IEnumerable<FormLayoutDefinition>> GetAllAsync()
    {
        EnsureAdmin();
        return await _unitOfWork.FormLayouts.GetAllAsync(includeInactive: true);
    }

    private async Task EnsureMenuItemAsync(FormLayoutDefinition layout, bool isNew)
    {
        var menuItem = await _unitOfWork.MenuItems.GetByKeyAsync(layout.MenuKey, includeInactive: true);
        if (menuItem == null)
        {
            var nextOrder = (await _unitOfWork.MenuItems.GetAllAsync(includeInactive: true)).Count + 1;
            menuItem = new AppMenuItem
            {
                Key = layout.MenuKey,
                Label = layout.Name,
                FormLayoutKey = layout.Key,
                Order = nextOrder,
                IsSystem = false,
                OwnerUserId = layout.OwnerUserId,
                VisibilityScope = layout.VisibilityScope,
                MaxHierarchyLevelAllowed = layout.MaxHierarchyLevelAllowed,
                CreatedDate = DateTime.UtcNow,
                Status = StatusCode.Pending
            };
            await _unitOfWork.MenuItems.AddAsync(menuItem);
            return;
        }

        menuItem.Label = layout.Name;
        menuItem.FormLayoutKey = layout.Key;
        menuItem.OwnerUserId = layout.OwnerUserId;
        menuItem.VisibilityScope = layout.VisibilityScope;
        menuItem.MaxHierarchyLevelAllowed = layout.MaxHierarchyLevelAllowed;
        menuItem.UpdatedDate = DateTime.UtcNow;
        if (isNew && menuItem.Status == StatusCode.Rejected)
        {
            menuItem.Status = StatusCode.Pending;
        }
        await _unitOfWork.MenuItems.UpdateAsync(menuItem);
    }

    private static void Validate(FormLayoutDefinition layout)
    {
        if (string.IsNullOrWhiteSpace(layout.ModelKey))
        {
            throw new InvalidOperationException("Model key is required.");
        }

        if (string.IsNullOrWhiteSpace(layout.ModelLabel))
        {
            throw new InvalidOperationException("Model label is required.");
        }

        if (string.IsNullOrWhiteSpace(layout.SubmitUrl))
        {
            throw new InvalidOperationException("Submit URL is required.");
        }

        if (string.IsNullOrWhiteSpace(layout.LayoutJson))
        {
            throw new InvalidOperationException("Layout JSON is required.");
        }

        if (layout.VisibilityScope == VisibilityScope.RoleBased && !layout.MaxHierarchyLevelAllowed.HasValue)
        {
            throw new InvalidOperationException("Maximum hierarchy level is required for role-based visibility.");
        }

        JsonDocument.Parse(layout.LayoutJson).Dispose();
    }

    private void EnsureAdmin()
    {
        if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Admin role required.");
        }
    }

    private static void EnsureLockedContract(FormLayoutDefinition existing, FormLayoutDefinition incoming)
    {
        if (!string.Equals(existing.ModelKey, incoming.ModelKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Model key cannot be changed while editing a form.");
        }

        if (!string.Equals(existing.SubmitUrl, incoming.SubmitUrl, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Submit URL cannot be changed while editing a form.");
        }

        if (!string.Equals(existing.SubmitMethod, incoming.SubmitMethod, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Submit method cannot be changed while editing a form.");
        }
    }

    private void EnsureCanView(FormLayoutDefinition layout)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var canView = layout.VisibilityScope switch
        {
            VisibilityScope.AllUsers => true,
            VisibilityScope.RoleBased => _currentUser.HierarchyLevel <= (layout.MaxHierarchyLevelAllowed ?? int.MaxValue),
            VisibilityScope.OwnerOnly => layout.OwnerUserId == _currentUser.UserId,
            _ => false
        };

        if (!canView)
        {
            throw new UnauthorizedAccessException("You do not have access to this form.");
        }
    }

    private async Task<FormLayoutDefinition> CreateDraftAsync(FormLayoutDefinition formLayout, FormLayoutDefinition? source)
    {
        var baseKey = source?.ModelKey ?? formLayout.ModelKey;
        var baseName = string.IsNullOrWhiteSpace(formLayout.Name)
            ? source?.Name ?? $"{formLayout.ModelLabel} Form"
            : formLayout.Name;
        var draftKey = await BuildUniqueLayoutKeyAsync($"form-{baseKey.ToLowerInvariant()}");

        return new FormLayoutDefinition
        {
            Key = draftKey,
            MenuKey = draftKey,
            Name = baseName,
            ModelKey = source?.ModelKey ?? formLayout.ModelKey,
            ModelLabel = source?.ModelLabel ?? formLayout.ModelLabel,
            GridSize = formLayout.GridSize,
            SubmitUrl = source?.SubmitUrl ?? formLayout.SubmitUrl,
            SubmitMethod = source?.SubmitMethod ?? formLayout.SubmitMethod,
            LayoutJson = formLayout.LayoutJson,
            OwnerUserId = formLayout.OwnerUserId ?? source?.OwnerUserId ?? _currentUser.UserId,
            VisibilityScope = formLayout.VisibilityScope,
            MaxHierarchyLevelAllowed = formLayout.MaxHierarchyLevelAllowed,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId.ToString(),
            Status = StatusCode.Accepted
        };
    }

    private async Task<string> BuildUniqueLayoutKeyAsync(string baseKey)
    {
        var normalizedBase = string.IsNullOrWhiteSpace(baseKey) ? "form-layout" : baseKey;
        var key = normalizedBase;
        var suffix = 1;

        while (await _unitOfWork.FormLayouts.GetByKeyAsync(key, includeInactive: true) != null)
        {
            key = $"{normalizedBase}-{suffix}";
            suffix += 1;
        }

        return key;
    }
}
