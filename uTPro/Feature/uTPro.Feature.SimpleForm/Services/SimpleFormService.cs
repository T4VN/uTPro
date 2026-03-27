using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Scoping;
using uTPro.Feature.SimpleForm.Models;

namespace uTPro.Feature.SimpleForm.Services;

class DISimpleFormService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISimpleFormService, SimpleFormService>();
}

public interface ISimpleFormService
{
    List<FormViewModel> GetAllForms();
    FormViewModel? GetForm(int id);
    FormViewModel? GetFormByAlias(string alias);
    (bool Success, string Message, int Id) SaveForm(SaveFormRequest request);
    (bool Success, string Message) DeleteForm(int id);
    (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, string? ip, string? ua);
    PagedResult<SubmissionViewModel> GetSubmissions(int formId, int skip, int take);
    (bool Success, string Message) DeleteSubmission(int id);
}

internal class SimpleFormService(IScopeProvider scopeProvider, ILogger<SimpleFormService> logger) : ISimpleFormService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public List<FormViewModel> GetAllForms()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var dtos = scope.Database.Fetch<SimpleFormDto>("SELECT * FROM utpro_SimpleForm ORDER BY Name");
        return dtos.Select(MapToViewModel).ToList();
    }

    public FormViewModel? GetForm(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var dto = scope.Database.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Id = @0", id);
        return dto == null ? null : MapToViewModel(dto);
    }

    public FormViewModel? GetFormByAlias(string alias)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var dto = scope.Database.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Alias = @0", alias);
        return dto == null ? null : MapToViewModel(dto);
    }

    public (bool Success, string Message, int Id) SaveForm(SaveFormRequest request)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var db = scope.Database;
            var now = DateTime.UtcNow;
            var fieldsJson = JsonSerializer.Serialize(request.Fields, JsonOpts);

            if (request.Id > 0)
            {
                var existing = db.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Id = @0", request.Id);
                if (existing == null) return (false, "Form not found", 0);

                existing.Name = request.Name;
                existing.Alias = request.Alias;
                existing.FieldsJson = fieldsJson;
                existing.SuccessMessage = request.SuccessMessage;
                existing.RedirectUrl = request.RedirectUrl;
                existing.EmailTo = request.EmailTo;
                existing.EmailSubject = request.EmailSubject;
                existing.StoreSubmissions = request.StoreSubmissions;
                existing.IsEnabled = request.IsEnabled;
                existing.UpdatedUtc = now;
                db.Update(existing);
                return (true, "Form updated", existing.Id);
            }
            else
            {
                var dup = db.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Alias = @0", request.Alias);
                if (dup != null) return (false, "Alias already exists", 0);

                var dto = new SimpleFormDto
                {
                    Name = request.Name,
                    Alias = request.Alias,
                    FieldsJson = fieldsJson,
                    SuccessMessage = request.SuccessMessage,
                    RedirectUrl = request.RedirectUrl,
                    EmailTo = request.EmailTo,
                    EmailSubject = request.EmailSubject,
                    StoreSubmissions = request.StoreSubmissions,
                    IsEnabled = request.IsEnabled,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                db.Insert(dto);
                return (true, "Form created", dto.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving form");
            return (false, ex.Message, 0);
        }
    }

    public (bool Success, string Message) DeleteForm(int id)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Execute("DELETE FROM utpro_SimpleFormSubmission WHERE FormId = @0", id);
            scope.Database.Execute("DELETE FROM utpro_SimpleForm WHERE Id = @0", id);
            return (true, "Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting form {Id}", id);
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, string? ip, string? ua)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var form = scope.Database.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Alias = @0", alias);
            if (form == null) return (false, "Form not found");
            if (!form.IsEnabled) return (false, "Form is disabled");

            // Validate required fields
            var fields = string.IsNullOrEmpty(form.FieldsJson)
                ? [] : JsonSerializer.Deserialize<List<FormFieldViewModel>>(form.FieldsJson, JsonOpts) ?? [];

            foreach (var f in fields.Where(f => f.Required))
            {
                if (!data.TryGetValue(f.Name, out var val) || string.IsNullOrWhiteSpace(val))
                    return (false, $"Field '{f.Label}' is required");
            }

            if (form.StoreSubmissions)
            {
                var sub = new SimpleFormSubmissionDto
                {
                    FormId = form.Id,
                    DataJson = JsonSerializer.Serialize(data, JsonOpts),
                    IpAddress = ip,
                    UserAgent = ua?.Length > 500 ? ua[..500] : ua,
                    CreatedUtc = DateTime.UtcNow
                };
                scope.Database.Insert(sub);
            }

            return (true, form.SuccessMessage ?? "Thank you for your submission!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting form {Alias}", alias);
            return (false, ex.Message);
        }
    }

    public PagedResult<SubmissionViewModel> GetSubmissions(int formId, int skip, int take)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var sql = scope.SqlContext.Sql()
            .Select("*").From("utpro_SimpleFormSubmission")
            .Where("FormId = @0", formId)
            .OrderByDescending("CreatedUtc");

        var page = db.Page<SimpleFormSubmissionDto>(skip / Math.Max(take, 1) + 1, take, sql);
        return new PagedResult<SubmissionViewModel>
        {
            Items = page.Items.Select(MapSubmission),
            Total = page.TotalItems
        };
    }

    public (bool Success, string Message) DeleteSubmission(int id)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Execute("DELETE FROM utpro_SimpleFormSubmission WHERE Id = @0", id);
            return (true, "Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting submission {Id}", id);
            return (false, ex.Message);
        }
    }

    private static FormViewModel MapToViewModel(SimpleFormDto dto) => new()
    {
        Id = dto.Id, Name = dto.Name, Alias = dto.Alias,
        Fields = string.IsNullOrEmpty(dto.FieldsJson)
            ? [] : JsonSerializer.Deserialize<List<FormFieldViewModel>>(dto.FieldsJson, JsonOpts) ?? [],
        SuccessMessage = dto.SuccessMessage, RedirectUrl = dto.RedirectUrl,
        EmailTo = dto.EmailTo, EmailSubject = dto.EmailSubject,
        StoreSubmissions = dto.StoreSubmissions, IsEnabled = dto.IsEnabled,
        CreatedUtc = dto.CreatedUtc, UpdatedUtc = dto.UpdatedUtc
    };

    private static SubmissionViewModel MapSubmission(SimpleFormSubmissionDto dto) => new()
    {
        Id = dto.Id, FormId = dto.FormId,
        Data = string.IsNullOrEmpty(dto.DataJson)
            ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(dto.DataJson, JsonOpts) ?? [],
        IpAddress = dto.IpAddress, CreatedUtc = dto.CreatedUtc
    };
}
